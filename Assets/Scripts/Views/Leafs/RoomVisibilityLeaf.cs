using System;
using System.Linq;
using Lunra.Core;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Lunra.Hothouse.Views
{
	public class RoomVisibilityLeaf : MonoBehaviour
	{
		public enum Conditions
		{
			Unknown = 0,
			DoorIndexActive = 10,
			DoorIndexInActive = 20
		}

		[Serializable]
		public struct ConditionEntry
		{
			// #region Serialized
// #pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
			public Conditions Condition;
			public int DoorIndex;
// #pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null
			// #endregion
		}

		public ConditionEntry[] AnyConditions = new ConditionEntry[0];
		public ConditionEntry[] AllConditions = new ConditionEntry[0];
		public ConditionEntry[] NoneConditions = new ConditionEntry[0];

		public bool CalculateVisibility(int[] activeDoorIndices)
		{
			bool calculate(ConditionEntry conditionEntry)
			{
				switch (conditionEntry.Condition)
				{
					case Conditions.DoorIndexActive:
						return activeDoorIndices.Contains(conditionEntry.DoorIndex);
					case Conditions.DoorIndexInActive:
						return !activeDoorIndices.Contains(conditionEntry.DoorIndex);
					default:
						Debug.LogError("Unrecognized condition: "+conditionEntry.Condition, gameObject);
						break;
				}

				return false;
			}
			
			bool? result = null;

			foreach (var condition in AnyConditions)
			{
				result = calculate(condition);
				if (result.Value) break;
			}

			foreach (var condition in AllConditions)
			{
				result = calculate(condition);
				if (!result.Value) break;
			}

			if (result.HasValue && !result.Value) return false;

			foreach (var condition in NoneConditions)
			{
				result = !calculate(condition);
				if (!result.Value) break;
			}

			return result ?? true;
		}

#if UNITY_EDITOR
		void OnDrawGizmosSelected()
		{
			if (Application.isPlaying) return;
			if (Selection.activeGameObject != gameObject) return;
			var rootRoom = transform.GetAncestor<RoomView>();
			if (rootRoom == null) return;
			if (rootRoom.DoorDefinitions == null) return;
			foreach (var door in rootRoom.DoorDefinitions)
			{
				if (door.Anchor == null) continue;
				Handles.Label(
					door.Anchor.position + Vector3.up,
					"Door_"+door.Index
				);
			}
		}
#endif
	}
}