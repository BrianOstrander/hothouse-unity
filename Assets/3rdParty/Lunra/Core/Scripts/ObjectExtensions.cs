using Newtonsoft.Json;

namespace Lunra.Core
{
	public static class ObjectExtensions 
	{
		public static string ToReadableJson(this object value) => value.Serialize(formatting: Formatting.Indented);
	}
}