using System;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	[Serializable]
	public struct DoorCache
	{
		public int Index;
		public Transform Anchor;
		public GameObject Plug;
		public int Size;
	}
}