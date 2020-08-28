using Newtonsoft.Json;
using UnityEngine;
using System;

namespace Lunra.Core.Converters
{
	public class NullableQuaternionConverter : JsonConverter
	{
		[Serializable]
		class SimpleQuaternion
		{
			public float x;
			public float y;
			public float z;
			public float w;

			public override string ToString () => $"( {x}, {y}, {z}, {w} )";
		}

		public override bool CanConvert (Type objectType) => objectType == typeof(Quaternion?);

		public override void WriteJson (JsonWriter writer, object value, JsonSerializer serializer)
		{
			var quaternion = (Quaternion?)value;
			var simple = quaternion.HasValue ? new SimpleQuaternion { x = quaternion.Value.x, y = quaternion.Value.y, z = quaternion.Value.z , w = quaternion.Value.w } : null;
			var settings = Serialization.SettingsFromSerializer(serializer);
			settings.TypeNameHandling = TypeNameHandling.None;
			writer.WriteRawValue(JsonConvert.SerializeObject(simple, settings));
		}

		public override object ReadJson (JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			var simple = serializer.Deserialize<SimpleQuaternion>(reader);
			if (simple == null) return null;
			return new Quaternion(simple.x, simple.y, simple.z, simple.w);
		}
	}
}