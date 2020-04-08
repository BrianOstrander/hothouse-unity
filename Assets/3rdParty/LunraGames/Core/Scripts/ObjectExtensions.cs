using Newtonsoft.Json;

namespace LunraGames
{
	public static class ObjectExtensions 
	{
		public static string ToReadableJson(this object value) => value.Serialize(formatting: Formatting.Indented);
	}
}