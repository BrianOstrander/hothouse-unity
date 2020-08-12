using System;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using Lunra.NumberDemon;
using UnityEngine;

namespace Lunra.Hothouse.Ai.Dweller
{
	public class FarmerState<S> : JobState<S, FarmerState<S>>
		where S : AgentState<GameModel, DwellerModel>
	{
		static readonly DayTime CalculateFloraObligationsDelay = DayTime.FromHours(12f);

		enum States
		{
			Unknown = 0,
			Idle = 10,
			NavigatingToSow = 20,
			Sowing = 30,
			NavigatingToTend = 40,
			Tending = 50
		}
		
		protected override Jobs Job => Jobs.Farmer;

		States state = States.Idle;
		TimeoutState timeoutInstance;
		Demon generator;
		float readyToSowRatio;
		
		public override void OnInitialize()
		{
			base.OnInitialize();
			
			generator = new Demon();

			AddChildStates(
				new InventoryRequestState(),
				new NavigateState(),
				new BalanceItemState(),
				new DestroyMeleeHandlerState(),
				timeoutInstance = new TimeoutState()
			);
			
			AddTransitions(
				new ToReturnOnJobChanged(),
				new ToReturnOnShiftEnd(),
				new ToReturnOnWorkplaceMissing(),
				new ToReturnOnWorkplaceIsNotNavigable(),

				new InventoryRequestState.ToInventoryRequestOnPromises(),
				
				new DestroyMeleeHandlerState.ToObligationOnExistingObligation(),
				new ToDestroyOvergrownFlora(),
				
				new BalanceItemState.ToBalanceOnAvailableDelivery((s, d) => d.Enterable.IsOwner),
				new BalanceItemState.ToBalanceOnAvailableDistribution((s, d) => s.Enterable.IsOwner),
				
				new ToTimeoutOnSow(),
				new ToNavigateToSow(),
				
				new ToTimeoutOnTending(),
				new ToNavigateToTend(),
				
				new ToNavigateToWorkplace()
			);
		}

		void CalculateReadyToSowRatio()
		{
			var readyToSowCount = 0f;
			var sownCount = 0f;

			foreach (var plot in Workplace.Farm.Plots)
			{
				switch (plot.State)
				{
					case FarmPlot.States.Invalid:
					case FarmPlot.States.Blocked:
						break;
					case FarmPlot.States.ReadyToSow:
						readyToSowCount++;
						break;
					case FarmPlot.States.Sown:
						sownCount++;
						break;
					default:
						Debug.LogError("Unrecognized state: "+plot.State);
						break;
				}
			}

			var total = readyToSowCount + sownCount;

			if (Mathf.Approximately(0f, total))
			{
				readyToSowRatio = 1f;
				return;
			}

			readyToSowRatio = readyToSowCount / total;
		}
		
		public override void Begin()
		{
			base.Begin();

			if (Workplace == null) return;
			if (string.IsNullOrEmpty(Workplace.Farm.SelectedFloraType)) return;
			
			if (Workplace.Farm.Plots.None() || (Game.NavigationMesh.CalculationState.Value == NavigationMeshModel.CalculationStates.Completed && Workplace.Farm.LastUpdatedRealTime < Game.NavigationMesh.LastUpdated.Value))
			{
				Workplace.Farm.CalculatePlots();
				CalculateReadyToSowRatio();
			}
			else if (state == States.Idle && CalculateFloraObligationsDelay < (Game.SimulationTime.Value - Workplace.Farm.LastUpdated))
			{
				Workplace.Farm.CalculateFloraObligations();
				CalculateReadyToSowRatio();
			}
		}

		public override void Idle()
		{
			if (state == States.Idle) return;

			state = States.Idle;
			Workplace.Farm.RemoveAttendingFarmer(Agent.Id.Value);
			CalculateReadyToSowRatio();
		}

		class ToDestroyOvergrownFlora : DestroyMeleeHandlerState.ToObligationHandlerOnAvailableObligation
		{
			bool isDestroyingFarmedFlora;
			
			public override bool IsTriggered()
			{
				isDestroyingFarmedFlora = SourceState.readyToSowRatio < SourceState.generator.NextFloat;
				return base.IsTriggered();
			}

			protected override bool IsObligationParentValid(IObligationModel obligationParent)
			{
				if (obligationParent is FloraModel obligationParentFlora && !obligationParentFlora.Farm.Value.IsNull)
				{
					return isDestroyingFarmedFlora && obligationParentFlora.Farm.Value.Id == SourceState.Workplace.Id.Value;
				}
				
				return SourceState.Workplace.Farm.Plots
					.Any(p => Vector3.Distance(obligationParent.Transform.Position.Value, p.Position) < p.Radius.Maximum);
			}
		}

		class ToTimeoutOnSow : AgentTransition<FarmerState<S>, TimeoutState, GameModel, DwellerModel>
		{
			FarmPlot selectedPlot;
			
			public override bool IsTriggered()
			{
				if (SourceState.state != States.NavigatingToSow) return false;

				selectedPlot = SourceState.Workplace.Farm.Plots
					.FirstOrDefault(
						p => p.State == FarmPlot.States.ReadyToSow && p.AttendingFarmer.Id == Agent.Id.Value && Vector3.Distance(p.Position, Agent.Transform.Position.Value) < (p.Radius.Minimum + Agent.InteractionRadius.Value)
					);

				if (selectedPlot == null)
				{
					// Debug.LogError("Navigating to sow, but nearest plot is null, this is unexpected");
					return false;
				}

				return true;
			}

			public override void Transition()
			{
				SourceState.state = States.Sowing;
				
				SourceState.timeoutInstance.ConfigureForInterval(
					DayTime.FromMinutes(15f),
					delta =>
					{
						if (delta.IsDone)
						{
							selectedPlot.State = FarmPlot.States.Sown;
							var flora = Game.Flora.Activate(
								Game.Flora.Definitions.First(d => d.Type == SourceState.Workplace.Farm.SelectedFloraType),
								selectedPlot.RoomId,
								selectedPlot.Position
							);

							flora.Farm.Value = InstanceId.New(SourceState.Workplace);
								
							flora.Tags.AddTag(Tags.Farm.Sown);
								
							selectedPlot.Flora = InstanceId.New(flora);
							
							selectedPlot.AttendingFarmer = InstanceId.Null();
							SourceState.state = States.Idle;
						}
					}
				);
			}
		}

		class ToNavigateToSow : AgentTransition<FarmerState<S>, NavigateState, GameModel, DwellerModel>
		{
			FarmPlot selectedPlot;
			Navigation.Result navigationResult;
		
			public override bool IsTriggered()
			{
				switch (SourceState.state)
				{
					case States.Idle:
					case States.NavigatingToSow:
						break;
					default:
						return false;
				}

				var nearestPlots = SourceState.Workplace.Farm.Plots
					.Where(p => p.State == FarmPlot.States.ReadyToSow && (p.AttendingFarmer.IsNull || p.AttendingFarmer.Id == Agent.Id.Value))
					.OrderBy(p => Vector3.Distance(p.Position, Agent.Transform.Position.Value));
				
				foreach (var plot in nearestPlots)
				{
					var isNavigable = NavigationUtility.CalculateNearest(
						Agent.Transform.Position.Value,
						out navigationResult,
						Navigation.QueryPosition(plot.Position, plot.Radius.Minimum)
					);

					if (isNavigable)
					{
						selectedPlot = plot;
						return true;
					}
				}
				
				return false;
			}

			public override void Transition()
			{
				selectedPlot.AttendingFarmer = InstanceId.New(Agent);
				SourceState.state = States.NavigatingToSow;
				Agent.NavigationPlan.Value = NavigationPlan.Navigating(
					navigationResult.Path,
					NavigationPlan.Interrupts.RadiusThreshold,
					Agent.InteractionRadius.Value
				);
			}
		}
		
		class ToTimeoutOnTending : AgentTransition<FarmerState<S>, TimeoutState, GameModel, DwellerModel>
		{
			FarmPlot selectedPlot;
			
			public override bool IsTriggered()
			{
				if (SourceState.state != States.NavigatingToTend) return false;

				selectedPlot = SourceState.Workplace.Farm.Plots
					.FirstOrDefault(
						p => p.State == FarmPlot.States.Sown && p.AttendingFarmer.Id == Agent.Id.Value && Vector3.Distance(p.Position, Agent.Transform.Position.Value) < (p.Radius.Minimum + Agent.InteractionRadius.Value)
					);

				if (selectedPlot == null)
				{
					// Debug.LogError("Navigating to sow, but nearest plot is null, this is unexpected");
					return false;
				}

				return true;
			}

			public override void Transition()
			{
				SourceState.state = States.Tending;
				
				SourceState.timeoutInstance.ConfigureForInterval(
					DayTime.FromMinutes(15f),
					delta =>
					{
						if (delta.IsDone)
						{
							if (selectedPlot.Flora.TryGetInstance<FloraModel>(Game, out var flora))
							{
								flora.Tags.AddTag(Tags.Farm.Tended, new DayTime(Game.SimulationTime.Value.Day + 1));
							}
							selectedPlot.AttendingFarmer = InstanceId.Null();
							SourceState.state = States.Idle;
						}
					}
				);
			}
		}
		
		class ToNavigateToTend : AgentTransition<FarmerState<S>, NavigateState, GameModel, DwellerModel>
		{
			FarmPlot selectedPlot;
			Navigation.Result navigationResult;
		
			public override bool IsTriggered()
			{
				switch (SourceState.state)
				{
					case States.Idle:
					case States.NavigatingToTend:
						break;
					default:
						return false;
				}
				
				var nearestPlots = SourceState.Workplace.Farm.Plots
					.Where(p => p.State == FarmPlot.States.Sown && (p.AttendingFarmer.IsNull || p.AttendingFarmer.Id == Agent.Id.Value))
					.OrderBy(p => Vector3.Distance(p.Position, Agent.Transform.Position.Value));

				foreach (var plot in nearestPlots)
				{
					var isNavigable = NavigationUtility.CalculateNearest(
						Agent.Transform.Position.Value,
						out navigationResult,
						Navigation.QueryPosition(plot.Position, plot.Radius.Minimum)
					);

					if (isNavigable)
					{
						if (plot.Flora.TryGetInstance<FloraModel>(Game, out var flora))
						{
							if (flora.Age.Value.IsDone) continue;
							if (flora.Tags.Contains(Tags.Farm.Tended)) continue;
							selectedPlot = plot;
							return true;
						}
					}
				}

				return false;
			}

			public override void Transition()
			{
				selectedPlot.AttendingFarmer = InstanceId.New(Agent);
				SourceState.state = States.NavigatingToTend;
				Agent.NavigationPlan.Value = NavigationPlan.Navigating(
					navigationResult.Path,
					NavigationPlan.Interrupts.RadiusThreshold,
					Agent.InteractionRadius.Value
				);
			}
		}

	}
}