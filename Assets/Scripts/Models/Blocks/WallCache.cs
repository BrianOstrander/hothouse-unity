using System;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	[Serializable]
	public struct WallCache
	{
		public int Index;
		[SerializeField] int doorIndex;

		public Vector3 Begin;
		public Vector3 End;
		public Vector3 Normal;
		public float Height;
		public bool Valid;

		public int? DoorIndex
		{
			get
			{
				if (doorIndex == -1) return null;
				return doorIndex;
			}
			set
			{
				if (value.HasValue)
				{
					if (value == -1) throw new Exception("-1 is an invalid doorId");
					doorIndex = value.Value;
				}
				else
				{
					doorIndex = -1;
				}
			}
		}
	}
}