namespace Lunra.Hothouse.Models
{
	public struct GoalActivityReservation
	{
		public string ReservationId { get; }
		public string ActivityId { get; }
		public InstanceId Client { get; }
		public InstanceId Destination { get; }
		public DayTime AppointmentBegin { get; }
		public DayTime AppointmentEnd { get; }

		public GoalActivityReservation(
			string reservationId,
			string activityId,
			InstanceId client,
			InstanceId destination,
			DayTime appointmentBegin,
			DayTime appointmentEnd
		)
		{
			ReservationId = reservationId;
			ActivityId = activityId;
			Client = client;
			Destination = destination;
			AppointmentBegin = appointmentBegin;
			AppointmentEnd = appointmentEnd;
		}
	}
}