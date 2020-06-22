namespace Lunra.Hothouse.Models
{
	public static class ObligationCategories
	{
		public static Jobs[] GetJobs(params Jobs[] jobs) => jobs;
		
		public static class Door
		{
			const string Category = "door";
			
			public static bool Contains(ObligationType obligation) => obligation.Category == Category;
			
			static ObligationType New(string action) => new ObligationType(Category, action);

			public static class Actions
			{
				public const string Open = "open";
			}
			
			public static ObligationType Open => New(Actions.Open);
		}

		public static class Clearable
		{
			const string Category = "clearable";
			
			public static bool Contains(ObligationType obligation) => obligation.Category == Category;
			
			static ObligationType New(string action) => new ObligationType(Category, action);

			public static class Actions
			{
				public const string Clear = "clear";
			}
			
			public static ObligationType Clear => New(Actions.Clear);
		}
	}
}