using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;
using Lunra.StyxMvp;
using Lunra.StyxMvp.Models;
using Lunra.StyxMvp.Presenters;
using UnityEngine;

namespace Lunra.Hothouse.Presenters
{
	public class WorldCameraPresenter : Presenter<WorldCameraView>
	{
		GameModel game;
		WorldCameraModel camera;

		bool cameraTransformIsStale;
		
		public WorldCameraPresenter(GameModel game)
		{
			this.game = game;
			camera = game.WorldCamera;

			App.Heartbeat.LateUpdate += OnHeartbeatLateUpdate;
			game.SimulationInitialize += OnGameSimulationInitialize;
			game.WorldCamera.IsEnabled.Changed += OnWorldCameraIsEnabled;
			game.WorldCamera.Transform.Position.Changed += OnWorldCameraPosition;
			game.WorldCamera.Transform.Rotation.Changed += OnWorldCameraRotation;
			
			game.WorldCamera.CameraInstance.Value = View.CameraInstance;
			game.Interaction.Camera.Value = View.CameraInstance;

			App.Heartbeat.DrawGizmos += cleanup =>
			{
				// Debug.DrawLine(
				// 	View.CameraInstance.transform.position,
				// 	View.RootTransform.position,
				// 	Color.red
				// );
				//
				//
				//
				// Debug.DrawLine(
				// 	View.CameraInstance.transform.position,
				// 	View.RootTransform.position,
				// 	Color.red
				// );
			};
		}

		protected override void UnBind()
		{
			App.Heartbeat.LateUpdate -= OnHeartbeatLateUpdate;
			game.SimulationInitialize -= OnGameSimulationInitialize;
			game.WorldCamera.IsEnabled.Changed -= OnWorldCameraIsEnabled;
			game.WorldCamera.Transform.Position.Changed -= OnWorldCameraPosition;
			game.WorldCamera.Transform.Rotation.Changed -= OnWorldCameraRotation;
		}

		void Show()
		{
			if (View.Visible) return;
			
			View.Reset();

			View.Prepare += () => UpdateAndCalculateTransform(true);
			
			ShowView(instant: true);
		}

		void Close()
		{
			if (View.NotVisible) return;
			
			CloseView(true);
		}
		
		#region Heartbeat Events
		void OnHeartbeatLateUpdate()
		{
			if (View.NotVisible) return;
			
			// TODO: This should probably be handled by the GameInteractionPresenter...
			
			var pan = Vector3.zero;

			if (Input.GetKey(KeyCode.Comma)) pan += View.transform.forward;
			if (Input.GetKey(KeyCode.O)) pan -= View.transform.forward;
			if (Input.GetKey(KeyCode.A)) pan -= View.transform.right;
			if (Input.GetKey(KeyCode.E)) pan += View.transform.right;

			camera.Transform.Position.Value += (pan * (camera.PanVelocity.Value * Time.deltaTime));

			var orbit = 0f;
			
			if (Input.GetKey(KeyCode.Quote)) orbit += 1f;
			if (Input.GetKey(KeyCode.Period)) orbit -= 1f;

			camera.Transform.Rotation.Value *= Quaternion.Euler(Vector3.up * (orbit * camera.OrbitVelocity.Value * Time.deltaTime));

			UpdateAndCalculateTransform();
		}
		#endregion
		
		#region GameModel Events
		void OnGameSimulationInitialize()
		{
			OnWorldCameraIsEnabled(game.WorldCamera.IsEnabled.Value);
		}
		#endregion
		
		#region WorldCameraModel Events
		void OnWorldCameraIsEnabled(bool enabled)
		{
			if (enabled) Show();
			else Close();
		}

		void OnWorldCameraPosition(Vector3 position) => cameraTransformIsStale = true;

		void OnWorldCameraRotation(Quaternion rotation) => cameraTransformIsStale = true;
		#endregion
		
		#region Utility
		void UpdateAndCalculateTransform(bool forced = false)
		{
			if (!forced && !cameraTransformIsStale) return;
			
			cameraTransformIsStale = false;
			
			View.RootTransform.position = camera.Transform.Position.Value;
			View.RootTransform.rotation = camera.Transform.Rotation.Value;
			
			/*
			var ground = new Plane(Vector3.up, Vector3.zero);
			var ray = new Ray(View.CameraInstance.transform.position, View.CameraInstance.transform.forward);

			var groundIntersection = View.RootTransform.position;
			if (ground.Raycast(ray, out var distance)) groundIntersection = ray.origin + (ray.direction * distance);
			*/
		}
		#endregion
	}
}