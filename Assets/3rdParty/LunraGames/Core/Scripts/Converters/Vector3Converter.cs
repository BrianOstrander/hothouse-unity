using Newtonsoft.Json;
using UnityEngine;
using System;

namespace LunraGames.Converters
{
	public class Vector3Converter : JsonConverter
	{
		[Serializable]
		class SimpleVector3
		{
			public float x;
			public float y;
			public float z;

			public override string ToString ()
			{
				return "( "+x+", "+y+", "+z+" )";
			}
		}

		public override bool CanConvert (Type objectType)
		{
			return objectType == typeof(Vector3);
		}

		public override void WriteJson (JsonWriter writer, object value, JsonSerializer serializer)
		{
			var vector3 = (Vector3)value;
			var simple = new SimpleVector3 { x = vector3.x, y = vector3.y, z = vector3.z };
			var settings = Serialization.SettingsFromSerializer(serializer);
			settings.TypeNameHandling = TypeNameHandling.None;
			writer.WriteRawValue(JsonConvert.SerializeObject(simple, settings));
		}

		public override object ReadJson (JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			var simple = serializer.Deserialize<SimpleVector3>(reader);
			return new Vector3(simple.x, simple.y, simple.z);
		}
	}
}