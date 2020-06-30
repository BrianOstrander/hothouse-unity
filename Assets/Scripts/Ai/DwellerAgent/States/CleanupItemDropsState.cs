using System.Linq;
using Lunra.Hothouse.Models;

namespace Lunra.Hothouse.Ai
{
	public class CleanupItemDropsState<S> : AgentState<GameModel, DwellerModel>
		where S : AgentState<GameModel, DwellerModel>
	{
		public override string Name => "CleanupItemDrops";
		
		public override void OnInitialize()
		{
			AddTransitions(
				new ToReturnOnTimeout()
			);
		}

		public override void Begin()
		{
			var foundNavigableItemDrop = NavigationUtility.CalculateNearest(
				Agent.Transform.Position.Value,
				out var result,
				Game.ItemDrops.AllActive
					.Select(m => Navigation.QueryEntrances(m))
					.ToArray()
			);
		}

		class ToReturnOnTimeout : AgentTransition<CleanupItemDropsState<S>, S, GameModel, DwellerModel>
		{
			public override bool IsTriggered() => true;
		}		
	}
}