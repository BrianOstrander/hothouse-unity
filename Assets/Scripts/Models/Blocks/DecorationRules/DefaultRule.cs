using System.Linq;
using Lunra.Hothouse.Views;

namespace Lunra.Hothouse.Models
{
	public class DefaultRule : DecorationRule
	{
		public override bool Applies(DecorationView view) => true;
		
		public override bool Validate(
			DecorationView view,
			RoomInfo room
		)
		{
			if (room.WallHeight < view.ExtentHeight) return false;
			if (room.WallSegmentWidth < view.ExtentsLeftRightWidth) return false;
			if (room.DecorationTagsBudgetForRoom.Any(kv => view.PrefabTags.Contains(kv.Key) && kv.Value <= 0)) return false;
			
			return true;
		}
	}
}