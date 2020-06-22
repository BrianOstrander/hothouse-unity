using System;
using System.Collections.Generic;
using Lunra.Core;
using Lunra.Hothouse.Models;
using Lunra.StyxMvp;
using UnityEngine;

namespace Lunra.Hothouse.Views
{
	public class JobManageView : View
	{
		#region Serialized
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
		[SerializeField] JobManageControlLeaf controlPrefab;
		[SerializeField] GameObject controlsRoot;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null
		#endregion

		#region Bindings
		public event Action<Jobs> IncreaseClick;
		public event Action<Jobs> DecreaseClick;
		
		public void InitializeJobs(params Jobs[] jobs)
		{
			entries.Clear();
			controlsRoot.transform.ClearChildren();

			foreach (var job in jobs)
			{
				var instance = controlsRoot.InstantiateChild(
					controlPrefab,
					setActive: true
				);

				instance.IncreaseClick += () => OnIncreaseClick(job);
				instance.DecreaseClick += () => OnDecreaseClick(job);
				
				entries.Add(job, instance);
			}	
		}
		
		public void UpdateJob(
			Jobs job,
			string name,
			int count,
			bool increaseEnabled,
			bool decreaseEnabled
		)
		{
			if (!entries.TryGetValue(job, out var entry))
			{
				Debug.LogError("No entry for job \""+job+"\" was bound");
				return;
			}

			entry.Name = name;
			entry.Count = count;
			entry.IncreaseEnabled = increaseEnabled;
			entry.DecreaseEnabled = decreaseEnabled;
			entry.ControlsEnabled = !(!increaseEnabled && !decreaseEnabled);
		}
		#endregion

		Dictionary<Jobs, JobManageControlLeaf> entries = new Dictionary<Jobs, JobManageControlLeaf>();
		
		public override void Cleanup()
		{
			base.Cleanup();

			controlPrefab.gameObject.SetActive(false);
			IncreaseClick = ActionExtensions.GetEmpty<Jobs>();
			DecreaseClick = ActionExtensions.GetEmpty<Jobs>();
			
			InitializeJobs();
		}

		#region Events
		void OnIncreaseClick(Jobs job) => IncreaseClick(job);
		void OnDecreaseClick(Jobs job) => DecreaseClick(job);
		#endregion
	}
 
}