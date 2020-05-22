namespace Lunra.Hothouse.Models
{
	public struct Hint
	{
		/*
		Hint.DismissTriggers
		- Unknown
		- Timeout 
		- ClickConfirm
		- Condition

			Hint.Conditions
		- Unknown
		- None
		- SingleFire
		- FireExtinguishing
		- ZeroBeds
		- LowRations
		- LowStalks
		- LowScrap
		- ZeroOpenDoors

		struct Hint.Condition
		- Conditions[] All
		- Conditions[] Any
		- Conditions[] None
		- bool IsTriggered(GameModel game)
			- bool Evaluate(GameModel game, Conditions condition)

		struct Hint
		- Condition TriggerCondition
		- DismissTriggers DismissTrigger
		- float DismissTimeout
		- Condition DismissCondition
		- string Message
		- States State

		struct HintCollection
		- Hint[] Hints
		- States State

		class HintsModel
		- HintCollection[] HintCollections
		*/

		public enum DismissTriggers
		{
			Unknown = 0,
			Timeout = 10,
			Confirmed = 20,
			Condition = 30
		}
	}
}