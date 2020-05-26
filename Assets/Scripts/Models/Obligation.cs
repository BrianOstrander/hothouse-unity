using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public struct Obligation
	{
		public enum States
		{
			Unknown = 0,
			Blocked = 10,
			Promised = 20,
			Available = 30,
			Complete = 40
		}

		public enum ConcentrationRequirements
		{
			Unknown = 0,
			Instant = 10,
			Interruptible = 20,
			NonInterruptible = 30
		}

		public string Id { get; private set; }
		public int Priority { get; private set; }
		public States State { get; private set; }
		public Jobs[] ValidJobs { get; private set; }
		public ConcentrationRequirements ConcentrationRequirement { get; private set; }
		public Interval ConcentrationElapsed { get; private set; }

		public Obligation(
			string id,
			int priority,
			States state,
			Jobs[] validJobs,
			ConcentrationRequirements concentrationRequirement,
			Interval concentrationElapsed
		)
		{
			Id = id;
			Priority = priority;
			State = state;
			ValidJobs = validJobs;
			ConcentrationRequirement = concentrationRequirement;
			ConcentrationElapsed = concentrationElapsed;
		}

		public Obligation Update(float time)
		{
			var result = new Obligation(
				Id,
				Priority,
				State,
				ValidJobs,
				ConcentrationRequirement,
				ConcentrationElapsed
			);
			
			switch (State)
			{
				case States.Promised:
					break;
				case States.Available:
				case States.Blocked:
				case States.Complete:
					Debug.LogError("Trying to update an obligation from an invalid state: "+State);
					return result;
				default:
					Debug.LogError("Unrecognized State: "+State);
					return result;
			}
			
			switch (ConcentrationRequirement)
			{
				case ConcentrationRequirements.Instant:
					result.ConcentrationElapsed = ConcentrationElapsed.Done();
					break;
				case ConcentrationRequirements.Interruptible:
				case ConcentrationRequirements.NonInterruptible:
					result.ConcentrationElapsed = ConcentrationElapsed.Update(time);
					break;
				default:
					Debug.LogError("Unrecognized ConcentrationRequirement: "+ConcentrationRequirement);
					return result;
			}

			if (result.ConcentrationElapsed.IsDone) result.State = States.Complete;
			
			return result;
		}

		public Obligation ResetConcentrationElapsed()
		{
			return new Obligation(
				Id,
				Priority,
				State,
				ValidJobs,
				ConcentrationRequirement,
				ConcentrationElapsed.Restarted()
			);
		}
		
		public Obligation New(
			int? priority = null,
			States? state = null
		)
		{
			return new Obligation(
				Id,
				priority ?? Priority,
				state ?? State,
				ValidJobs,
				ConcentrationRequirement,
				ConcentrationElapsed.Restarted()
			);
		}
	}
}