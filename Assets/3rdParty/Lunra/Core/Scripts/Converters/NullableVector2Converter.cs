using Newtonsoft.Json;
using UnityEngine;
using System;
using Lunra.Core;

namespace Lunra.Core.Converters
{
	public class NullableVector2Converter : JsonConverter
	{
		[Serializable]
		class SimpleVector2
		{
			public float x;
			public float y;

			public override string ToString () => $"( {x}, {y} )";
		}

		public override bool CanConvert (Type objectType) => objectType == typeof(Vector2?);

		public override void WriteJson (JsonWriter writer, object value, JsonSerializer serializer)
		{
			var vector2 = (Vector2?)value;
			var simple = vector2.HasValue ? new SimpleVector2 { x = vector2.Value.x, y = vector2.Value.y } : null;
			var settings = Serialization.SettingsFromSerializer(serializer);
			settings.TypeNameHandling = TypeNameHandling.None;
			writer.WriteRawValue(JsonConvert.SerializeObject(simple, settings));
		}

		public override object ReadJson (JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			var simple = serializer.Deserialize<SimpleVector2>(reader);
			if (simple == null) return null;
			return new Vector2(simple.x, simple.y );
		}
	}
}