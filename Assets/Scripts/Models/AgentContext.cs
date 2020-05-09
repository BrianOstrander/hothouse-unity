namespace Lunra.Hothouse.Models
{
	public struct AgentContext
	{
		public readonly string Name;
		public readonly string PreviousState;
		public readonly string CurrentState;
		public readonly string Transition;

		public AgentContext(
			string name,
			string previousState,
			string currentState,
			string transition
		)
		{
			Name = name;
			PreviousState = previousState;
			CurrentState = currentState;
			Transition = transition;
		}
		
		public override string ToString() => Name + ": " + PreviousState + "." + Transition + " -> " + CurrentState;
	}
}