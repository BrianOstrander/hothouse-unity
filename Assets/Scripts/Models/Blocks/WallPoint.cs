using System;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public struct WallPoint
	{
		public int Index;
		public Vector3 Position;
		public Vector3 WallNormal;
		public float Height;
		public int? DoorIndex;
		public int[] Neighbors;
	}
}