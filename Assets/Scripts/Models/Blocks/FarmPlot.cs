using System;
using Lunra.Core;
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
			ReadyToSow = 30,
			Sown = 40
		}

		public string Id;
		public Vector3 Position;
		public string RoomId;
		public FloatRange Radius;
		public States State;
		public InstanceId Flora;
		public InstanceId AttendingFarmer;
		
		public FarmPlot(
			Vector3 position,
			string roomId,
			FloatRange radius,
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
			AttendingFarmer = InstanceId.Null();
		}
	}
}