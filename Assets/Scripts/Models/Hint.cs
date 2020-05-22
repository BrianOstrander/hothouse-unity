namespace Lunra.Hothouse.Models
{
	public struct Hint
	{
		/*
		struct HintCollection
		- Hint[] Hints
		- States State

		class HintsModel
		- HintCollection[] HintCollections
		*/

		public enum States
		{
			Unknown = 0,
			Idle = 10,
			Active = 20,
			Dismissed = 30
		}

		public enum DismissTriggers
		{
			Unknown = 0,
			Timeout = 10,
			Condition = 20,
			Confirmed = 30
		}

		States state;
		string message;
		Condition activateCondition;
		
		DismissTriggers dismissTrigger;

		float dismissTimeout;
		Condition dismissCondition;
		
		
	}
}