using Lunra.Hothouse.Models;
using UnityEngine;

namespace Lunra.Hothouse.Ai.Dweller
{
	public class ConstructAssembleHandlerState<S> : ObligationHandlerState<S, ConstructAssembleHandlerState<S>>
		where S : AgentState<GameModel, DwellerModel>
	{
		static readonly Obligation[] DefaultObligationsHandled =
		{
			ObligationCategories.Construct.Assemble
		};

		public override Obligation[] ObligationsHandled => DefaultObligationsHandled;

		public override void OnInitialize()
		{
			AddChildStates(
				new NavigateState(),
				TimeoutInstance = new TimeoutState()
			);
			
			AddTransitions(
				new ToReturnOnMissingObligation(),
				new ToReturnOnTimeout(),
				
				new ToTimeoutOnAssembleTarget(),
				
				new ToNavigateToTarget()
			);
		}

		class ToTimeoutOnAssembleTarget : ToTimeoutOnTarget
		{
			protected override bool CanPopObligation
			{
				get
				{
					switch (SourceState.CurrentCache.TargetParent)
					{
						case BuildingModel buildingModel:
							return buildingModel.IsBuildingState(BuildingStates.Constructing);
						default:
							Debug.LogError("Unrecognized target parent type: "+SourceState.CurrentCache.TargetParent.GetType());
							return false;
					}
				}
			}
		}
	}
}