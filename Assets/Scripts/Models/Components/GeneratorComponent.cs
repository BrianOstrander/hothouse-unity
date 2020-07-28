using System;
using System.Linq;
using Lunra.Core;
using Lunra.NumberDemon;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public interface IGeneratorModel : IInventoryModel
	{
		GeneratorComponent Generator { get; }
	}
	
	public class GeneratorComponent : Model
	{
		public enum States
		{
			Unknown = 0,
			WaitingToRefill = 10,
			WaitingToExpire = 20
		}
	
		#region Serialized
		[JsonProperty] States state;
		[JsonProperty] FloatRange refillDurationRange;
		[JsonProperty] FloatRange expireDurationRange;
		[JsonProperty] (Inventory.Types Type, int Minimum, int Maximum)[] items;
		[JsonProperty] DayTime nextState;
		[JsonProperty] float rateMaximum;

		[JsonProperty] float rate;
		readonly ListenerProperty<float> rateListener;
		[JsonIgnore] public ReadonlyProperty<float> Rate { get; }
		#endregion

		#region Non Serialized
		#endregion

		public GeneratorComponent()
		{
			Rate = new ReadonlyProperty<float>(
				value => rate = value,
				() => rate,
				out rateListener
			);
		}
		
		public void Reset(
			FloatRange refillDurationRange,
			FloatRange expireDurationRange,
			params (Inventory.Types Type, int Minimum, int Maximum)[] items
		)
		{
			state = States.Unknown;
			this.refillDurationRange = refillDurationRange;
			this.expireDurationRange = expireDurationRange;
			this.items = items;
			nextState = DayTime.Zero;
			rateMaximum = items.Sum(i => i.Maximum);
		}

		public void Update(
			GameModel game,
			IGeneratorModel model
		)
		{
			if (game.SimulationTime.Value < nextState) return;

			float hoursUntilNextState;
			
			switch (state)
			{
				case States.Unknown:
				case States.WaitingToExpire:
					hoursUntilNextState = refillDurationRange.Evaluate(DemonUtility.NextFloat);
					state = States.WaitingToRefill;
					if (!model.Inventory.Available.Value.IsEmpty) model.Inventory.Remove(model.Inventory.Available.Value);
					break;
				case States.WaitingToRefill:
					hoursUntilNextState = expireDurationRange.Evaluate(DemonUtility.NextFloat);
					state = States.WaitingToExpire;

					var availableMaximum = model.Inventory.AvailableCapacity.Value.GetCapacityFor(model.Inventory.Available.Value);
					var generated = Inventory.FromEntries(
						items
							.Select(i => (i.Type, DemonUtility.GetNextInteger(i.Minimum, i.Maximum + 1)))
							.ToArray()
					);

					if (availableMaximum.Intersects(generated, out generated))
					{
						model.Inventory.Add(generated);
					}
					
					break;
				default:
					Debug.LogError("Unrecognized State: " + state);
					return;
			}

			nextState = game.SimulationTime.Value + DayTime.FromHours(hoursUntilNextState);
		}

		public void CalculateRate(IGeneratorModel model) => rateListener.Value = model.Inventory.All.Value.TotalWeight / rateMaximum;
		
		public override string ToString()
		{
			var result = "Generator\n - ";
			
			switch (state)
			{
				case States.WaitingToRefill:
				case States.WaitingToExpire:
					result += state + ":\t" + nextState;
					break;
				default:
					result += "Unknown State:\t" + state;
					break;
			}

			return result;
		}
		
		public string ToString(DayTime time)
		{
			return ToString() + "\n - Remaining:\t\t" + (time < nextState ? (nextState - time) : DayTime.Zero);
		}
	}
}