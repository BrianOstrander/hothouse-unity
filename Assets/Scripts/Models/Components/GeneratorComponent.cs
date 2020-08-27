using System;
using System.Linq;
using Lunra.Core;
using Lunra.NumberDemon;
using Lunra.Satchel;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public interface IGeneratorModel : IInventoryModel, IEnterableModel
	{
		GeneratorComponent Generator { get; }
	}
	
	public class GeneratorComponent : ComponentModel<IGeneratorModel>
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
		[JsonProperty] (Item Item, int Minimum, int Maximum)[] items;
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

		public override void Bind()
		{
			Game.SimulationUpdate += OnGameSimulationUpdate;
		}

		public override void UnBind()
		{
			Game.SimulationUpdate -= OnGameSimulationUpdate;
		}

		public void Reset(
			FloatRange refillDurationRange,
			FloatRange expireDurationRange,
			params (Item Item, int Minimum, int Maximum)[] items
		)
		{
			state = States.Unknown;
			this.refillDurationRange = refillDurationRange;
			this.expireDurationRange = expireDurationRange;
			this.items = items;
			nextState = DayTime.Zero;
			rateMaximum = items.Sum(i => i.Maximum);
		}

		void OnGameSimulationUpdate()
		{
			if (Game.SimulationTime.Value < nextState) return;

			float hoursUntilNextState;
			
			switch (state)
			{
				case States.Unknown:
				case States.WaitingToExpire:
					hoursUntilNextState = refillDurationRange.Evaluate(DemonUtility.NextFloat);
					state = States.WaitingToRefill;
					if (!Model.Inventory.Available.Value.IsEmpty) Model.Inventory.Remove(Model.Inventory.Available.Value);
					break;
				case States.WaitingToRefill:
					hoursUntilNextState = expireDurationRange.Evaluate(DemonUtility.NextFloat);
					state = States.WaitingToExpire;

					Debug.LogError("TODO: Handle item generation");
					// var availableMaximum = Model.Inventory.AvailableCapacity.Value.GetCapacityFor(Model.Inventory.Available.Value);
					// var generated = Inventory.FromEntries(
					// 	items
					// 		.Select(i => (i.Type, DemonUtility.GetNextInteger(i.Minimum, i.Maximum + 1)))
					// 		.ToArray()
					// );
					//
					// if (availableMaximum.Intersects(generated, out generated))
					// {
					// 	Model.Inventory.Add(generated);
					// }
					
					break;
				default:
					Debug.LogError("Unrecognized State: " + state);
					return;
			}

			nextState = Game.SimulationTime.Value + DayTime.FromHours(hoursUntilNextState);
		}

		public void CalculateRate() => rateListener.Value = Model.Inventory.All.Value.TotalWeight / rateMaximum;
		
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