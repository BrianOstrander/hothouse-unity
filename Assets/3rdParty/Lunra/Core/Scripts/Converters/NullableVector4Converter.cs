using Newtonsoft.Json;
using UnityEngine;
using System;
using Lunra.Core;

namespace Lunra.Core.Converters
{
	public class NullableVector4Converter : JsonConverter
	{
		[Serializable]
		class SimpleVector4
		{
			public float x;
			public float y;
			public float z;
			public float w;

			public override string ToString () => $"( {x}, {y}, {z}, {w} )";
		}

		public override bool CanConvert (Type objectType) => objectType == typeof(Vector4?);

		public override void WriteJson (JsonWriter writer, object value, JsonSerializer serializer)
		{
			var vector4 = (Vector4?)value;
			var simple = vector4.HasValue ? new SimpleVector4 { x = vector4.Value.x, y = vector4.Value.y, z = vector4.Value.z, w = vector4.Value.w } : null;
			var settings = Serialization.SettingsFromSerializer(serializer);
			settings.TypeNameHandling = TypeNameHandling.None;
			writer.WriteRawValue(JsonConvert.SerializeObject(simple, settings));
		}

		public override object ReadJson (JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			var simple = serializer.Deserialize<SimpleVector4>(reader);
			if (simple == null) return null;
			return new Vector4(simple.x, simple.y, simple.z, simple.w);
		}
	}
}