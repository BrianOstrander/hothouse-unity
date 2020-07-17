using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public class FarmPlot
	{
		public enum States
		{
			Unknown = 0,
			Invalid = 10,
			ReadyToPlow = 20,
			ReadyToSow = 30,
			Sown = 40,
			ReadyForHarvest = 50,
			Harvested = 60
		}

		public Vector3 Position;
		public string RoomId;
		public States State;
		public InstanceId Flora;
		
		public FarmPlot(
			Vector3 position,
			string roomId,
			States state,
			InstanceId flora
		)
		{
			Position = position;
			RoomId = roomId;
			State = state;
			Flora = flora;
		}
	}
}