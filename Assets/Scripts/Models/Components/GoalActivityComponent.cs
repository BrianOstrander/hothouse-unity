using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;

namespace Lunra.Hothouse.Models
{
	public interface IGoalActivityModel : IInventoryModel
	{
		GoalActivityComponent Activities { get; }
	}

	public class GoalActivityComponent : ComponentModel<IGoalActivityModel>
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

		public GoalActivity[] GetAvailable(DayTime appointmentBegin)
		{
			var results = new List<GoalActivity>();

			var appointmentEndLimit = appointmentBegin + new DayTime(99);
			
			foreach (var reservation in Reservations.Value)
			{
				if (reservation.AppointmentBegin <= appointmentBegin && appointmentBegin <= reservation.AppointmentEnd)
				{
					// Our appointment time begins in the middle of a reservation.
					return results.ToArray();
				}
				

				if (appointmentBegin < reservation.AppointmentBegin)
				{
					appointmentEndLimit = reservation.AppointmentBegin;
					break;
				}
			}

			foreach (var activity in All.Value)
			{
				if (activity.AnyRestrictions) continue;
				
				if ((appointmentBegin + activity.Activity.Duration) < appointmentEndLimit)
				{
					results.Add(activity.Activity);
				}
			}

			return results.ToArray();
		}

		public GoalActivity GetActivity(string reservationId)
		{
			var reservation = Reservations.Value.First(r => r.ReservationId == reservationId);
			return All.Value.First(a => a.Activity.Id == reservation.ActivityId).Activity;
		}
		
		public GoalActivityReservation ReserveActivity(
			IGoalPromiseModel client,

			GoalActivity activity,
			DayTime appointmentBegin
		)
		{
			var reservation = GoalActivityReservation.New(
				activity,
				client,
				Model,
				appointmentBegin
			);
			
			client.GoalPromises.All.Push(reservation);

			reservationsListener.Value = Reservations.Value
				.Append(reservation)
				// .OrderBy(r => r.AppointmentBegin)
				.OrderBy(r => r.AppointmentBegin.TotalTime) // Not sure if ordering works on these... need to check...
				.ToArray();

			if (activity.Input.HasValue)
			{
				Model.Inventory.AddForbidden(activity.Input.Value);	
			}
			
			if (activity.Output.HasValue)
			{
				Model.Inventory.AddReserved(activity.Output.Value);
			}
			
			return reservation;
		}

		// I don't think I need this...
		/*
		public void CorrectReservation(
			string reservationId,
			DayTime appointmentBegin
		)
		{
			var activity = GetActivity(reservationId);

			reservationsListener.Value = Reservations.Value
				.Select(
					r =>
					{
						if (r.ReservationId != reservationId) return r;

						return r.New(
							appointmentBegin,
							appointmentBegin + activity.Duration
						);
					}
				)
				.ToArray();
		}
		*/

		public GoalActivity UnReserveActivity(IGoalModel client)



		{
			var reservation = client.GoalPromises.All.Pop();
			var activity = All.Value.First(a => a.Activity.Id == reservation.ActivityId).Activity;
			
			reservationsListener.Value = reservationsListener.Value
				.Where(r => r.ReservationId != reservation.ReservationId)
				.ToArray();

			if (activity.Input.HasValue)
			{
				Model.Inventory.RemoveForbidden(activity.Input.Value);
				Model.Inventory.Remove(activity.Input.Value);
			}

			if (activity.Output.HasValue)
			{
				Model.Inventory.RemoveReserved(activity.Output.Value);
				Model.Inventory.Add(activity.Output.Value);
			}
			
			return activity;
		}

		public void CalculateRestrictions()


		{
			if (All.Value.None()) return;
			
			var updatedStates = new List<State>();

			var availableCapacity = calculateAvailableCapacity ? Model.Inventory.AvailableCapacity.Value.GetCapacityFor(Model.Inventory.Available.Value) : default;

			foreach (var state in All.Value)
			{
				var restriction = Restrictions.None;

				if (!Model.Enterable.AnyAvailable())
				{
					restriction |= Restrictions.MissingAvailableEntrances;
				}
				if (state.Activity.Input.HasValue && !Model.Inventory.Available.Value.Contains(state.Activity.Input.Value))
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

				if (Reservations.Value.Any())
				{
					result += "\n - Reservations:";
					foreach (var reservation in Reservations.Value)
					{
						result += "\n\t" + reservation;
					}
				}
			}

			return result;
		}
	}
	
	public static class GoalActivityGameModelExtensions
	{
		public static IEnumerable<IGoalActivityModel> GetActivities(
			this GameModel game	
		)
		{
			return game.Buildings.AllActive;
		}
	}
}