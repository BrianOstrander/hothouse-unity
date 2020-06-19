using Newtonsoft.Json;

namespace Lunra.Hothouse.Models
{
#pragma warning disable CS0661 // Defines == or != operator but does not override Object.GetHashCode()
#pragma warning disable CS0659 // Overrides Object.Equals(object) but does not override Object.GetHashCode()
	public struct ObligationType
#pragma warning restore CS0659 // Overrides Object.Equals(object) but does not override Object.GetHashCode()
#pragma warning restore CS0661 // Defines == or != operator but does not override Object.GetHashCode()
	{
		public static ObligationType None() => new ObligationType(null, null);

		public readonly string Category;
		public readonly string Action;

		public ObligationType(
			string category,
			string action
		)
		{
			Category = category;
			Action = action;
		}

		public override string ToString() => Category + "." + Action;
		
		[JsonIgnore] public string PrefabId => "obligation_indicator_" + Category;
		
		public bool Equals(ObligationType type) => Category == type.Category && Action == type.Action;

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;

			return obj.GetType() == GetType() && Equals((ObligationType)obj);
		}
		
		public static bool operator ==(ObligationType type0, ObligationType type1)
		{
			return type0.Category == type1.Category && type0.Action == type1.Action;
		}

		public static bool operator !=(ObligationType type0, ObligationType type1) { return !(type0 == type1); }
	}
}