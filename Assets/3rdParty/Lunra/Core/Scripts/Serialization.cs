using System;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;

using Lunra.Core.Converters;
using QuaternionConverter = Lunra.Core.Converters.QuaternionConverter;
using ColorConverter = Lunra.Core.Converters.ColorConverter;

namespace Lunra.Core
{
	public static class Serialization 
	{
		static JsonConverter[] converters =
		{
			new Vector2Converter(),
			new Vector3Converter(),
			new Vector4Converter(),
			new QuaternionConverter(),
			new ColorConverter(),
			new StringEnumConverter()
		};

		static JsonSerializerSettings serializerSettings;

		static JsonSerializerSettings SerializerSettings 
		{
			get
			{
				if (serializerSettings == null)
				{
					serializerSettings = new JsonSerializerSettings();
					serializerSettings.ObjectCreationHandling = ObjectCreationHandling.Auto;
					serializerSettings.DefaultValueHandling = DefaultValueHandling.Ignore;
					foreach (var converter in converters) serializerSettings.Converters.Add(converter);
					foreach (var converter in addedConverters) serializerSettings.Converters.Add(converter);
				}
				return serializerSettings;
			}
		}

		static JsonSerializerSettings verboseSerializerSettings;

		/// <summary>
		/// Gets the verbose serializer settings, perfect for use of complex generics.
		/// </summary>
		/// <value>The verbose serializer settings.</value>
		static JsonSerializerSettings VerboseSerializerSettings {
			get {
				if (verboseSerializerSettings == null)
				{
					verboseSerializerSettings = new JsonSerializerSettings();
					verboseSerializerSettings.TypeNameHandling = TypeNameHandling.All;
					verboseSerializerSettings.TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple;
					verboseSerializerSettings.ObjectCreationHandling = ObjectCreationHandling.Auto;
					verboseSerializerSettings.DefaultValueHandling = DefaultValueHandling.Ignore;
					foreach (var converter in converters) verboseSerializerSettings.Converters.Add(converter);
					foreach (var converter in addedConverters) verboseSerializerSettings.Converters.Add(converter);
				}
				return verboseSerializerSettings;
			}
		}

		static List<JsonConverter> addedConverters = new List<JsonConverter>();

		public static void AddConverters(params JsonConverter[] converters)
		{
			foreach (var converter in converters)
			{
				if (addedConverters.Contains(converter)) continue;
				addedConverters.Add(converter);
				serializerSettings?.Converters.Add(converter);
				verboseSerializerSettings?.Converters.Add(converter);
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

		public static T DeserializeJson<T>(string json, T defaultValue = default, bool verbose = false)
		{
			if (StringExtensions.IsNullOrWhiteSpace(json)) return defaultValue;

			try 
			{
				return JsonConvert.DeserializeObject<T>(json, verbose ? VerboseSerializerSettings : SerializerSettings);
			}
			catch (Exception e)
			{
				Debug.LogError($"Encountered the following exception:\n{e.Message}\n\nWhile Parsing: {json}");
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
				case JTokenType.Array: return token.Select(DeserializeRaw).ToList();
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

		public static T Deserialize<T>(this string json, T defaultValue = default, bool verbose = false)
		{
			return DeserializeJson(json, defaultValue, verbose);
		}
	}
}