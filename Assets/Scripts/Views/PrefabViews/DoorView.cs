using System;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using UnityEngine;
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
#endif

namespace Lunra.Hothouse.Views
{
	public class DoorView : PrefabView, IEnterableView, IRoomIdView
	{
		static class Constants
		{
			public const int EntranceCountPerAnchor = 3;
			public const float EntrancePlacementLimit = 0.33f;
			public const float EntranceForwardOffset = 1.25f;
			public const string DoorFramePrefix = "door_frame_";
			public const string DoorMovementPrefix = "door_movement";
			public const string DoorAnchorPrefix = "door_anchor";
			public const string DoorEntranceRoot = "entrances";
			public const char DoorAnchorSizeTerminator = 'm';
		}
		
		#region Serialized
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
		[SerializeField] GameObject door;
		[SerializeField] GameObject entrancesRoot;
		[SerializeField] Transform[] entrances = new Transform[0];
		[SerializeField] DoorCache[] doorDefinitions = new DoorCache[0];
		[SerializeField] int doorSize;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null
		#endregion
		
		#region Bindings
		public bool IsOpen { set => door.SetActive(!value); }
		public event Action<bool> Highlight;
		public event Action Click;
		#endregion
		
		#region Reverse Bindings
		public GameObject EntrancesRoot
		{
			get => entrancesRoot;
			set => entrancesRoot = value;
		}

		public Transform[] Entrances
		{
			get => entrances;
			set => entrances = value;
		}
		public DoorCache[] DoorDefinitions => doorDefinitions;
		#endregion

		public override void Cleanup()
		{
			base.Cleanup();

			IsOpen = false;
			
			Highlight = null;
			Click = null;
		}
		
		#region Events
		public void OnPointerEnter() => Highlight?.Invoke(true);
		public void OnPointerExit() => Highlight?.Invoke(false);
		public void OnPointerClick() => Click?.Invoke();
		#endregion
		
#if UNITY_EDITOR
		protected override void OnCalculateCachedData()
		{
			PrefabId = gameObject.name;

			var doorAnchor = transform.GetFirstDescendantOrDefault(d => d.gameObject.activeInHierarchy && !string.IsNullOrEmpty(d.name) && d.name.StartsWith(Constants.DoorFramePrefix));

			if (doorAnchor == null)
			{
				Debug.LogError("Unable to find door frame");
				return;
			}

			var doorAnchorSizeSerialized = doorAnchor.name.Substring(Constants.DoorFramePrefix.Length, doorAnchor.name.Length - Constants.DoorFramePrefix.Length);
			
			if (string.IsNullOrEmpty(doorAnchorSizeSerialized) || !doorAnchorSizeSerialized.Contains(Constants.DoorAnchorSizeTerminator))
			{
				Debug.LogError("Unable to parse door size from: "+doorAnchor.name, doorAnchor);
				return;
			}

			doorAnchorSizeSerialized = doorAnchorSizeSerialized.Split(Constants.DoorAnchorSizeTerminator).FirstOrDefault();

			if (string.IsNullOrEmpty(doorAnchorSizeSerialized))
			{
				Debug.LogError("Unable to parse door size before terminator from: "+doorAnchor.name, doorAnchor);
				return;
			}

			if (!int.TryParse(doorAnchorSizeSerialized, out var doorAnchorSize))
			{
				Debug.LogError("Unable to parse door size from: "+doorAnchor.name, doorAnchor);
				return;
			}

			doorSize = doorAnchorSize;

			var doorDefinitionsList = new List<DoorCache>();
			var doorId = 0;
			
			foreach (var door in transform.GetDescendants(d => !string.IsNullOrEmpty(d.name) && d.name.StartsWith(Constants.DoorAnchorPrefix)))
			{
				if (!door.gameObject.activeInHierarchy) continue;
				
				doorDefinitionsList.Add(
					new DoorCache
					{
						Index = doorId,
						Anchor = door,
						Size = doorSize
					}
				);
				
				doorId++;
			}

			doorDefinitions = doorDefinitionsList.ToArray();

			var doorMovement = transform.GetFirstDescendantOrDefault(d => !string.IsNullOrEmpty(d.name) && d.name.StartsWith(Constants.DoorMovementPrefix));

			if (doorMovement == null) Debug.LogError("Unable to find door movement");
			else door = doorMovement.gameObject;
			
			if (EntrancesRoot != null) DestroyImmediate(EntrancesRoot);
			
			EntrancesRoot = new GameObject(Constants.DoorEntranceRoot);
			EntrancesRoot.transform.SetParent(RootTransform);

			var entrancesList = new List<Transform>();
			
			foreach (var door in doorDefinitions)
			{
				var edge0 = door.Anchor.position + (door.Anchor.right * (door.Size * Constants.EntrancePlacementLimit));
				var edge1 = door.Anchor.position + (door.Anchor.right * (door.Size * -Constants.EntrancePlacementLimit));
				
				for (var i = 0; i < Constants.EntranceCountPerAnchor; i++)
				{
					var entrance = new GameObject("entrance_" + i);
					entrance.transform.SetParent(EntrancesRoot.transform);
					var progress = i / (Constants.EntranceCountPerAnchor - 1f);
					entrance.transform.position += (door.Anchor.forward * Constants.EntranceForwardOffset) + Vector3.Lerp(edge0, edge1, progress);
					entrance.transform.forward = door.Anchor.forward;
					
					entrancesList.Add(entrance.transform);
				}
			}

			entrances = entrancesList.ToArray();
			
			NormalizeMeshCollidersFromRoot();
		}
		
		void OnDrawGizmosSelected()
		{
			ViewGizmos.DrawDoorGizmo(doorDefinitions);
			
			if (Application.isPlaying) return;
			if (entrances == null) return;
			
			Gizmos.color = Color.yellow.NewA(0.33f);
			foreach (var entrance in entrances)
			{
				Gizmos.DrawLine(entrance.position, entrance.position + (entrance.forward * 0.25f));
			}
		}
#endif
	}
}