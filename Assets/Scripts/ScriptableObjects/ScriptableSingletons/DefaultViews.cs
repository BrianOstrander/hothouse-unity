using System;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;

using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LunraGames.SubLight
{
	public class DefaultViews : Singletonnes.ScriptableSingleton<DefaultViews>
	{
		[Serializable]
		public class Entry
		{
			public GameObject PrefabRoot;
		}

		[SerializeField, HideInInspector]
		List<Entry> entries = new List<Entry>();

		public List<GameObject> Prefabs { get { return entries.Where(e => e != null && e.PrefabRoot != null).Select(e => e.PrefabRoot).ToList(); } }

#if UNITY_EDITOR
		public List<Entry> Entries
		{
			get { return entries; }
			set { entries = value; }
		}

		public void Add(Object prefab)
		{
			if (Contains(prefab)) Debug.Log("Prefab already added to DefaultViews");
			else
			{
				entries.Add(
					new Entry
					{
						PrefabRoot = (prefab as Component).transform.root.gameObject
					}
				);
				SetConfigDirty();
			}
		}

		public void Remove(Object prefab)
		{
			if (Contains(prefab))
			{
				entries.RemoveAll(e => e.PrefabRoot.GetInstanceID() == (prefab as Component).transform.root.gameObject.GetInstanceID());
				SetConfigDirty();
			}
			else Debug.Log("Prefab is not listed in DefaultViews");
		}

		public bool Contains(Object prefab)
		{
			return entries.FirstOrDefault(e => e.PrefabRoot.GetInstanceID() == (prefab as Component).transform.root.gameObject.GetInstanceID()) != null;
		}

		void SetConfigDirty()
		{
			EditorUtility.SetDirty(Instance);
		}
#endif
	}
}