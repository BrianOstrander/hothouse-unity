using Newtonsoft.Json;
using UnityEngine;
using System;

namespace LunraGames.Converters
{
	public class Vector4Converter : JsonConverter
	{
		[Serializable]
		class SimpleVector4
		{
			public float x;
			public float y;
			public float z;
			public float w;

			public override string ToString ()
			{
				return "( "+x+", "+y+", "+z+", "+w+" )";
			}
		}

		public override bool CanConvert (Type objectType)
		{
			return objectType == typeof(Vector4);
		}

		public override void WriteJson (JsonWriter writer, object value, JsonSerializer serializer)
		{
			var vector4 = (Vector4)value;
			var simple = new SimpleVector4 { x = vector4.x, y = vector4.y, z = vector4.z, w = vector4.w };
			var settings = Serialization.SettingsFromSerializer(serializer);
			settings.TypeNameHandling = TypeNameHandling.None;
			writer.WriteRawValue(JsonConvert.SerializeObject(simple, settings));
		}

		public override object ReadJson (JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			var simple = serializer.Deserialize<SimpleVector4>(reader);
			return new Vector4(simple.x, simple.y, simple.z, simple.w);
		}
	}
}