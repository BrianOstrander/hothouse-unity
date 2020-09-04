using System.Linq;
using Lunra.Core;
using Lunra.Satchel;
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

		[JsonProperty] Stack[] itemDrops = new Stack[0];
		[JsonIgnore] public ListenerProperty<Stack[]> ItemDrops { get; }
		
		[JsonProperty] float meleeRangeBonus;
		[JsonIgnore] public ListenerProperty<float> MeleeRangeBonus { get; }
		
		[JsonProperty] Obligation markedObligation;
		[JsonIgnore] public ListenerProperty<Obligation> MarkedObligation { get; }
		[JsonProperty] int maximumClearers;
		[JsonIgnore] public ListenerProperty<int> MaximumClearers { get; }
		#endregion
		
		#region NonSerialized
		#endregion
		
		public ClearableComponent()
		{
			State = new ListenerProperty<ClearableStates>(value => state = value, () => state);
			ItemDrops = new ListenerProperty<Stack[]>(value => itemDrops = value, () => itemDrops);
			MeleeRangeBonus = new ListenerProperty<float>(value => meleeRangeBonus = value, () => meleeRangeBonus);
			MarkedObligation = new ListenerProperty<Obligation>(value => markedObligation = value, () => markedObligation);
			MaximumClearers = new ListenerProperty<int>(value => maximumClearers = value, () => maximumClearers);
		}

		public void Reset(
			Stack[] itemDrops,
			Obligation markedObligation = null,
			int maximumClearers = 1
		)
		{
			State.Value = ClearableStates.NotMarked;
			ItemDrops.Value = itemDrops;
			MarkedObligation.Value = markedObligation ?? ObligationCategories.Destroy.Generic;
			MaximumClearers.Value = maximumClearers;
		}

		public override void Bind()
		{
			Game.Toolbar.CancelTask.Changed += OnToolbarCancelTask;
			Game.Toolbar.ClearanceTask.Changed += OnToolbarClearanceTask;
			Model.Health.Destroyed += OnHealthDestroyed;
			State.Changed += OnState;
		}

		public override void UnBind()
		{
			Game.Toolbar.CancelTask.Changed -= OnToolbarCancelTask;
			Game.Toolbar.ClearanceTask.Changed -= OnToolbarClearanceTask;
			Model.Health.Destroyed -= OnHealthDestroyed;
			State.Changed -= OnState;
		}

		#region Events
		void OnState(ClearableStates state)
		{
			switch (state)
			{
				case ClearableStates.NotMarked:

					if (Model.Obligations.HasAny(MarkedObligation.Value))
					{
						foreach (var obligationPromiseParent in Game.Query.All<IObligationPromiseModel>(m => m.ObligationPromises.HasAny()))
						{
							if (obligationPromiseParent.ObligationPromises.All.TryPeek(out var currentPromise))
							{
								if (currentPromise.Obligation.Is(MarkedObligation.Value)) obligationPromiseParent.ObligationPromises.BreakPromise(); 
							}
						}
						
						while (Model.Obligations.HasAny(MarkedObligation.Value)) Model.Obligations.RemoveAny(MarkedObligation.Value);
					}
					
					break;
				case ClearableStates.Highlighted:
					break;
				case ClearableStates.Marked:
					for (var i = 0; i < MaximumClearers.Value; i++) Model.Obligations.Add(MarkedObligation.Value);
					break;
				default:
					Debug.LogError("Unrecognized state: " + state);
					break;
			}
		}
		
		void OnToolbarCancelTask(Interaction.RoomVector3 interaction)
		{
			if (interaction.State == Interaction.States.OutOfRange) return;
			if (State.Value == ClearableStates.NotMarked) return;
			
			var radiusContains = interaction.Value.RadiusContains(Model.Transform.Position.Value);

			switch (interaction.State)
			{
				case Interaction.States.Idle:
					break;
				case Interaction.States.Begin:
				case Interaction.States.Active:
					State.Value = radiusContains ? ClearableStates.Highlighted : ClearableStates.Marked;
					break;
				case Interaction.States.End:
					State.Value = radiusContains ? ClearableStates.NotMarked : ClearableStates.Marked;
					break;
				case Interaction.States.Cancel:
					State.Value = ClearableStates.Marked;
					break;
				default:
					Debug.LogError("Unrecognized Interaction.State: "+interaction.State);
					break;
			}
		}
		
		void OnToolbarClearanceTask(Interaction.RoomVector3 interaction)
		{
			if (interaction.State == Interaction.States.OutOfRange) return;
			if (State.Value == ClearableStates.Marked) return;
			
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
					break;
				case Interaction.States.Cancel:
					State.Value = ClearableStates.NotMarked;
					break;
				default:
					Debug.LogError("Unrecognized Interaction.State: "+interaction.State);
					break;
			}
		}

		void OnHealthDestroyed(Damage.Result result)
		{
			if (Model.Clearable.ItemDrops.Value.Any())
			{
				Game.ItemDrops.Activate(
					Model.RoomTransform.Id.Value,
					Model.Transform.Position.Value,
					Quaternion.identity,
					ItemDrops.Value
				);
			}
		}
		#endregion
	}
}