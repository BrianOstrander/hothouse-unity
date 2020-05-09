using System;
using System.Linq;
using UnityEngine;
using Lunra.StyxMvp;
using Lunra.StyxMvp.Models;
using Lunra.StyxMvp.Services;
using Lunra.Core;
using Lunra.Hothouse.Models;

namespace Lunra.Hothouse.Services
{
	public class InitializePayload : IStatePayload
	{
		public PreferencesModel Preferences;
	}

	public class InitializeState : State<InitializePayload>
	{
		// Reminder: Keep variables in payload for easy reset of states!

		protected override void Begin()
		{
			App.S.PushBlocking(InitializePreferences);
		}

		protected override void Idle()
		{
			App.S.RequestState(
				new MainMenuPayload
				{
					Preferences = Payload.Preferences
				}
			);
			
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
			if (result.Status != ResultStatus.Success)
			{
				App.Restart("Listing preferences failed with status" + result.Status);
				return;
			}

			if (result.Length == 0)
			{
				App.M.Save(
					GeneratePreferences(), 
					saveResult => OnInitializePreferencesSaved(saveResult, done)
				);
			}
			else
			{
				var toLoad = result.Models.Where(p => p.SupportedVersion.Value).OrderBy(p => p.Version.Value).LastOrDefault();
				if (toLoad == null)
				{
					App.M.Save(
						GeneratePreferences(),
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
			if (result.Status != ResultStatus.Success)
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
			if (result.Status != ResultStatus.Success)
			{
				App.Restart("Saving preferences failed with status " + result.Status);
				return;
			}

			Payload.Preferences = result.TypedModel;
			
			done();
		}
		#endregion
		
		#region Utility

		PreferencesModel GeneratePreferences()
		{
			var preferences = App.M.Create<PreferencesModel>(App.M.CreateUniqueId());

			// TODO: Default preferences here...
			
			return preferences;
		}
		#endregion
	}
}