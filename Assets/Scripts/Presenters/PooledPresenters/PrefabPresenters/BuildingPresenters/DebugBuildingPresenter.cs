using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Views;

namespace Lunra.WildVacuum.Presenters
{
	public class DebugBuildingPresenter : BuildingPresenter<BuildingModel, DebugBuildingView>
	{
		public DebugBuildingPresenter(GameModel game, BuildingModel model) : base(game, model) { }

		protected override void Bind()
		{
			base.Bind();

			Model.Inventory.Changed += OnBuildingInventory;
			Model.DesireQuality.Changed += OnBuildingDesireQuality;
		}

		protected override void UnBind()
		{
			base.UnBind();
			
			Model.Inventory.Changed -= OnBuildingInventory;
			Model.DesireQuality.Changed -= OnBuildingDesireQuality;
		}

		protected override void OnViewShown()
		{
			base.OnViewShown();
			
			OnRefreshDebugLabel();
		}

		#region Events
		void OnRefreshDebugLabel()
		{
			if (View.NotVisible) return;

			var result = string.Empty;

			if (Model.Inventory.Value.IsEmpty && Model.Inventory.Value.IsCapacityZero) result += "Inventory: Empty\n";
			else
			{
				result += "Inventory:\n";
				foreach (var itemMaximum in Model.Inventory.Value.Maximum.Where(m => 0 < m.Count))
				{
					var current = Model.Inventory.Value.GetCurrent(itemMaximum.Type);
					result += " > " + itemMaximum.Type + " : " + current + " / " + itemMaximum.Count + "\n";
				}
			}

			if (Model.DesireQuality.Value == null || Model.DesireQuality.Value.None()) result += "DesireQualities: None\n";
			else
			{
				result += "DesireQualities:\n";
				foreach (var kv in Model.DesireQuality.Value) result += " > " + kv.Key + " : " + kv.Value + "\n";
			}

			View.Text = result;
		}
		#endregion
		
		#region Building Events
		void OnBuildingInventory(Inventory inventory) => OnRefreshDebugLabel();

		void OnBuildingDesireQuality(Dictionary<Desires, float> desireQuality) => OnRefreshDebugLabel();
		#endregion
	}
}