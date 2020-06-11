using System;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	[Serializable]
	public struct RoomCollider
	{
		public Collider Collider;
		public Vector3 Position;
		public Vector3 Scale;
		public Quaternion Rotation;
	}
}