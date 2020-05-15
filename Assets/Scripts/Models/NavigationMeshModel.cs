using System;
using Lunra.Core;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public class NavigationMeshModel : Model
	{
		public enum CalculationStates
		{
			Unknown = 0,
			NotInitialized = 10,
			Queued = 20,
			Calculating = 30,
			Completed = 40
		}
		
		#region Non Serialized
		DateTime lastUpdated;
		[JsonIgnore] public ListenerProperty<DateTime> LastUpdated { get; }

		CalculationStates calculationState;
		[JsonIgnore] public ListenerProperty<CalculationStates> CalculationState;
		
		Transform root;
		[JsonIgnore] public ListenerProperty<Transform> Root;
		#endregion

		#region Events
		public event Action Initialize = ActionExtensions.Empty;
		#endregion

		public NavigationMeshModel()
		{
			LastUpdated = new ListenerProperty<DateTime>(value => lastUpdated = value, () => lastUpdated);
			CalculationState = new ListenerProperty<CalculationStates>(value => calculationState = value, () => calculationState);
			Root = new ListenerProperty<Transform>(value => root = value, () => root);
		}

		public void TriggerInitialize()
		{
			if (CalculationState.Value != CalculationStates.NotInitialized)
			{
				Debug.LogError("Unrecognized state upon initialization: "+CalculationState.Value);
				return;
			}

			if (Root.Value == null) throw new NullReferenceException(nameof(Root)+" cannot be null");
			
			Initialize();
		}

		public void QueueCalculation() => CalculationState.Value = CalculationStates.Queued;
	}
}