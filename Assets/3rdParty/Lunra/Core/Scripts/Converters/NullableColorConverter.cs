using Newtonsoft.Json;
using UnityEngine;
using System;

namespace Lunra.Core.Converters
{
	public class NullableColorConverter : JsonConverter
	{
		[Serializable]
		class SimpleColor
		{
			public float r;
			public float g;
			public float b;
			public float a;

			public override string ToString () => $"( {r}, {g}, {b}, {a} )";
		}

		public override bool CanConvert (Type objectType) => objectType == typeof(Color?);

		public override void WriteJson (JsonWriter writer, object value, JsonSerializer serializer)
		{
			var color = (Color?)value;
			var simple = color.HasValue ? new SimpleColor { r = color.Value.r, g = color.Value.g, b = color.Value.b, a = color.Value.a } : null;
			var settings = Serialization.SettingsFromSerializer(serializer);
			settings.TypeNameHandling = TypeNameHandling.None;
			writer.WriteRawValue(JsonConvert.SerializeObject(simple, settings));
		}

		public override object ReadJson (JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			var simple = serializer.Deserialize<SimpleColor>(reader);
			if (simple == null) return null;
			return new Color(simple.r, simple.g, simple.b, simple.a);
		}
	}
}