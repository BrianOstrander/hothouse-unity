using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;

namespace Lunra.Core.Resolvers
{
	public class NonPublicPropertiesResolver : DefaultContractResolver
	{
		// protected override JsonProperty CreateProperty(
		// 	MemberInfo member,
		// 	MemberSerialization memberSerialization
		// )
		// {
		// 	var property = base.CreateProperty(member, memberSerialization);
		// 	if (member is PropertyInfo propertyInfo)
		// 	{
		// 		property.Readable = (propertyInfo.GetMethod != null);
		// 		property.Writable = (propertyInfo.SetMethod != null);
		// 	}
		//
		// 	return property;
		// }
		protected override JsonProperty CreateProperty(
			MemberInfo member,
			MemberSerialization memberSerialization)
		{
			var prop = base.CreateProperty(member, memberSerialization);

			if (!prop.Writable)
			{
				var property = member as PropertyInfo;
				
				if (property != null)
				{
					prop.Writable = property.GetSetMethod(true) != null;
					if (prop.PropertyName == "State") Debug.Log($"is non? {property.GetSetMethod(true) != null}");
				}
			}

			return prop;
		}
	}
}