using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Models.AgentModels;
using Lunra.Hothouse.Views;
using UnityEngine;

namespace Lunra.Hothouse.Presenters
{
	public abstract class BaseBuildingPresenter<M, V> : PrefabPresenter<M, V>
		where M : BuildingModel
		where V : BuildingView
	{
		protected BaseBuildingPresenter(GameModel game, M model) : base(game, model) { }

		protected override void Bind()
		{
			base.Bind();

			if (View.Entrances.None())
			{
				Debug.LogError(
					"No entrances found for "+View.name+"."+StringExtensions.GetNonNullOrEmpty(View.PrefabId, "< null or empty PrefabId >")
				);
			}

			Model.Inventory.Changed += OnBuildingInventory;
			Model.Operate += OnBuildingOperate;
		}

		protected override void UnBind()
		{
			base.UnBind();

			Model.Inventory.Changed -= OnBuildingInventory;
			Model.Operate -= OnBuildingOperate;
		}

		protected override void OnInitialized()
		{
			OnBuildingInventory(Model.Inventory.Value);
		}

		#region Building Events
		void OnBuildingInventory(Inventory inventory)
		{
			var anyChanged = false;
			var newDesireQuality = Model.DesireQuality.Value.Select(
				d =>
				{
					var result = d.CalculateState(inventory);
					anyChanged |= d.State != result.State;
					return result;
				}
			).ToArray(); // Has to call ToArray otherwise anyChanged will never get called...
			
			if (anyChanged) Model.DesireQuality.Value = newDesireQuality;
		}
		
		void OnBuildingOperate(DwellerModel dweller, Desires desire)
		{
			var quality = Model.DesireQuality.Value.FirstOrDefault(d => d.Desire == desire);

			if (quality.Desire != desire)
			{
				Debug.LogError("Dweller "+dweller.Id.Value+" tried to operate desire "+desire+" on this building, but it doesn't fulfill that");
				return;
			}
			if (quality.State != DesireQuality.States.Available)
			{
				Debug.LogError("Dweller "+dweller.Id.Value+" tried to operate desire "+desire+" on this building, but its state is "+quality.State);
				return;
			}

			if (quality.Cost.IsEmpty) return;

			Model.Inventory.Value = Model.Inventory.Value.Subtract(quality.Cost);
		}
		#endregion
		
		#region View Events
		protected override void OnViewShown()
		{
			Model.Entrances.Value = View.Entrances.Select(e => new Entrance(e, Entrance.States.Available)).ToArray();
		}
		#endregion
	}
}