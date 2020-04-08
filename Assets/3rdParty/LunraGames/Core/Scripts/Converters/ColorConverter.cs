using Newtonsoft.Json;
using UnityEngine;
using System;

namespace LunraGames.Converters
{
	public class ColorConverter : JsonConverter
	{
		[Serializable]
		class SimpleColor
		{
			public float r;
			public float g;
			public float b;
			public float a;

			public override string ToString ()
			{
				return "( "+r+", "+g+", "+b+", "+a+" )";
			}
		}

		public override bool CanConvert (Type objectType)
		{
			return objectType == typeof(Color);
		}

		public override void WriteJson (JsonWriter writer, object value, JsonSerializer serializer)
		{
			var color = (Color)value;
			var simple = new SimpleColor { r = color.r, g = color.g, b = color.b, a = color.a };
			var settings = Serialization.SettingsFromSerializer(serializer);
			settings.TypeNameHandling = TypeNameHandling.None;
			writer.WriteRawValue(JsonConvert.SerializeObject(simple, settings));
		}

		public override object ReadJson (JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			var simple = serializer.Deserialize<SimpleColor>(reader);
			return new Color(simple.r, simple.g, simple.b, simple.a);
		}
	}
}