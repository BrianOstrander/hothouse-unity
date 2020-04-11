using UnityEngine;
using UnityEditor;
using System;
using Newtonsoft.Json;
using Lunra.Core;
using Lunra.Core.Converters;

namespace Lunra.Editor.Core
{
	public static class EditorPrefsExtensions 
	{
		static JsonConverter[] converters = 
		{
			new Vector2Converter(),
			new Vector3Converter(),
			new Vector4Converter(),
			new QuaternionConverter(),
			new ColorConverter()
		};

		static JsonSerializerSettings serializerSettings;

		static JsonSerializerSettings SerializerSettings 
		{
			get
			{
				if (serializerSettings == null)
				{
					serializerSettings = new JsonSerializerSettings();
					serializerSettings.TypeNameHandling = TypeNameHandling.All;
					foreach (var converter in converters) serializerSettings.Converters.Add(converter);
				}
				return serializerSettings;
			}
		}
		
		public static T GetJson<T>(string key, T defaultValue = default)
		{
			var serialized = EditorPrefs.GetString(key, string.Empty);
			if (StringExtensions.IsNullOrWhiteSpace(serialized)) return defaultValue;

			try 
			{
				return JsonConvert.DeserializeObject<T>(serialized, SerializerSettings);
			}
			catch (Exception e)
			{
				Debug.LogError("Problem parsing "+key+" with value: \n\t"+serialized+"\nReturning default value\n Exception:\n"+e.Message);
				return defaultValue;
			}
		}

		public static void SetJson(string key, object value)
		{
			if (value == null) EditorPrefs.SetString(key, string.Empty);
			else EditorPrefs.SetString(key, JsonConvert.SerializeObject(value, Formatting.None, SerializerSettings));
		}
	}
}