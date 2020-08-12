using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public static class LayerNames
	{
		public const string Floor = "Floor";
		public const string Unexplored = "Unexplored";
		public const string RoomBoundary = "RoomBoundary";
		public const string Flora = "Flora";
	}

	public static class LayerMasks
	{
		public static readonly int Default = LayerMask.GetMask("Default");
		public static readonly int Floor = LayerMask.GetMask(LayerNames.Floor);
		public static readonly int DefaultAndFloor = LayerMask.GetMask("Default", LayerNames.Floor);
		public static readonly int RoomBoundary = LayerMask.GetMask(LayerNames.RoomBoundary);
		public static readonly int Unexplored = LayerMask.GetMask(LayerNames.Unexplored);
	}
}