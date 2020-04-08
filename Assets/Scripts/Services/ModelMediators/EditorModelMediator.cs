#if UNITY_EDITOR
using System.Linq;
using System.Collections.Generic;
using LunraGames.SubLight.Models;
using UnityEngine;

namespace LunraGames.SubLight
{
	public class EditorModelMediator : DesktopModelMediator 
	{
		public enum ValidationStates
		{
			Unknown = 0,
			Processing = 10,
			Valid = 20,
			Invalid = 30
		}
		
		static EditorModelMediator instance;
		public static EditorModelMediator Instance
		{
			get
			{
				if (instance == null)
				{
					instance = new EditorModelMediator(true);
					instance.Initialize(instance.OnInstanceInitialized);
				}
				return instance;
			}
		}

		void OnInstanceInitialized(RequestStatus status)
		{
			switch (status)
			{
				case RequestStatus.Success: break;
				default:
					Debug.LogError("Editor time save load service returned: " + status);
					return;
			}
		}

		protected override bool SuppressErrorLogging => true;
		
		Dictionary<SaveTypes, bool> CanSaveOverrides
		{
			get
			{
				return new Dictionary<SaveTypes, bool>
				{
					{ SaveTypes.EncounterInfo, true },
					{ SaveTypes.GalaxyInfo, true },
					{ SaveTypes.GamemodeInfo, true },
					{ SaveTypes.ModuleTrait, true }
					// --
				};
			}
		}

		protected override Dictionary<SaveTypes, bool> CanSave
		{
			get
			{
				var dict = base.CanSave;
				var overrideDict = CanSaveOverrides;

				foreach (var kv in overrideDict)
				{
					if (dict.ContainsKey(kv.Key)) dict[kv.Key] = kv.Value;
					else dict.Add(kv.Key, kv.Value);
				}

				return dict;
			}
		}

		public EditorModelMediator(bool readableSaves = false) : base(readableSaves) {}
	}
}
#endif