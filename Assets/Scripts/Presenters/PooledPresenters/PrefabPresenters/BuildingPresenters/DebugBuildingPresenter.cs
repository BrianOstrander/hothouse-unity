using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;

namespace Lunra.Hothouse.Presenters
{
	public class DebugBuildingPresenter : BaseBuildingPresenter<BuildingModel, DebugBuildingView>
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
			/*
			if (View.NotVisible) return;

			var result = string.Empty;

			if (Model.Inventory.Value.IsEmpty && Model.Inventory.Value.AllMaximumsZero) result += "Inventory: Empty\n";
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
				foreach (var desire in Model.DesireQuality.Value)
				{
					switch (desire.State)
					{
						case DesireQuality.States.NotAvailable:
							result += "<color=red>";
							break;
						case DesireQuality.States.Unknown:
							result += "<color=yellow>";
							break;
							
					}
					
					result += " > " + desire.Desire + " : " + desire.Quality + "\n";
					
					switch (desire.State)
					{
						case DesireQuality.States.NotAvailable:
							result += "</color>";
							break;
						case DesireQuality.States.Unknown:
							result += "</color>";
							break;
					}
				}
			}

			View.Text = result;
			*/
			View.Text = "TODO";
		}
		#endregion
		
		#region Building Events
		void OnBuildingInventory(Inventory inventory) => OnRefreshDebugLabel();

		void OnBuildingDesireQuality(DesireQuality[] desireQuality) => OnRefreshDebugLabel();
		#endregion
	}
}