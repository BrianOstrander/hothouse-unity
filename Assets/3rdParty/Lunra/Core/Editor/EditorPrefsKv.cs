using System;
using System.Linq;

using UnityEngine;

using UnityEditor;

namespace Lunra.Editor.Core
{
	public abstract class EditorPrefsKv<T>
	{
		public readonly string Name;
		public readonly string Key;
		public readonly T Default;

		public abstract T Value { get; set; }

		public EditorPrefsKv(string key, T defaultValue = default)
		{
			if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
			
			Name = key.Contains('.') ? key.Split('.').Last() : key;
			Key = key;
			Default = defaultValue;
		}

		public string LabelName => ObjectNames.NicifyVariableName(Name);
		
		public GUIContent Label => new GUIContent(LabelName);

		public abstract void Draw();
	}

	public class EditorPrefsString : EditorPrefsKv<string>
	{
		public override string Value
		{
			get => EditorPrefs.GetString(Key, Default);
			set => EditorPrefs.SetString(Key, value);
		}

		public EditorPrefsString(string key, string defaultValue = null) : base(key, defaultValue) {}

		public override void Draw() => Value = EditorGUILayout.TextField(Label, Value);
	}

	public class EditorPrefsBool : EditorPrefsKv<bool>
	{
		public override bool Value
		{
			get => EditorPrefs.GetBool(Key, Default);
			set => EditorPrefs.SetBool(Key, value);
		}

		public EditorPrefsBool(string key, bool defaultValue = false) : base(key, defaultValue) {}

		public override void Draw() => Value = EditorGUILayout.Toggle(Label, Value);
	}

	public class EditorPrefsFloat : EditorPrefsKv<float>
	{
		public override float Value
		{
			get => EditorPrefs.GetFloat(Key, Default);
			set => EditorPrefs.SetFloat(Key, value);
		}

		public Vector2 HorizontalScroll
		{
			get => new Vector2(Value, 0f);
			set => Value = value.x;
		}

		public Vector2 VerticalScroll
		{
			get => new Vector2(0f, Value);
			set => Value = value.y;
		}

		public EditorPrefsFloat(string key, float defaultValue = 0f) : base(key, defaultValue) {}

		public override void Draw() => Value = EditorGUILayout.FloatField(Label, Value);
	}

	public class EditorPrefsInt : EditorPrefsKv<int>
	{
		public override int Value
		{
			get => EditorPrefs.GetInt(Key, Default);
			set => EditorPrefs.SetInt(Key, value);
		}

		public EditorPrefsInt(string key, int defaultValue = 0) : base(key, defaultValue) {}

		public override void Draw() => Value = EditorGUILayout.IntField(Label, Value);
	}

	public class EditorPrefsEnum<T> : EditorPrefsKv<T> where T : Enum
	{
		public override T Value
		{
			get
			{
				var intValue = EditorPrefs.GetInt(Key, Convert.ToInt32(Default));
				return Enum.GetValues(typeof(T)).Cast<T>().FirstOrDefault(e => Convert.ToInt32(e) == intValue);
			}
			set => EditorPrefs.SetInt(Key, Convert.ToInt32(value));
		}

		public EditorPrefsEnum(string key, T defaultValue = default) : base(key, defaultValue) 
		{
			if (!typeof(T).IsEnum) Debug.LogError(typeof(T).FullName + " is not an enum.");
		}

		public override void Draw() => Value = (T)EditorGUILayout.EnumPopup(Label, Value);
	}
}