using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using Lunra.NumberDemon;
using Lunra.StyxMvp.Models;
using UnityEngine;

namespace Lunra.Hothouse.Ai.SnapCap
{
	public class HuntState<S> : AgentState<GameModel, SnapCapModel>
		where S : AgentState<GameModel, SnapCapModel>
	{
		TimeoutState timeoutState;
		
		Demon generator = new Demon();
		InstanceId selectedPrey = InstanceId.Null();
		Attack selectedAttack;
		
		public override void OnInitialize()
		{
			AddChildStates(
				timeoutState = new TimeoutState(),
				new NavigateState(),
				new WanderState()
					.Configure(
						new WanderState.Configuration(
							() => 1,
							() => generator.GetNextFloat(2f, 8f),
							generator.GetNextRotation,
							() => Game.SimulationTime.Value
						)
					)
			);
			
			AddTransitions(
				new ToReturnOnHuntForbidden(),
				new ToReturnOnSleep(),
				
				new ToTimeoutToAttackPrey(),
				new ToNavigateToPrey(),
				
				new WanderState.ToWander()
			);
		}

		public class ToHuntOnAwake : AgentTransition<S, HuntState<S>, GameModel, SnapCapModel>
		{
			public override bool IsTriggered() => Agent.HuntForbiddenExpiration.Value < Game.SimulationTime.Value && Agent.AwakeTime.Value.Contains(Game.SimulationTime.Value);
		}
		
		class ToReturnOnHuntForbidden : AgentTransition<HuntState<S>, S, GameModel, SnapCapModel>
		{
			public override bool IsTriggered() => Game.SimulationTime.Value < Agent.HuntForbiddenExpiration.Value;
		}

		class ToReturnOnSleep : AgentTransition<HuntState<S>, S, GameModel, SnapCapModel>
		{
			public override bool IsTriggered() => !Agent.AwakeTime.Value.Contains(Game.SimulationTime.Value);
		}

		class ToTimeoutToAttackPrey : AgentTransition<HuntState<S>, TimeoutState, GameModel, SnapCapModel>
		{
			IHealthModel selectedPrey;
			
			public override bool IsTriggered()
			{
				if (SourceState.selectedAttack == null) return false;
				if (!SourceState.selectedPrey.TryGetInstance(Game, out selectedPrey) || selectedPrey.Health.IsDestroyed) return false;
				if (!SourceState.selectedAttack.Range.Contains(selectedPrey.DistanceTo(Agent))) return false;

				return true;
			}

			public override void Transition()
			{
				SourceState.timeoutState.ConfigureForInterval(SourceState.selectedAttack.Duration);
				
				var result = SourceState.selectedAttack.Trigger(
					Game,
					Agent,
					selectedPrey
				);

				if (result.IsTargetDestroyed)
				{
					SourceState.selectedPrey = null;
					SourceState.selectedAttack = null;
					Agent.HuntForbiddenExpiration.Value = Game.SimulationTime.Value + DayTime.FromHours(24f);
				}
			}
		}
		
		class ToNavigateToPrey : AgentTransition<HuntState<S>, NavigateState, GameModel, SnapCapModel>
		{
			Attack selectedAttack;
			IHealthModel selectedPrey;
			Navigation.Result navigationResult;
			
			public override bool IsTriggered()
			{
				var possiblePrey = Game.Dwellers.AllActive.Where(m => m.ShareRoom(Agent));
				if (possiblePrey.None()) return false;

				selectedAttack = null;
				selectedPrey = null;
				
				foreach (var prey in possiblePrey.OrderBy(m => m.DistanceTo(Agent)))
				{
					var isNavigable = NavigationUtility.CalculateNearest(
						Agent.Transform.Position.Value,
						out navigationResult,
						Navigation.QueryOrigin(prey)
					);

					if (isNavigable)
					{
						var attackFound = Agent.Attacks.TryGetMostEffective(
							prey,
							out selectedAttack,
							new FloatRange(
								Vector3.Distance(navigationResult.Target, prey.Transform.Position.Value),
								Agent.DistanceTo(prey)
							),
							Game.SimulationTime.Value + navigationResult.CalculateNavigationTime(Agent.NavigationVelocity.Value)
						);
						
						if (!attackFound) continue;
						
						selectedPrey = prey;
						break;
					}
				}

				if (selectedPrey == null) return false;

				return true;
			}

			public override void Transition()
			{
				SourceState.selectedPrey = selectedPrey.GetInstanceId();
				SourceState.selectedAttack = selectedAttack;

				var pathDistance = navigationResult.Path.corners.TotalDistance();
				
				if (Agent.NavigationPathMaximum.Value < pathDistance)
				{
					Agent.NavigationPlan.Value = NavigationPlan.Navigating(
						navigationResult.Path,
						NavigationPlan.Interrupts.PathElapsed,
						pathElapsed: pathDistance - Agent.NavigationPathMaximum.Value 
					);
				}
				else
				{
					Agent.NavigationPlan.Value = NavigationPlan.Navigating(
						navigationResult.Path,
						NavigationPlan.Interrupts.RadiusThreshold | NavigationPlan.Interrupts.LineOfSight,
						selectedAttack.Range.Maximum
					);
				}
			}
		}

		class TimeoutState : BaseTimeoutState<HuntState<S>, SnapCapModel> { }
		class NavigateState : BaseNavigateState<HuntState<S>, SnapCapModel> { }
		class WanderState : WanderState<HuntState<S>> { }
	}
}