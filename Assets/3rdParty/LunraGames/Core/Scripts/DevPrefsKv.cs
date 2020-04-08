using System;
using System.Linq;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LunraGames
{
	public abstract class DevPrefsKv<T>
	{
		public readonly string Key;
		public readonly T Default;

		public abstract T Value { get; set; }

		public DevPrefsKv(string key, T defaultValue = default(T))
		{
			Key = key;
			Default = defaultValue;
		}

		public static implicit operator T(DevPrefsKv<T> p)
		{
			return p.Value;
		}
	}

	public class DevPrefsString : DevPrefsKv<string>
	{
		public override string Value
		{
#if UNITY_EDITOR
			get { return EditorPrefs.GetString(Key, Default); }
			set { EditorPrefs.SetString(Key, value); }
#else
			get; set;
#endif
		}

		public DevPrefsString(string key, string defaultValue = null) : base(key, defaultValue) { }
	}

	public class DevPrefsBool : DevPrefsKv<bool>
	{
		public override bool Value
		{
#if UNITY_EDITOR
			get { return EditorPrefs.GetBool(Key, Default); }
			set { EditorPrefs.SetBool(Key, value); }
#else
			get; set;
#endif
		}

		public DevPrefsBool(string key, bool defaultValue = false) : base(key, defaultValue) { }
	}

	public class DevPrefsFloat : DevPrefsKv<float>
	{
		public override float Value
		{
#if UNITY_EDITOR
			get { return EditorPrefs.GetFloat(Key, Default); }
			set { EditorPrefs.SetFloat(Key, value); }
#else
			get; set;
#endif
		}

		public DevPrefsFloat(string key, float defaultValue = 0f) : base(key, defaultValue) { }
	}

	public class DevPrefsInt : DevPrefsKv<int>
	{
		public override int Value
		{
#if UNITY_EDITOR
			get { return EditorPrefs.GetInt(Key, Default); }
			set { EditorPrefs.SetInt(Key, value); }
#else
			get; set;
#endif
		}

		public DevPrefsInt(string key, int defaultValue = 0) : base(key, defaultValue) { }
	}

	public class DevPrefsEnum<T> : DevPrefsKv<T> where T : struct, IConvertible
	{
		public override T Value
		{
#if UNITY_EDITOR
			get
			{
				var intValue = EditorPrefs.GetInt(Key, Convert.ToInt32(Default));
				return Enum.GetValues(typeof(T)).Cast<T>().FirstOrDefault(e => Convert.ToInt32(e) == intValue);
			}
			set
			{
				EditorPrefs.SetInt(Key, Convert.ToInt32(value));
			}
#else
			get; set;
#endif
		}

		public DevPrefsEnum(string key, T defaultValue = default(T)) : base(key, defaultValue)
		{
			if (!typeof(T).IsEnum) Debug.LogError(typeof(T).FullName + " is not an enum.");
		}
	}

	public class DevPrefsToggle<P, T>
		where P : DevPrefsKv<T>
	{
		public readonly P Property;
		public readonly DevPrefsBool Enabled;

		public DevPrefsToggle(
			P property,
			bool enabled = false
		)
		{
			Property = property;
			Enabled = new DevPrefsBool("DPToggle_" + property.Key, enabled);
		}

		public T Value 
		{
			get { return Property.Value; }
			set { Property.Value = value; }
		}

		public T Get(T fallback = default(T)) { return Enabled.Value ? Property.Value : fallback; }
		public void Set(ref T value)
		{
			if (Enabled.Value) value = Property.Value;
		}
	}
}