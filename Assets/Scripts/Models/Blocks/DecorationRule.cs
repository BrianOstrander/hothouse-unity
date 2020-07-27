using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Views;
using Lunra.NumberDemon;

namespace Lunra.Hothouse.Models
{
	[DecorationRule]
	public abstract class DecorationRule
	{
		public class RoomInfo
		{
			public RoomModel Room;
			public float WallHeight;
			public float WallSegmentWidth;
			public Dictionary<string, int> DecorationTagsOnWall { get; } = new Dictionary<string, int>();
			public Dictionary<string, int> DecorationTagsInRoom { get; } = new Dictionary<string, int>();
			public Dictionary<string, int> DecorationTagsRequiredForRoom { get; } = new Dictionary<string, int>();
			public Dictionary<string, int> DecorationTagsBudgetForRoom { get; } = new Dictionary<string, int>();

			public void ResetForWall(
				float wallHeight,
				float wallSegmentWidth
			)
			{
				WallHeight = wallHeight;
				WallSegmentWidth = wallSegmentWidth;
				DecorationTagsOnWall.Clear();	
			}

			public void ResetForRoom(RoomModel room)
			{
				Room = room;
				DecorationTagsOnWall.Clear();
				DecorationTagsInRoom.Clear();
				DecorationTagsRequiredForRoom.Clear();
				DecorationTagsBudgetForRoom.Clear();
			}

			public void RegisterTags(params string[] tags)
			{
				foreach (var tag in tags)
				{
					RegisterTagInDictionary(tag, DecorationTagsOnWall);
					RegisterTagInDictionary(tag, DecorationTagsInRoom);

					if (DecorationTagsRequiredForRoom.TryGetValue(tag, out var requiredCount))
					{
						if (requiredCount <= 1) DecorationTagsRequiredForRoom.Remove(tag);
						else DecorationTagsRequiredForRoom[tag]--;
					}
					
					if (DecorationTagsBudgetForRoom.TryGetValue(tag, out var maximumCount))
					{
						if (0 < maximumCount) DecorationTagsBudgetForRoom[tag]--;
					}
				}
			}

			void RegisterTagInDictionary(string tag, Dictionary<string, int> target)
			{
				if (target.ContainsKey(tag)) target[tag]++;
				else target.Add(tag, 1);	
			}
		}
		
		public string Type { get; private set; }
		protected GameModel Game { get; private set; }
		protected Demon Generator { get; private set; }
		
		public virtual void Initialize(GameModel game)
		{
			Game = game;
			Generator = new Demon();
			
			Type = GetType().Name
				.Replace("Rule", String.Empty)
				.ToSnakeCase();
		}

		public abstract bool Applies(DecorationView view);
		
		public abstract bool Validate(DecorationView view, RoomInfo room);
	}
}