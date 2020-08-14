using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public interface IClearableModel : IPrefabModel, IHealthModel, IObligationModel
	{
		ClearableComponent Clearable { get; }
	}

	public class ClearableComponent : ComponentModel<IClearableModel>
	{
		#region Serialized
		[JsonProperty] ClearableStates state;
		[JsonIgnore] public ListenerProperty<ClearableStates> State { get; }
		
		[JsonProperty] Inventory itemDrops = Inventory.Empty;
		[JsonIgnore] public ListenerProperty<Inventory> ItemDrops { get; }
		
		[JsonProperty] float meleeRangeBonus;
		[JsonIgnore] public ListenerProperty<float> MeleeRangeBonus { get; }
		#endregion
		
		#region NonSerialized 
		#endregion
		
		public ClearableComponent()
		{
			State = new ListenerProperty<ClearableStates>(value => state = value, () => state);
			ItemDrops = new ListenerProperty<Inventory>(value => itemDrops = value, () => itemDrops);
			MeleeRangeBonus = new ListenerProperty<float>(value => meleeRangeBonus = value, () => meleeRangeBonus);
		}

		public void Reset(Inventory itemDrops)
		{
			State.Value = ClearableStates.NotMarked;
			ItemDrops.Value = itemDrops;
		}

		public override void Bind()
		{
			Game.Toolbar.ClearanceTask.Changed += OnToolbarClearanceTask;
		}

		public override void UnBind()
		{
			Game.Toolbar.ClearanceTask.Changed -= OnToolbarClearanceTask;
		}

		#region Events
		void OnToolbarClearanceTask(Interaction.RoomVector3 interaction)
		{
			if (interaction.State == Interaction.States.OutOfRange) return;
			if (State.Value == ClearableStates.Marked) return;
			// if (Model.Obligations.HasAny(ObligationCategories.Destroy.Melee)) return;
			
			var radiusContains = interaction.Value.RadiusContains(Model.Transform.Position.Value);

			switch (interaction.State)
			{
				case Interaction.States.Idle:
					break;
				case Interaction.States.Begin:
				case Interaction.States.Active:
					State.Value = radiusContains ? ClearableStates.Highlighted : ClearableStates.NotMarked;
					break;
				case Interaction.States.End:
					State.Value = radiusContains ? ClearableStates.Marked : ClearableStates.NotMarked;
					// if (radiusContains) Model.Obligations.Add(ObligationCategories.Destroy.Melee);
					// Model.Clearable.SelectionState.Value = radiusContains ? SelectionStates.Selected : SelectionStates.NotSelected;
					break;
				case Interaction.States.Cancel:
					State.Value = ClearableStates.NotMarked;
					// Model.Clearable.SelectionState.Value = SelectionStates.NotSelected;
					break;
				default:
					Debug.LogError("Unrecognized Interaction.State: "+interaction.State);
					break;
			}
		}
		#endregion
	}
}