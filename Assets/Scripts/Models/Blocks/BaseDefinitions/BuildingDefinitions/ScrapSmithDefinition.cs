namespace Lunra.Hothouse.Models
{
	public class ScrapSmithDefinition : BuildingDefinition
	{
		// public override Recipe[] Recipes => new[]
		// {
		// 	new Recipe(
		// 		"craft_tool_sharp",
		// 		Inventory.FromEntries(
		// 			(Inventory.Types.Stalk, 1),
		// 			(Inventory.Types.Scrap, 1)
		// 		),
		// 		Inventory.FromEntry(Inventory.Types.ToolSharp, 1),
		// 		DayTime.FromRealSeconds(30f)
		// 	),
		// 	new Recipe(
		// 		"craft_tool_blunt",
		// 		Inventory.FromEntries(
		// 			(Inventory.Types.Stalk, 1),
		// 			(Inventory.Types.Scrap, 1)
		// 		),
		// 		Inventory.FromEntry(Inventory.Types.ToolBlunt, 1),
		// 		DayTime.FromRealSeconds(30f)
		// 	),
		// 	new Recipe(
		// 		"craft_tool_sling",
		// 		Inventory.FromEntries(
		// 			(Inventory.Types.Stalk, 1),
		// 			(Inventory.Types.Scrap, 1)
		// 		),
		// 		Inventory.FromEntry(Inventory.Types.ToolSling, 1),
		// 		DayTime.FromRealSeconds(30f)
		// 	)
		// };

		public override int MaximumOwners => 1;
	}
}