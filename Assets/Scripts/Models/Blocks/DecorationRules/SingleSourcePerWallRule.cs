using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Views;

namespace Lunra.Hothouse.Models
{
	public class SingleSourcePerWallRule : DecorationRule
	{
		public override bool Applies(DecorationView view) => view.PrefabTags.Any(t => t.StartsWith(DecorationView.Constants.Tags.Sources.Prefix));

		public override bool Validate(DecorationView view, RoomInfo room)
		{
			return room.DecorationTagsOnWall
				.None(kv => kv.Key.StartsWith(DecorationView.Constants.Tags.Sources.Prefix) && 0 < kv.Value);
		}
	}
}