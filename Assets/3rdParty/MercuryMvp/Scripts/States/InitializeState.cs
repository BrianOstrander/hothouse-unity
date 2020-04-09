using System;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;

using LunraGames.SubLight.Presenters;
using LunraGames.SubLight.Models;

namespace LunraGames.SubLight
{
	public class InitializePayload : IStatePayload
	{
		public List<GameObject> DefaultViews = new List<GameObject>();
	}

	public class InitializeState : State<InitializePayload>
	{
		// Reminder: Keep variables in payload for easy reset of states!

		protected override void Begin()
		{
			App.SM.PushBlocking(InitializeModels, "InitializeModels");
			App.SM.PushBlocking(InitializeViews, "InitializeViews");
			App.SM.PushBlocking(InitializePresenters, "InitializePresenters");
			App.SM.PushBlocking(InitializePreferences, "InitializePreferences");
			App.SM.PushBlocking(InitializeAudio, "InitializeAudio");

			// if (DevPrefs.WipeGameSavesOnStart) SM.PushBlocking(WipeGameSaves, "WipeGameSaves");
		}

		protected override void Idle()
		{
			Debug.Log("we got to the initialize idle...");
			// var mainCamera = (Payload.HomeStatePayload.MainCamera = new HoloRoomFocusCameraPresenter());

			App.P.AddGlobals(
				// mainCamera,
				// new GenericFocusCameraPresenter<PriorityFocusDetails>(mainCamera.GantryAnchor, mainCamera.FieldOfView)
			);

			// App.SM.RequestState(Payload.HomeStatePayload);
		}

		#region Mediators
		void InitializeModels(Action done)
		{
			App.M.Initialize(
				status =>
				{
					if (status == RequestStatus.Success) done();
					else App.Restart("Initializing ModelMediator failed with status " + status);
				}
			);
		}

		void InitializeViews(Action done)
		{
			App.V.Initialize(
				status =>
				{
					if (status == RequestStatus.Success) done();
					else App.Restart("Initializing ViewMediator failed with status " + status);
				}
			);
		}

		void InitializePresenters(Action done)
		{
			App.P.Initialize(
				status =>
				{
					if (status == RequestStatus.Success) done();
					else App.Restart("Initializing PresenterMediator failed with status " + status);
				}
			);
		}
		#endregion

		#region Preferences
		void InitializePreferences(Action done)
		{
			App.M.Index<PreferencesModel>(result => OnInitializePreferencesIndex(result, done));
		}

		void OnInitializePreferencesIndex(ModelIndexResult<SaveModel> result, Action done)
		{
			if (result.Status != RequestStatus.Success)
			{
				App.Restart("Listing preferences failed with status" + result.Status);
				return;
			}

			if (result.Length == 0)
			{
				App.M.Save(
					App.M.Create<PreferencesModel>(App.M.CreateUniqueId()), 
					saveResult => OnInitializePreferencesSaved(saveResult, done)
				);
			}
			else
			{
				var toLoad = result.Models.Where(p => p.SupportedVersion.Value).OrderBy(p => p.Version.Value).LastOrDefault();
				if (toLoad == null)
				{
					App.M.Save(
						App.M.Create<PreferencesModel>(App.M.CreateUniqueId()),
						saveResult => OnInitializePreferencesSaved(saveResult, done)
					);
				}
				else
				{
					App.M.Load<PreferencesModel>(
						toLoad,
						loadResult => OnInitializePreferencesLoad(loadResult, done)
					);
				}
			}
		}

		void OnInitializePreferencesLoad(ModelResult<PreferencesModel> result, Action done)
		{
			if (result.Status != RequestStatus.Success)
			{
				App.Restart("Loading preferences failed with status" + result.Status);
				return;
			}

			App.M.Save(
				result.TypedModel,
				saveResult => OnInitializePreferencesSaved(saveResult, done)
			);
		}

		void OnInitializePreferencesSaved(ModelResult<PreferencesModel> result, Action done)
		{
			if (result.Status != RequestStatus.Success)
			{
				App.Restart("Saving preferences failed with status " + result.Status);
				return;
			}

			App.SetPreferences(result.TypedModel);
			done();
		}
		#endregion

		void InitializeAudio(Action done)
		{
			App.Audio.Initialize(
				status =>
				{
					if (status == RequestStatus.Success) done(); 
					else App.Restart("Initializing Audio failed with status " + status);
				}
			);
		}
	}
}