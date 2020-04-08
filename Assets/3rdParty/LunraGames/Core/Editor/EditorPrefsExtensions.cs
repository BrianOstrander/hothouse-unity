using UnityEngine;
using UnityEditor;
using System;
using Newtonsoft.Json;
using LunraGames;
using LunraGames.Converters;

namespace LunraGamesEditor
{
	public static class EditorPrefsExtensions 
	{
		static JsonConverter[] Converters = 
		{
			new Vector2Converter(),
			new Vector3Converter(),
			new Vector4Converter(),
			new QuaternionConverter(),
			new ColorConverter()
		};

		static JsonSerializerSettings _SerializerSettings;

		static JsonSerializerSettings SerializerSettings 
		{
			get
			{
				if (_SerializerSettings == null)
				{
					_SerializerSettings = new JsonSerializerSettings();
					_SerializerSettings.TypeNameHandling = TypeNameHandling.All;
					foreach (var converter in Converters) _SerializerSettings.Converters.Add(converter);
				}
				return _SerializerSettings;
			}
		}
		
		public static T GetJson<T>(string key, T defaultValue = default(T))
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