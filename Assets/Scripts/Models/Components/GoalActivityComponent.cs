using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Lunra.Core;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public interface IGoalActivityModel : IInventoryModel
	{
		GoalActivityComponent Activities { get; }
	}

	public class GoalActivityComponent : Model
	{
		public struct State
		{
			public GoalActivity Activity { get; }
			public Restrictions Restriction { get; }
			public bool AnyRestrictions { get; }

			public State(
				GoalActivity activity,
				Restrictions restriction
			)
			{
				Activity = activity;
				Restriction = restriction;
				AnyRestrictions = Restriction != Restrictions.None;
			}
		}
	
		[Flags]
		public enum Restrictions
		{
			None = 0,
			NotCalculated = 1,
			MissingAvailableEntrances = 2,
			MissingInput = 4,
			MissingOutputCapacity = 8
		}
	
		#region Serialized
		[JsonProperty] State[] all = new State[0];
		readonly ListenerProperty<State[]> allListener;
		[JsonIgnore] public ReadonlyProperty<State[]> All { get; }

		[JsonProperty] bool calculateAvailableCapacity;
		
		[JsonProperty] GoalActivityReservation[] reservations = new GoalActivityReservation[0];
		readonly ListenerProperty<GoalActivityReservation[]> reservationsListener;
		[JsonIgnore] public ReadonlyProperty<GoalActivityReservation[]> Reservations { get; }
		#endregion
		
		#region Non Serialized
		#endregion

		public GoalActivityComponent()
		{
			All = new ReadonlyProperty<State[]>(
				value => all = value,
				() => all,
				out allListener
			);
			Reservations = new ReadonlyProperty<GoalActivityReservation[]>(
				value => reservations = value,
				() => reservations,
				out reservationsListener
			);
		}

		public void Reset(GoalActivity[] all)
		{
			allListener.Value = all
				.Select(a => new State(a, Restrictions.NotCalculated))
				.ToArray();

			calculateAvailableCapacity = All.Value.Any(a => a.Activity.Output.HasValue);
			
			reservationsListener.Value = new GoalActivityReservation[0];
		}

		// public GoalActivity[] GetAvailable(
		// 	DayTime appointmentBegin
		// )
		// {
		// 	
		// }
		
		public GoalActivityReservation ReserveActivity(
			IGoalPromiseModel client,
			IGoalActivityModel destination,
			GoalActivity activity,
			DayTime appointmentBegin
		)
		{
			var reservation = new GoalActivityReservation(
				Guid.NewGuid().ToString(),
				activity.Id,
				InstanceId.New(client),
				InstanceId.New(destination),
				appointmentBegin,
				appointmentBegin + activity.Duration
			);
			
			client.GoalPromises.All.Push(reservation);

			reservationsListener.Value = Reservations.Value
				.Append(reservation)
				// .OrderBy(r => r.AppointmentBegin)
				.OrderBy(r => r.AppointmentBegin.TotalTime) // Not sure if ordering works on these... need to check...
				.ToArray();
			
			return reservation;
		}

		public void CalculateRestrictions(
			IGoalActivityModel model
		)
		{
			if (All.Value.None()) return;
			
			var updatedStates = new List<State>();

			var availableCapacity = calculateAvailableCapacity ? model.Inventory.AvailableCapacity.Value.GetCapacityFor(model.Inventory.Available.Value) : default;

			foreach (var state in All.Value)
			{
				var restriction = Restrictions.None;

				if (!model.Enterable.AnyAvailable())
				{
					restriction |= Restrictions.MissingAvailableEntrances;
				}
				if (state.Activity.Input.HasValue && !model.Inventory.Available.Value.Contains(state.Activity.Input.Value))
				{
					restriction |= Restrictions.MissingInput;
				}
				if (state.Activity.Output.HasValue && !availableCapacity.Contains(state.Activity.Output.Value))
				{
					restriction |= Restrictions.MissingOutputCapacity;
				}

				if (state.Restriction != restriction)
				{
					updatedStates.Add(
						new State(
							state.Activity,
							restriction
						)	
					);
				}
			}

			if (updatedStates.None()) return;

			allListener.Value = All.Value
				.Where(a => updatedStates.None(u => u.Activity.Id == a.Activity.Id))
				.Concat(updatedStates)
				.ToArray();
		}

		public override string ToString()
		{
			var result = "Activities: [ " + All.Value.Count(a => !a.AnyRestrictions) + " / " + All.Value.Length + " ] Available";

			if (All.Value.Any())
			{
				result += "\n - All:";
				foreach (var state in All.Value.OrderBy(a => a.Activity.Type))
				{
					result += "\n\t[ " + (state.AnyRestrictions ? "Not Available" : "Available") + " ]\t" + state.Activity.Type;

					if (state.AnyRestrictions)
					{
						foreach (var restriction in EnumExtensions.GetValues(Restrictions.None))
						{
							if (state.Restriction.HasFlag(restriction)) result += "\n\t\t" + restriction;
						}
					}
				}
				
				
			}

			return result;
		}
	}
}