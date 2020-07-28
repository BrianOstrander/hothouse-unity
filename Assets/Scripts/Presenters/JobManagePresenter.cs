using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;
using Lunra.StyxMvp.Presenters;
using UnityEngine;

namespace Lunra.Hothouse.Presenters
{
	public class JobManagePresenter : Presenter<JobManageView>
	{
		static readonly Jobs[] ValidJobs = EnumExtensions.GetValues(Jobs.Unknown, Jobs.None)
			.Append(Jobs.None)
			.ToArray();
		
		GameModel game;
		JobManageModel jobManage;

		public JobManagePresenter(GameModel game)
		{
			this.game = game;
			jobManage = game.JobManage;

			game.SimulationInitialize += OnGameSimulationInitialized;
			
			game.Dwellers.All.Changed += OnGameDwellersAll;
			
			Show();
		}

		protected override void UnBind()
		{
			game.SimulationInitialize -= OnGameSimulationInitialized;
			
			game.Dwellers.All.Changed -= OnGameDwellersAll;
		}

		void Show()
		{
			if (View.Visible) return;
			
			View.Cleanup();

			View.IncreaseClick += OnViewIncreaseClick;
			View.DecreaseClick += OnViewDecreaseClick;
			
			View.InitializeJobs(ValidJobs);

			View.Prepare += UpdateJobs;
			
			ShowView(instant: true);
		}
		
		#region View Events
		void OnViewIncreaseClick(Jobs job)
		{
			var target = game.Dwellers
				.FirstOrDefaultActive(m => m.Job.Value == Jobs.None);
			
			if (target == null)
			{
				Debug.LogError("Tried to increase number of dwellers assigned to "+job+" but no unassigned dwellers were found");
				return;
			}

			target.Job.Value = job;
			
			UpdateJobs();
		}
		
		void OnViewDecreaseClick(Jobs job)
		{
			var target = game.Dwellers
				.FirstOrDefaultActive(m => m.Job.Value == job);
			
			if (target == null)
			{
				Debug.LogError("Tried to decrease number of dwellers assigned to "+job+" but no existing assignees were found");
				return;
			}

			target.Job.Value = Jobs.None;

			if (!target.Workplace.Value.IsNull)
			{
				if (target.Workplace.Value.TryGetInstance<IClaimOwnershipModel>(game, out var workplace)) workplace.Ownership.Remove(target);
				target.Workplace.Value = InstanceId.Null();
			}
			
			UpdateJobs();
		}
		#endregion
		
		#region GameModel Events
		void OnGameSimulationInitialized() => UpdateJobs();
		void OnGameDwellersAll(DwellerPoolModel.Reservoir all) => UpdateJobs();
		#endregion
		
		#region Utility
		void UpdateJobs()
		{
			if (View.NotVisible) return;

			var unassignedCount = game.Dwellers.AllActive.Count(m => m.Job.Value == Jobs.None);
			var increaseEnabled = 0 < unassignedCount;
			
			foreach (var job in ValidJobs)
			{
				var count = game.Dwellers.AllActive.Count(m => m.Job.Value == job);
				View.UpdateJob(
					job,
					job.ToString(),
					count,
					job != Jobs.None && increaseEnabled,
					job != Jobs.None && 0 < count
				);
			}
		}
		#endregion
	}
}