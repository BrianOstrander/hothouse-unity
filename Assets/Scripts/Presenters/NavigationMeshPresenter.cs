using System;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;
using Lunra.StyxMvp;
using Lunra.StyxMvp.Presenters;

namespace Lunra.Hothouse.Presenters
{
	public class NavigationMeshPresenter : Presenter<NavigationMeshView>
	{
		GameModel game;
		NavigationMeshModel navigationMesh;

		public NavigationMeshPresenter(GameModel game)
		{
			this.game = game;
			navigationMesh = game.NavigationMesh;

			App.Heartbeat.Update += OnHeartbeatUpdate;
			
			navigationMesh.CalculationState.Value = NavigationMeshModel.CalculationStates.NotInitialized;
			navigationMesh.Root.Value = View.RootTransform;
			
			navigationMesh.Initialize += OnNavigationMeshInitialize;
			
			Show();
		}

		protected override void Deconstruct()
		{
			App.Heartbeat.Update -= OnHeartbeatUpdate;
			
			navigationMesh.Initialize -= OnNavigationMeshInitialize;
		}
		
		void Show()
		{
			if (View.Visible) return;
			
			View.Cleanup();
			
			ShowView(instant: true);
		}

		void Close()
		{
			if (View.NotVisible) return;
			
			CloseView(true);
		}
		
		#region Heartbeat Events
		void OnHeartbeatUpdate()
		{
			if (navigationMesh.CalculationState.Value != NavigationMeshModel.CalculationStates.Queued) return;

			navigationMesh.LastUpdated.Value = DateTime.Now;
			navigationMesh.CalculationState.Value = NavigationMeshModel.CalculationStates.Calculating;
			
			View.RebuildSurfaces(
				() => navigationMesh.CalculationState.Value = NavigationMeshModel.CalculationStates.Completed
			);
		}
		#endregion

		#region NavigationMeshModel Events
		void OnNavigationMeshInitialize()
		{
			if (View.NotVisible) throw new Exception("Trying to initialize NavigationMesh but the view is not visible");

			navigationMesh.CalculationState.Value = NavigationMeshModel.CalculationStates.Queued;
		}
		#endregion
	}
}