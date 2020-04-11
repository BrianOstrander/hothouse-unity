using System;
using System.Linq;
using Lunra.StyxMvp;
using Lunra.StyxMvp.Models;
using Lunra.StyxMvp.Services;
using Lunra.WildVacuum.Models;
using UnityEngine;

namespace Lunra.WildVacuum.Services
{
	public class InitializePayload : IStatePayload { }

	public class InitializeState : State<InitializePayload>
	{
		// Reminder: Keep variables in payload for easy reset of states!

		protected override void Begin()
		{
			App.S.PushBlocking(InitializePreferences);
		}

		protected override void Idle()
		{
			Debug.Log("idle on initialize");
			
			/*
			var mainCamera = (Payload.HomeStatePayload.MainCamera = new HoloRoomFocusCameraPresenter());

			App.P.AddGlobals(
				mainCamera,
				new GenericFocusCameraPresenter<PriorityFocusDetails>(mainCamera.GantryAnchor, mainCamera.FieldOfView)
			);

			App.SM.RequestState(Payload.HomeStatePayload);
			
			App.SM.PushBlocking(
				() => Debug.Log("blocking..."),
				() => false
			);
			*/
		}

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

			// App.SetPreferences(result.TypedModel);
			Debug.LogWarning("Do something with these preferences...");
			done();
		}
		#endregion
	}
}