using Newtonsoft.Json;
using UnityEngine;
using System;

namespace LunraGames.Converters
{
	public class RectConverter : JsonConverter
	{
		[Serializable]
		class SimpleRect
		{
			public float x;
			public float y;
			public float width;
			public float height;

			public override string ToString ()
			{
				return "( "+x+", "+y+", "+width+", "+height+" )";
			}
		}

		public override bool CanConvert (Type objectType)
		{
			return objectType == typeof(Rect);
		}

		public override void WriteJson (JsonWriter writer, object value, JsonSerializer serializer)
		{
			var rect = (Rect)value;
			var simple = new SimpleRect { x = rect.x, y = rect.y, width = rect.width, height = rect.height };
			var settings = Serialization.SettingsFromSerializer(serializer);
			settings.TypeNameHandling = TypeNameHandling.None;
			writer.WriteRawValue(JsonConvert.SerializeObject(simple, settings));
		}

		public override object ReadJson (JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			var simple = serializer.Deserialize<SimpleRect>(reader);
			return new Rect(simple.x, simple.y, simple.width, simple.height);
		}
	}
}