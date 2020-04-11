using System;
using System.Linq;

using UnityEngine;

namespace Lunra.Core
{
	public abstract class PlayerPrefsKv<T>
	{
		public readonly string Key;
		public readonly T Default;

		public abstract T Value { get; set; }

		public PlayerPrefsKv(string key, T defaultValue = default)
		{
			Key = key;
			Default = defaultValue;
		}
	}

	public class PlayerPrefsString : PlayerPrefsKv<string>
	{
		public override string Value
		{
			get => PlayerPrefs.GetString(Key, Default);
			set => PlayerPrefs.SetString(Key, value);
		}

		public PlayerPrefsString(string key, string defaultValue = null) : base(key, defaultValue) { }
	}

	public class PlayerPrefsBool : PlayerPrefsKv<bool>
	{
		public override bool Value
		{
			get => PlayerPrefsExtensions.GetBool(Key, Default);
			set => PlayerPrefsExtensions.SetBool(Key, value);
		}

		public PlayerPrefsBool(string key, bool defaultValue = false) : base(key, defaultValue) { }
	}

	public class PlayerPrefsFloat : PlayerPrefsKv<float>
	{
		public override float Value
		{
			get => PlayerPrefs.GetFloat(Key, Default);
			set => PlayerPrefs.SetFloat(Key, value);
		}

		public PlayerPrefsFloat(string key, float defaultValue = 0f) : base(key, defaultValue) { }
	}

	public class PlayerPrefsInt : PlayerPrefsKv<int>
	{
		public override int Value
		{
			get => PlayerPrefs.GetInt(Key, Default);
			set => PlayerPrefs.SetInt(Key, value);
		}

		public PlayerPrefsInt(string key, int defaultValue = 0) : base(key, defaultValue) { }
	}

	public class PlayerPrefsEnum<T> : PlayerPrefsKv<T> where T : struct, IConvertible
	{
		public override T Value
		{
			get
			{
				var intValue = PlayerPrefs.GetInt(Key, Convert.ToInt32(Default));
				return Enum.GetValues(typeof(T)).Cast<T>().FirstOrDefault(e => Convert.ToInt32(e) == intValue);
			}
			set => PlayerPrefs.SetInt(Key, Convert.ToInt32(value));
		}

		public PlayerPrefsEnum(string key, T defaultValue = default) : base(key, defaultValue)
		{
			if (!typeof(T).IsEnum) Debug.LogError(typeof(T).FullName + " is not an enum.");
		}
	}

	public class DevPrefsToggle<P, T>
		where P : PlayerPrefsKv<T>
	{
		public readonly P Property;
		public readonly PlayerPrefsBool Enabled;

		public DevPrefsToggle(
			P property,
			bool enabled = false
		)
		{
			Property = property;
			Enabled = new PlayerPrefsBool("DPToggle_" + property.Key, enabled);
		}

		public T Value 
		{
			get => Property.Value;
			set => Property.Value = value;
		}

		public T Get(T fallback = default) { return Enabled.Value ? Property.Value : fallback; }
		public void Set(ref T value)
		{
			if (Enabled.Value) value = Property.Value;
		}
	}
}