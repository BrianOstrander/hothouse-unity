using Newtonsoft.Json;
using UnityEngine;
using System;

namespace Lunra.Core.Converters
{
	public class NullableRectConverter : JsonConverter
	{
		[Serializable]
		class SimpleRect
		{
			public float x;
			public float y;
			public float width;
			public float height;

			public override string ToString () => $"( {x}, {y}, {width}, {height} )";
		}

		public override bool CanConvert (Type objectType) => objectType == typeof(Rect?);

		public override void WriteJson (JsonWriter writer, object value, JsonSerializer serializer)
		{
			var rect = (Rect?)value;
			var simple = rect.HasValue ? new SimpleRect { x = rect.Value.x, y = rect.Value.y, width = rect.Value.width, height = rect.Value.height } : null;
			var settings = Serialization.SettingsFromSerializer(serializer);
			settings.TypeNameHandling = TypeNameHandling.None;
			writer.WriteRawValue(JsonConvert.SerializeObject(simple, settings));
		}

		public override object ReadJson (JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			var simple = serializer.Deserialize<SimpleRect>(reader);
			if (simple == null) return null;
			return new Rect(simple.x, simple.y, simple.width, simple.height);
		}
	}
}