using System;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;

using LunraGames.Converters;
using QuaternionConverter = LunraGames.Converters.QuaternionConverter;
using ColorConverter = LunraGames.Converters.ColorConverter;

namespace LunraGames
{
	public static class Serialization 
	{
		static JsonConverter[] Converters =
		{
			new Vector2Converter(),
			new Vector3Converter(),
			new Vector4Converter(),
			new QuaternionConverter(),
			new ColorConverter(),
			new StringEnumConverter()
		};

		static JsonSerializerSettings _SerializerSettings;

		static JsonSerializerSettings SerializerSettings 
		{
			get
			{
				if (_SerializerSettings == null)
				{
					_SerializerSettings = new JsonSerializerSettings();
					_SerializerSettings.ObjectCreationHandling = ObjectCreationHandling.Replace;
					_SerializerSettings.DefaultValueHandling = DefaultValueHandling.Ignore;
					foreach (var converter in Converters) _SerializerSettings.Converters.Add(converter);
					foreach (var converter in AddedConverters) _SerializerSettings.Converters.Add(converter);
#if PROP_JSON
					// This overrides the default vector converter, so we can use our own.
					_SerializerSettings.Converters.Remove(_SerializerSettings.Converters.First(c => c.GetType() == typeof(VectorConverter)));
#endif
				}
				return _SerializerSettings;
			}
		}

		static JsonSerializerSettings _VerboseSerializerSettings;

		/// <summary>
		/// Gets the verbose serializer settings, perfect for use of complex generics.
		/// </summary>
		/// <value>The verbose serializer settings.</value>
		static JsonSerializerSettings VerboseSerializerSettings {
			get {
				if (_VerboseSerializerSettings == null)
				{
					_VerboseSerializerSettings = new JsonSerializerSettings();
					_VerboseSerializerSettings.TypeNameHandling = TypeNameHandling.All;
					_VerboseSerializerSettings.TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple;
					_VerboseSerializerSettings.ObjectCreationHandling = ObjectCreationHandling.Replace;
					_VerboseSerializerSettings.DefaultValueHandling = DefaultValueHandling.Ignore;
					foreach (var converter in Converters) _VerboseSerializerSettings.Converters.Add(converter);
					foreach (var converter in AddedConverters) _VerboseSerializerSettings.Converters.Add(converter);
#if PROP_JSON
					// This overrides the default vector converter, so we can use our own.
					_VerboseSerializerSettings.Converters.Remove(_VerboseSerializerSettings.Converters.First(c => c.GetType() == typeof(VectorConverter)));
#endif
				}
				return _VerboseSerializerSettings;
			}
		}

		static List<JsonConverter> AddedConverters = new List<JsonConverter>();

		public static void AddConverters(params JsonConverter[] converters)
		{
			foreach (var converter in converters)
			{
				if (AddedConverters.Contains(converter)) continue;
				AddedConverters.Add(converter);
				if (_SerializerSettings != null) _SerializerSettings.Converters.Add(converter);
				if (_VerboseSerializerSettings != null) _VerboseSerializerSettings.Converters.Add(converter);
			}
		}

		public static JsonSerializerSettings SettingsFromSerializer(JsonSerializer serializer, bool includeConverters = false)
		{
			var settings = new JsonSerializerSettings();
			settings.TypeNameHandling = serializer.TypeNameHandling;
			settings.TypeNameAssemblyFormatHandling = serializer.TypeNameAssemblyFormatHandling;
			settings.ObjectCreationHandling = serializer.ObjectCreationHandling;
			settings.DefaultValueHandling = serializer.DefaultValueHandling;
			if (includeConverters) settings.Converters = serializer.Converters;
			return settings;
		}

		public static object DeserializeJson(Type type, string json, object defaultValue = null, bool verbose = false)
		{
			if (StringExtensions.IsNullOrWhiteSpace(json)) return defaultValue;

			try 
			{
				return JsonConvert.DeserializeObject(json, type, verbose ? VerboseSerializerSettings : SerializerSettings);
			}
			catch (Exception e)
			{
				Debug.LogError("Problem parsing value: \n\t"+json+"\nReturning default value\n Exception:\n"+e.Message);
				return defaultValue;
			}
		}

		public static T DeserializeJson<T>(string json, T defaultValue = default(T), bool verbose = false)
		{
			if (StringExtensions.IsNullOrWhiteSpace(json)) return defaultValue;

			try 
			{
				return JsonConvert.DeserializeObject<T>(json, verbose ? VerboseSerializerSettings : SerializerSettings);
			}
			catch (Exception e)
			{
				Debug.LogError("Problem parsing value: \n\t"+json+"\nReturning default value\n Exception:\n"+e.Message);
				return defaultValue;
			}
		}

		public static string SerializeJson(object value, bool verbose = false, Formatting formatting = Formatting.None)
		{
			return value == null ? string.Empty : JsonConvert.SerializeObject(value, formatting, verbose ? VerboseSerializerSettings : SerializerSettings);
		}

		/// <summary>
		/// Deserializes the json into a raw dictionary of strings and objects.
		/// </summary>
		/// <returns>The dictionary of key value pairs.</returns>
		/// <param name="json">Json.</param>
		/// <param name="defaultValue">Default value.</param>
		/// <param name="verbose">Verbose.</param>
		public static object DeserializeJsonRaw(string json, object defaultValue = null, bool verbose = false)
		{
			if (StringExtensions.IsNullOrWhiteSpace(json)) return defaultValue;

			try { return DeserializeRaw(JToken.Parse(json)); }
			catch (Exception e)
			{
				Debug.LogError("Problem parsing value: \n\t" + json + "\nReturning default value\n Exception:\n" + e.Message);
				return defaultValue;
			}
		}

		static object DeserializeRaw(JToken token)
		{
			switch (token.Type)
			{
				case JTokenType.Object: return token.Children<JProperty>().ToDictionary(p => p.Name, p => DeserializeRaw(p.Value));
				case JTokenType.Array: return token.Select(t => DeserializeRaw(t)).ToList();
				default: return ((JValue)token).Value;
			}
		}

		public static string Serialize(this object target, bool verbose = false, Formatting formatting = Formatting.None)
		{
			return SerializeJson(target, verbose, formatting);
		}

		public static object Deserialize(this string json, Type type, object defaultValue = null, bool verbose = false)
		{
			return DeserializeJson(type, json, defaultValue, verbose);
		}

		public static T Deserialize<T>(this string json, T defaultValue = default(T), bool verbose = false)
		{
			return DeserializeJson(json, defaultValue, verbose);
		}
	}
}