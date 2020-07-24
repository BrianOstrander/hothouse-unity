using System;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	[Serializable]
	public struct WallCache
	{
		public int Id;
		[SerializeField] int doorId;

		public Vector3 Begin;
		public Vector3 End;
		public Vector3 Normal;
		public float Height;

		public int? DoorId
		{
			get
			{
				if (doorId == -1) return null;
				return doorId;
			}
			set
			{
				if (value.HasValue)
				{
					if (value == -1) throw new Exception("-1 is an invalid doorId");
					doorId = value.Value;
				}
				else
				{
					doorId = -1;
				}
			}
		}
	}
}