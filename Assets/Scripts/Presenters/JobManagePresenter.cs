using System.Collections.Generic;
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
		GameModel game;
		JobManageModel jobManage;

		public JobManagePresenter(GameModel game)
		{
			this.game = game;
			jobManage = game.JobManage;

			game.Dwellers.All.Changed += OnGameDwellersAll;
			
			Show();
		}

		protected override void UnBind()
		{
			game.Dwellers.All.Changed -= OnGameDwellersAll;
		}

		void Show()
		{
			if (View.Visible) return;
			
			View.Reset();

			View.Prepare += UpdateControls;

			ShowView(instant: true);
		}
		
		#region GameModel Events
		void OnGameDwellersAll(DwellerPoolModel.Reservoir all) => UpdateControls();
		#endregion

		void UpdateControls()
		{
			if (View.NotVisible) return;

			var jobCounts = EnumExtensions.GetValues(Jobs.Unknown)
				.ToDictionary(
					k => k,
					k => 0
				);

			foreach (var dweller in game.Dwellers.AllActive)
			{
				if (dweller.Job.Value == Jobs.Unknown)
				{
					Debug.LogError("Unrecognized Job: " + dweller.Job.Value);
					continue;
				}

				jobCounts[dweller.Job.Value]++;
			}

			var res = string.Empty;
			foreach (var kv in jobCounts)
			{
				res += "\n" + kv.Key + ": " + kv.Value;
			}
			
			Debug.Log(res);
		}
	}
}