using Newtonsoft.Json;
using UnityEngine;
using System;

namespace Lunra.Core.Converters
{
	public class NullableVector3Converter : JsonConverter
	{
		[Serializable]
		class SimpleVector3
		{
			public float x;
			public float y;
			public float z;

			public override string ToString () => $"( {x}, {y}, {z} )";
		}

		public override bool CanConvert (Type objectType) => objectType == typeof(Vector3?);

		public override void WriteJson (JsonWriter writer, object value, JsonSerializer serializer)
		{
			var vector3 = (Vector3?)value;
			var simple = vector3.HasValue ? new SimpleVector3 { x = vector3.Value.x, y = vector3.Value.y, z = vector3.Value.z } : null;
			var settings = Serialization.SettingsFromSerializer(serializer);
			settings.TypeNameHandling = TypeNameHandling.None;
			writer.WriteRawValue(JsonConvert.SerializeObject(simple, settings));
		}

		public override object ReadJson (JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			var simple = serializer.Deserialize<SimpleVector3>(reader);
			if (simple == null) return null;
			return new Vector3(simple.x, simple.y, simple.z);
		}
	}
}