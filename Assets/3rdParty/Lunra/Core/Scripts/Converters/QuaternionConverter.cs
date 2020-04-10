using Newtonsoft.Json;
using UnityEngine;
using System;

namespace LunraGames.Converters
{
	public class QuaternionConverter : JsonConverter
	{
		[Serializable]
		class SimpleQuaternion
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
			return objectType == typeof(Quaternion);
		}

		public override void WriteJson (JsonWriter writer, object value, JsonSerializer serializer)
		{
			var quaternion = (Quaternion)value;
			var simple = new SimpleQuaternion { x = quaternion.x, y = quaternion.y, z = quaternion.z , w = quaternion.w };
			var settings = Serialization.SettingsFromSerializer(serializer);
			settings.TypeNameHandling = TypeNameHandling.None;
			writer.WriteRawValue(JsonConvert.SerializeObject(simple, settings));
		}

		public override object ReadJson (JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			var simple = serializer.Deserialize<SimpleQuaternion>(reader);
			return new Quaternion(simple.x, simple.y, simple.z, simple.w);
		}
	}
}