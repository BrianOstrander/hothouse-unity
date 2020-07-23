using System;
using Lunra.StyxMvp.Models;

namespace Lunra.Hothouse.Models
{
	public struct GoalActivityReservation
	{
		public static GoalActivityReservation New(
			GoalActivity activity,
			IModel client,
			IModel destination,
			DayTime appointmentBegin
		)
		{
			return new GoalActivityReservation(
				Guid.NewGuid().ToString(),
				activity.Id,
				activity.Type,
				InstanceId.New(client),
				InstanceId.New(destination),
				appointmentBegin,
				appointmentBegin + activity.Duration
			);
		}
		
		public string ReservationId { get; }
		public string ActivityId { get; }
		public string ActivityType { get; }
		public InstanceId Client { get; }
		public InstanceId Destination { get; }
		public DayTime AppointmentBegin { get; }
		public DayTime AppointmentEnd { get; }

		GoalActivityReservation(
			string reservationId,
			string activityId,
			string activityType,
			InstanceId client,
			InstanceId destination,
			DayTime appointmentBegin,
			DayTime appointmentEnd
		)
		{
			ReservationId = reservationId;
			ActivityId = activityId;
			ActivityType = activityType;
			Client = client;
			Destination = destination;
			AppointmentBegin = appointmentBegin;
			AppointmentEnd = appointmentEnd;
		}

		public GoalActivityReservation New(
			DayTime appointmentBegin,
			DayTime appointmentEnd
		)
		{
			return new GoalActivityReservation(
				ReservationId,
				ActivityId,
				ActivityType,
				Client,
				Destination,
				appointmentBegin,
				appointmentEnd
			);
		}
		
		public override string ToString()
		{
			var result = "[ " + Model.ShortenId(Client.Id) + " -> " + Model.ShortenId(Destination.Id) + " ] " + ActivityType;
			result += " from ( " + AppointmentBegin + " ) to ( " + AppointmentEnd + " )";

			return result;
		}
	}
}