using System;
using System.Linq;
using Lunra.Core;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public struct Obligation
	{
		static string GenerateId => Guid.NewGuid().ToString(); 
		
		public static Obligation New(
			ObligationType type,
			int priority,
			Jobs[] validJobs,
			ConcentrationRequirements concentrationRequirement,
			Interval concentrationElapsed
		)
		{
			return new Obligation(
				GenerateId,
				GenerateId,
				type,
				States.NotInitialized,
				priority,
				validJobs,
				concentrationRequirement,
				concentrationElapsed
			);
		}
		
		public enum States
		{
			Unknown = 0,
			NotInitialized = 10,
			Blocked = 20,
			Available = 30,
			Promised = 40,
			Complete = 50
		}

		public enum ConcentrationRequirements
		{
			Unknown = 0,
			Instant = 10,
			Interruptible = 20,
			NonInterruptible = 30
		}

		/// <summary>
		/// The static Id this promise is assigned, it will not change even if the state changes.
		/// </summary>
		public string Id { get; private set; }
		/// <summary>
		/// The promise Id, which is changed anytime the state of this obligation changes in a way that would require
		/// agents to recalculate if they can even access it.
		/// </summary>
		public string PromiseId { get; private set; }
		public ObligationType Type { get; private set; }
		public States State { get; private set; }
		public int Priority { get; private set; }
		public Jobs[] ValidJobs { get; private set; }
		public ConcentrationRequirements ConcentrationRequirement { get; private set; }
		public Interval ConcentrationElapsed { get; private set; }

		public bool IsValidJob(Jobs job) => ValidJobs.None() || ValidJobs.Contains(job);
		[JsonIgnore] public bool IsValid => !(State == States.Unknown || ConcentrationRequirement == ConcentrationRequirements.Unknown || string.IsNullOrEmpty(Id) || string.IsNullOrEmpty(PromiseId));
		
		Obligation(
			string id,
			string promiseId,
			ObligationType type,
			States state,
			int priority,
			Jobs[] validJobs,
			ConcentrationRequirements concentrationRequirement,
			Interval concentrationElapsed
		)
		{
			Id = id;
			PromiseId = promiseId;
			State = state;
			Type = type;
			Priority = priority;
			ValidJobs = validJobs;
			ConcentrationRequirement = concentrationRequirement;
			ConcentrationElapsed = concentrationElapsed;
		}

		public Obligation NewConcentrationElapsed(Interval concentrationElapsed)
		{
			var result = new Obligation(
				Id,
				PromiseId,
				Type,
				State,
				Priority,
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
					result.ConcentrationElapsed = concentrationElapsed;
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
				PromiseId,
				Type,
				State,
				Priority,
				ValidJobs,
				ConcentrationRequirement,
				ConcentrationElapsed.Restarted()
			);
		}
		
		public Obligation New(
			States? state = null,
			int? priority = null
		)
		{
			return new Obligation(
				Id,
				PromiseId,
				Type,
				state ?? State,
				priority ?? Priority,
				ValidJobs,
				ConcentrationRequirement,
				ConcentrationElapsed.Restarted()
			);
		}
		
		public Obligation NewPromiseId()
		{
			return new Obligation(
				Id,
				GenerateId,
				Type,
				State,
				Priority,
				ValidJobs,
				ConcentrationRequirement,
				ConcentrationElapsed.Restarted()
			);
		}
	}
}