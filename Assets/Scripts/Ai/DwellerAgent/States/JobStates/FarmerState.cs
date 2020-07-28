using System;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using UnityEngine;

namespace Lunra.Hothouse.Ai.Dweller
{
	public class FarmerState<S> : JobState<S, FarmerState<S>>
		where S : AgentState<GameModel, DwellerModel>
	{
		const float CalculateFloraObligationsDelay = 3f;

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
		
		public override void OnInitialize()
		{
			base.OnInitialize();
			
			AddChildStates(
				new CleanupState(),
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
				
				new ToTimeoutOnSow(),
				new ToNavigateToSow(),
				
				new ToTimeoutOnTending(),
				new ToNavigateToTend(),
				
				new ToNavigateToWorkplace(),
				
				new BalanceItemState.ToBalanceOnAvailableDelivery((s, d) => d.Enterable.IsOwner),
				new BalanceItemState.ToBalanceOnAvailableDistribution((s, d) => s.Enterable.IsOwner),
				
				new CleanupState.ToCleanupOnItemsAvailable()
			);
		}

		public override void Idle()
		{
			if (Workplace == null) return;

			if (Workplace.Farm.SelectedSeed != Inventory.Types.Unknown)
			{
				if (Workplace.Farm.Plots.None() || (Game.NavigationMesh.CalculationState.Value == NavigationMeshModel.CalculationStates.Completed && Workplace.Farm.LastUpdated < Game.NavigationMesh.LastUpdated.Value))
				{
					Workplace.Farm.CalculatePlots(Game, Workplace);
				}
				else if (CalculateFloraObligationsDelay < (DateTime.Now - Workplace.Farm.LastUpdated).TotalSeconds)
				{
					Workplace.Farm.CalculateFloraObligations(Game, Workplace);
				}
			}
		}

		class ToDestroyOvergrownFlora : DestroyMeleeHandlerState.ToObligationHandlerOnAvailableObligation
		{
			protected override bool IsObligationParentValid(IObligationModel obligationParent)
			{
				if (obligationParent is FloraModel obligationParentFlora && !obligationParentFlora.Farm.Value.IsNull)
				{
					return obligationParentFlora.Farm.Value.Id == SourceState.Workplace.Id.Value;
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
						p => p.State == FarmPlot.States.ReadyToSow && p.AttendingFarmer.Id == Agent.Id.Value && Vector3.Distance(p.Position, Agent.Transform.Position.Value) < (p.Radius.Minimum + Agent.MeleeRange.Value)
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
					DayTime.FromHours(1f),
					delta =>
					{
						if (delta.IsDone)
						{
							var seedInventory = Inventory.FromEntry(SourceState.Workplace.Farm.SelectedSeed, 1);

							if (Agent.Inventory.All.Value.Intersects(seedInventory))
							{
								selectedPlot.State = FarmPlot.States.Sown;
								Agent.Inventory.Remove(seedInventory);
								var flora = Game.Flora.Activate(
									Game.Flora.Definitions.First(d => d.Seed == SourceState.Workplace.Farm.SelectedSeed),
									selectedPlot.RoomId,
									selectedPlot.Position
								);

								flora.Farm.Value = InstanceId.New(SourceState.Workplace);
								
								flora.Tags.AddTag(Tags.Farm.Sown);
								
								selectedPlot.Flora = InstanceId.New(flora);
							}
							else
							{
								selectedPlot.State = FarmPlot.States.ReadyToSow;
							}
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
				if (SourceState.state != States.Idle) return false;
				if (Agent.Inventory.IsFull()) return false;
				if (SourceState.Workplace.Inventory.Available.Value[SourceState.Workplace.Farm.SelectedSeed] == 0) return false;

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
				Agent.NavigationPlan.Value = NavigationPlan.Navigating(navigationResult.Path);
				var seedInventory = Inventory.FromEntry(SourceState.Workplace.Farm.SelectedSeed, 1);
				SourceState.Workplace.Inventory.Remove(seedInventory);
				Agent.Inventory.Add(seedInventory);
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
						p => p.State == FarmPlot.States.Sown && p.AttendingFarmer.Id == Agent.Id.Value && Vector3.Distance(p.Position, Agent.Transform.Position.Value) < (p.Radius.Minimum + Agent.MeleeRange.Value)
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
					DayTime.FromHours(1f),
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
				if (SourceState.state != States.Idle) return false;
				
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
						if (plot.Flora.TryGetInstance<FloraModel>(Game, out var flora) && !flora.Tags.Containts(Tags.Farm.Tended))
						{
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
				Agent.NavigationPlan.Value = NavigationPlan.Navigating(navigationResult.Path);
			}
		}

	}
}