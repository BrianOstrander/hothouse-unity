using System;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public class FarmPlot
	{
		public enum States
		{
			Unknown = 0,
			Invalid = 10,
			Blocked = 20,
			ReadyToPlow = 30,
			ReadyToSow = 40,
			Sown = 50,
			ReadyForHarvest = 60,
			Harvested = 70
		}

		public string Id;
		public Vector3 Position;
		public string RoomId;
		public float Radius;
		public States State;
		public InstanceId Flora;
		
		public FarmPlot(
			Vector3 position,
			string roomId,
			float radius,
			States state,
			InstanceId flora
		)
		{
			Id = Guid.NewGuid().ToString();
			Position = position;
			RoomId = roomId;
			Radius = radius;
			State = state;
			Flora = flora;
		}
	}
}