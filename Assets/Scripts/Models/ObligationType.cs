namespace Lunra.Hothouse.Models
{
	public struct ObligationType
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
	}
}