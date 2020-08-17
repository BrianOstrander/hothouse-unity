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
		bool shouldFlee;
		
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
				new ToNavigateToFlee(),
				
				new ToReturnOnHuntForbidden(),
				new ToReturnOnSleep(),
				
				new ToTimeoutToAttackPrey(),
				new ToNavigateToPrey(),
				
				new WanderState.ToWander()
			);
		}

		public override void End()
		{
			shouldFlee = false;
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

		class ToNavigateToFlee : AgentTransition<HuntState<S>, NavigateState, GameModel, SnapCapModel>
		{
			Navigation.Result navigationResult;
			
			public override bool IsTriggered()
			{
				if (!SourceState.shouldFlee) return false;

				var adjacentRoomIds = Game.Rooms.FirstActive(Agent.RoomTransform.Id.Value).AdjacentRoomIds.Value
					.Where(kv => kv.Value)
					.Select(kv => kv.Key)
					.Append(Agent.RoomTransform.Id.Value);

				var emptiestRoomDwellerCount = int.MaxValue;
				RoomModel emptiestRoom = null;
				
				foreach (var roomId in adjacentRoomIds)
				{
					var currentRoom = Game.Rooms.FirstActive(roomId);
					var currentDwellerCount = Game.Dwellers.AllActive.Count(m => m.IsInRoom(roomId));
					if (emptiestRoom == null || currentDwellerCount < emptiestRoomDwellerCount)
					{
						emptiestRoom = currentRoom;
						emptiestRoomDwellerCount = currentDwellerCount;
					}
				}

				if (emptiestRoom == null) return false;

				foreach (var door in Game.Doors.AllActive.Where(m => m.IsConnnecting(emptiestRoom.RoomTransform.Id.Value)).OrderByDescending(m => m.DistanceTo(Agent)))
				{
					var isNavigable = NavigationUtility.CalculateNearest(
						Agent.Transform.Position.Value,
						out navigationResult,
						Navigation.QueryEntrances(door, e => e.IsNavigable)
					);

					if (isNavigable) return true;
				}
				
				return false;
			}

			public override void Transition()
			{
				Agent.NavigationPlan.Value = NavigationPlan.Navigating(
					navigationResult.Path
				);
			}
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
				var result = SourceState.selectedAttack.Trigger(
					Game,
					Agent,
					selectedPrey
				);

				SourceState.timeoutState.ConfigureForInterval(
					SourceState.selectedAttack.Duration,
					progress =>
					{
						if (progress.IsDone && result.IsTargetDestroyed)
						{
							SourceState.shouldFlee = true;
							Agent.HuntForbiddenExpiration.Value = Game.SimulationTime.Value + DayTime.FromHours(24f);
						}
					}
				);
				
				if (result.IsTargetDestroyed)
				{
					SourceState.selectedPrey = null;
					SourceState.selectedAttack = null;
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
					if (Agent.HuntRangeMaximum.Value < Agent.DistanceTo(prey)) break;
					
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