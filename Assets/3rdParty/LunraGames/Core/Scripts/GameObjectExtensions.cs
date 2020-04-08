using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LunraGames 
{
	public static class GameObjectExtensions 
	{
		public static GameObject InstantiateObject(
			this GameObject prefab,
			Vector3? position = null,
			Vector3? scale = null,
			Quaternion? rotation = null,
			bool? setActive = null
		)
		{
			position = position.HasValue? position : Vector3.zero;
			scale = scale.HasValue? scale : Vector3.one;
			rotation = rotation.HasValue? rotation : Quaternion.identity;

			var transform = Object.Instantiate(prefab).transform;
			transform.position = position.Value;
			transform.localScale = scale.Value;
			transform.rotation = rotation.Value;

			if (setActive.HasValue) transform.gameObject.SetActive(setActive.Value);

			return transform.gameObject;
		}

		public static GameObject InstantiateChildObject(
			this GameObject gameObject,
			GameObject prefab,
			Vector3? localPosition = null,
			Vector3? localScale = null,
			Quaternion? localRotation = null,
			bool? setActive = null
		)
		{
			localPosition = localPosition.HasValue ? localPosition : Vector3.zero;
			localScale = localScale.HasValue ? localScale : Vector3.one;
			localRotation = localRotation.HasValue ? localRotation : Quaternion.identity;

			var child = Object.Instantiate(prefab).transform;
			child.SetParent(gameObject.transform);
			child.localScale = localScale.Value;
			child.localPosition = localPosition.Value;
			child.localRotation = localRotation.Value;

			if (setActive.HasValue) child.gameObject.SetActive(setActive.Value);

			return child.gameObject;
		}

		public static T InstantiateChild<T>(
			this GameObject gameObject, 
			T prefab, 
			Vector3? localPosition = null, 
			Vector3? localScale = null,
			Quaternion? localRotation = null,
			bool? setActive = null
		) 
			where T : MonoBehaviour
		{
			return gameObject.InstantiateChildObject(prefab.gameObject, localPosition, localScale, localRotation, setActive).GetComponent<T>();
		}

		public static bool HasComponent<T>(this GameObject gameObject)
			where T : MonoBehaviour
		{
			return gameObject.HasComponent(typeof(T));
		}

		public static bool HasComponent(this GameObject gameObject, Type type)
		{
			return gameObject.GetComponent(type) != null;
		}

		public static bool HasComponents(this GameObject gameObject, IEnumerable<Type> components)
		{
			foreach (var type in components)
			{
				if (!gameObject.HasComponent(type)) return false;
			}
			return true;
		}

		/// <summary>
		/// A normal FindWithTag call results in an error if the tag doesn't
		/// exist. This prints the warning to console instead of throwing the
		/// error, and returns null.
		/// </summary>
		/// <returns>The with tag or handle missing tag.</returns>
		/// <param name="tag">Tag.</param>
		public static GameObject FindWithTagOrHandleMissingTag(string tag, bool quiet = false)
		{
			try
			{
				return GameObject.FindWithTag(tag);
			}
			catch (UnityException e)
			{
				if (!quiet) Debug.LogException(e);
				return null;
			}
		}

		public static void SetLayerRecursively(this GameObject gameObject, int layer)
		{
			gameObject.layer = layer;
			for (var i = 0; i < gameObject.transform.childCount; i++)
			{
				gameObject.transform.GetChild(i).gameObject.SetLayerRecursively(layer);
			}
		}
	}
}