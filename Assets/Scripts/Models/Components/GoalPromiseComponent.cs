using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public interface IGoalPromiseModel : IRoomTransformModel
	{
		GoalPromiseComponent GoalPromises { get; }
	}

	public class GoalPromiseComponent : Model
	{
		#region Serialized
		[JsonProperty] Stack<GoalActivityReservation> all = new Stack<GoalActivityReservation>();
		[JsonIgnore] public StackProperty<GoalActivityReservation> All { get; }
		#endregion
		
		#region Non Serialized
		public bool HasAny() => all.Any();
		#endregion

		public GoalPromiseComponent()
		{
			All = new StackProperty<GoalActivityReservation>(all);
		}

		public void BreakRemainingPromises(
			GameModel game
		)
		{
			Debug.Log("BREAK REMAINING RESERVATIOS HEREEEE");
			// foreach (var promise in All.PeekAll())
			// {
			// 	if (!promise.Target.TryGetInstance<IObligationModel>(game, out var target)) continue;
			//
			// 	target.Obligations.RemoveForbidden(promise.Obligation);
			// }	
		}

		public void Reset()
		{
			All.Clear();
		}

		public override string ToString()
		{
			var result = "GOAL Promises:";
			var elements = All.PeekAll();
			
			if (elements.None()) return result + " None";

			foreach (var element in elements)
			{
				result += "\n - " + element;
			}

			return result;
		}
	}
}