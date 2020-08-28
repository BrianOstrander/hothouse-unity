using Newtonsoft.Json;

namespace Lunra.Core
{
	public class IdCounter
	{
		public const long UndefinedId = 0L;
		
		[JsonProperty] long currentId = UndefinedId + 1L;

		public long Next()
		{
			var result = currentId;
			
			unchecked { currentId++; }
			
			return result;
		}
	}
}