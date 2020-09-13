using Types = Lunra.Satchel.PropertyValidation.Types;

namespace Lunra.Satchel
{
	public static class PropertyValidations
	{
		public static class Bool
		{
			public static PropertyValidation Defined(PropertyKey<bool> key) => new PropertyValidation(
				key.Key,
				Types.Bool | Types.Defined
			);
			
			public static PropertyValidation EqualTo(PropertyKey<bool> key, bool value) => new PropertyValidation(
				key.Key,
				Types.Bool | Types.EqualTo,
				new [] { value }
			);
		}
		
		public static class Int
		{
			public static PropertyValidation Defined(PropertyKey<int> key) => new PropertyValidation(
				key.Key,
				Types.Int | Types.Defined
			);
			
			public static PropertyValidation EqualTo(PropertyKey<int> key, int value) => new PropertyValidation(
				key.Key,
				Types.Int | Types.EqualTo,
				intOperands: new [] { value }
			);
			
			public static PropertyValidation LessThan(PropertyKey<int> key, int value) => new PropertyValidation(
				key.Key,
				Types.Int | Types.LessThan,
				intOperands: new [] { value }
			);
			
			public static PropertyValidation GreaterThan(PropertyKey<int> key, int value) => new PropertyValidation(
				key.Key,
				Types.Int | Types.GreaterThan,
				intOperands: new [] { value }
			);
			
			public static PropertyValidation LessThanOrEqualTo(PropertyKey<int> key, int value) => new PropertyValidation(
				key.Key,
				Types.Int | Types.LessThan | Types.EqualTo,
				intOperands: new [] { value }
			);
			
			public static PropertyValidation GreaterThanOrEqualTo(PropertyKey<int> key, int value) => new PropertyValidation(
				key.Key,
				Types.Int | Types.GreaterThan | Types.EqualTo,
				intOperands: new [] { value }
			);
		}
		
		public static class Long
		{
			public static PropertyValidation Defined(PropertyKey<long> key) => new PropertyValidation(
				key.Key,
				Types.Long | Types.Defined
			);
			
			public static PropertyValidation EqualTo(PropertyKey<long> key, long value) => new PropertyValidation(
				key.Key,
				Types.Long | Types.EqualTo,
				longOperands: new [] { value }
			);
			
			public static PropertyValidation LessThan(PropertyKey<long> key, long value) => new PropertyValidation(
				key.Key,
				Types.Long | Types.LessThan,
				longOperands: new [] { value }
			);
			
			public static PropertyValidation GreaterThan(PropertyKey<long> key, long value) => new PropertyValidation(
				key.Key,
				Types.Long | Types.GreaterThan,
				longOperands: new [] { value }
			);
			
			public static PropertyValidation LessThanOrEqualTo(PropertyKey<long> key, long value) => new PropertyValidation(
				key.Key,
				Types.Long | Types.LessThan | Types.EqualTo,
				longOperands: new [] { value }
			);
			
			public static PropertyValidation GreaterThanOrEqualTo(PropertyKey<long> key, long value) => new PropertyValidation(
				key.Key,
				Types.Long | Types.GreaterThan | Types.EqualTo,
				longOperands: new [] { value }
			);
		}
		
		public static class Float
		{
			public static PropertyValidation Defined(PropertyKey<float> key) => new PropertyValidation(
				key.Key,
				Types.Float | Types.Defined
			);
			
			public static PropertyValidation EqualTo(PropertyKey<float> key, float value) => new PropertyValidation(
				key.Key,
				Types.Float | Types.EqualTo,
				floatOperands: new [] { value }
			);
			
			public static PropertyValidation LessThan(PropertyKey<float> key, float value) => new PropertyValidation(
				key.Key,
				Types.Float | Types.LessThan,
				floatOperands: new [] { value }
			);
			
			public static PropertyValidation GreaterThan(PropertyKey<float> key, float value) => new PropertyValidation(
				key.Key,
				Types.Float | Types.GreaterThan,
				floatOperands: new [] { value }
			);
			
			public static PropertyValidation LessThanOrEqualTo(PropertyKey<float> key, float value) => new PropertyValidation(
				key.Key,
				Types.Float | Types.LessThan | Types.EqualTo,
				floatOperands: new [] { value }
			);
			
			public static PropertyValidation GreaterThanOrEqualTo(PropertyKey<float> key, float value) => new PropertyValidation(
				key.Key,
				Types.Float | Types.GreaterThan | Types.EqualTo,
				floatOperands: new [] { value }
			);
		}
		
		public static class String
		{
			public static PropertyValidation Defined(PropertyKey<string> key) => new PropertyValidation(
				key.Key,
				Types.String | Types.Defined
			);
			
			public static PropertyValidation EqualTo(PropertyKey<string> key, string value) => new PropertyValidation(
				key.Key,
				Types.String | Types.EqualTo,
				stringOperands: new [] { value }
			);
			
			public static PropertyValidation Contains(PropertyKey<string> key, string value) => new PropertyValidation(
				key.Key,
				Types.String | Types.Contains,
				stringOperands: new [] { value }
			);
			
			public static PropertyValidation StartsWith(PropertyKey<string> key, string value) => new PropertyValidation(
				key.Key,
				Types.String | Types.StartsWith,
				stringOperands: new [] { value }
			);
			
			public static PropertyValidation EndsWith(PropertyKey<string> key, string value) => new PropertyValidation(
				key.Key,
				Types.String | Types.EndsWith,
				stringOperands: new [] { value }
			);
		}
	}
}