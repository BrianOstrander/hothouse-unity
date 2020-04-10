using Newtonsoft.Json;
using UnityEngine;
using System;

namespace LunraGames.Converters
{
	public class Vector2Converter : JsonConverter
	{
		[Serializable]
		class SimpleVector2
		{
			public float x;
			public float y;

			public override string ToString ()
			{
				return "( "+x+", "+y+" )";
			}
		}

		public override bool CanConvert (Type objectType)
		{
			return objectType == typeof(Vector2);
		}

		public override void WriteJson (JsonWriter writer, object value, JsonSerializer serializer)
		{
			var vector2 = (Vector2)value;
			var simple = new SimpleVector2 { x = vector2.x, y = vector2.y };
			var settings = Serialization.SettingsFromSerializer(serializer);
			settings.TypeNameHandling = TypeNameHandling.None;
			writer.WriteRawValue(JsonConvert.SerializeObject(simple, settings));
		}

		public override object ReadJson (JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			var simple = serializer.Deserialize<SimpleVector2>(reader);
			return new Vector2(simple.x, simple.y );
		}
	}
}