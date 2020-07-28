using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using UnityEngine;
using UnityEngine.AI;

namespace Lunra.Hothouse.Ai.Seeker
{
	public class HuntState : AgentState<GameModel, SeekerModel>
	{
		struct Cache
		{
			public bool IsCached;
			public IEnumerable<string> AdjacentRoomIds;
			public IEnumerable<IHealthModel> Targets;

			public Cache(GameModel game, AgentModel agent)
			{
				IsCached = true;

				AdjacentRoomIds = game.Rooms
					.FirstActive(agent.RoomTransform.Id.Value).AdjacentRoomIds.Value
					.Where(kv => kv.Value)
					.Select(kv => kv.Key);

				Targets = game.Dwellers.AllActive
					.Concat<IHealthModel>(
						game.Buildings.AllActive
							.Where(m => m.IsBuildingState(BuildingStates.Operating))
					)
					.ToArray();
			}
		}
		
		public override string Name => "Hunt";

		TimeoutState<HuntState> timeoutState;
		
		public override void OnInitialize()
		{
			AddChildStates(
				new NavigateState<HuntState>(),
				timeoutState = new TimeoutState<HuntState>()
			);
			
			AddTransitions(
				new ToTimeoutOnAttack(),
				new ToNavigateToTarget(),
				new AgentTransitionFallthrough<TimeoutState<HuntState>, GameModel, SeekerModel>(
					"NoTargetTimeout",
					transition: () => timeoutState.ConfigureForInterval(DayTime.FromHours(8f))
				)
			);
		}

		// public override void Begin() => ResetCache();
		public override void Idle() => ResetCache();
		public override void End() => ResetCache();

		void ResetCache() => currentCache = default;

		Cache currentCache;
		Cache CurrentCache
		{
			get
			{
				if (currentCache.IsCached) return currentCache;
				
				currentCache = new Cache(Game, Agent);

				return currentCache;
			}
		}
		
		protected class ToTimeoutOnAttack : AgentTransition<HuntState, TimeoutState<HuntState>, GameModel, SeekerModel>
		{
			IEnumerable<IHealthModel> GetTargets(Cache cache)
			{
				return cache.Targets
					.Where(
						m =>
						{
							var range = Agent.DamageRange.Value;

							if (m is IBoundaryModel b) range += b.Boundary.Radius.Value;
							
							if (range < Vector3.Distance(m.Transform.Position.Value, Agent.Transform.Position.Value)) return false;
							if (Agent.RoomTransform.Id.Value == m.RoomTransform.Id.Value) return true;
							return cache.AdjacentRoomIds.Any(roomId => roomId == m.RoomTransform.Id.Value);
						}
					);
			}
			
			public override bool IsTriggered() => GetTargets(SourceState.CurrentCache).Any();

			public override void Transition()
			{
				SourceState.timeoutState.ConfigureForInterval(
					DayTime.FromHours(1f),
					OnTimeoutUpdate
				);
			}

			void OnTimeoutUpdate(
				(
					float Progress,
					bool IsDone
				) delta
			)
			{
				if (!delta.IsDone) return;

				var targets = GetTargets(new Cache(Game, Agent));
				
				var res = "ToAttack:";
				foreach (var target in targets)
				{
					res += "\n - " + target.ShortId;
					
					Damage.ApplyGeneric(
						Agent.DamageAmount.Value,
						target
					);
				}
				Debug.Log(res);
				
				Damage.ApplyGeneric(
					Agent.Health.Current.Value,
					Agent
				);
			}
		}
		
		class ToNavigateToTarget : AgentTransition<HuntState, NavigateState<HuntState>, GameModel, SeekerModel>
		{
			NavMeshPath path;
			
			public override bool IsTriggered()
			{
				if (SourceState.CurrentCache.Targets.None()) return false;
				
				var foundTarget = NavigationUtility.CalculateNearest(
					Agent.Transform.Position.Value,
					out var result,
					SourceState.CurrentCache.Targets
						.Select(
							m =>
							{
								if (m is IBoundaryModel mBoundary) return Navigation.QueryInRadius(mBoundary, Agent.DamageRange.Value);
								return Navigation.QueryOrigin(m);
							}
						)
						.ToArray()
				);

				if (!foundTarget) return false;

				path = result.Path;
				return true;
			}

			public override void Transition() => Agent.NavigationPlan.Value = NavigationPlan.Navigating(path);
		}
	}
}