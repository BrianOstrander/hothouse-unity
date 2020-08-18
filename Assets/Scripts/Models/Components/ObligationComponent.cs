using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.StyxMvp;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public interface IObligationModel : IEnterableModel
	{
		ObligationComponent Obligations { get; }
	}

	public class ObligationComponent : ComponentModel<IObligationModel>
	{
		public class State
		{
			[JsonProperty] public Obligation[] Available { get; private set; }
			[JsonProperty] public Obligation[] Forbidden { get; private set; }

			public State()
			{
				Available = new Obligation[0];
				Forbidden = new Obligation[0];
			}
			
			public State(
				Obligation[] available,
				Obligation[] forbidden
			)
			{
				Available = available;
				Forbidden = forbidden;
			}
		}

		public delegate void ObligationComplete(Obligation obligation, IModel source);
		
		#region Serialized
		[JsonProperty] State all = new State();
		[JsonIgnore] public ListenerProperty<State> All { get; }
		#endregion

		#region Non Serialized
		Dictionary<string, List<ObligationComplete>> bindings = new Dictionary<string, List<ObligationComplete>>();
		#endregion

		public ObligationComponent()
		{
			All = new ListenerProperty<State>(value => all = value, () => all);
		}

		public void Add(Obligation obligation)
		{
			All.Value = new State(
				All.Value.Available.Append(obligation).ToArray(),
				All.Value.Forbidden
			);
		}
		
		public bool Remove(Obligation obligation)
		{
			if (All.Value.Available.None(o => o.Type == obligation.Type)) return false;

			var availableList = new List<Obligation>(All.Value.Available);
			availableList.Remove(availableList.First(o => o.Type == obligation.Type));
			
			All.Value = new State(
				availableList.ToArray(),
				All.Value.Forbidden
			);
			
			return true;
		}
		
		public bool AddForbidden(Obligation obligation)
		{
			if (All.Value.Available.None(o => o.Type == obligation.Type)) return false;

			var availableList = new List<Obligation>(All.Value.Available);
			availableList.Remove(availableList.First(o => o.Type == obligation.Type));
			
			All.Value = new State(
				availableList.ToArray(),
				All.Value.Forbidden.Append(obligation).ToArray()
			);
			
			return true;
		}
		
		public bool RemoveForbidden(
			Obligation obligation,
			bool appendToAvailable = true
		)
		{
			if (All.Value.Forbidden.None(o => o.Type == obligation.Type)) return false;

			var forbiddenList = new List<Obligation>(All.Value.Forbidden);
			forbiddenList.Remove(forbiddenList.First(o => o.Type == obligation.Type));
			
			All.Value = new State(
				appendToAvailable ? All.Value.Available.Append(obligation).ToArray() : All.Value.Available,
				forbiddenList.ToArray()
			);
			
			return true;
		}

		public bool RemoveAny(Obligation obligation)
		{
			var result = Remove(obligation);
			result |= RemoveForbidden(obligation, false);
			return result;
		}

		public bool HasAny() => All.Value.Available.Any() || All.Value.Forbidden.Any();
		public bool HasAny(Obligation obligation) => HasAvailable(obligation) || HasForbidden(obligation);
		public bool HasForbidden(Obligation obligation) => All.Value.Forbidden.Any(o => o.Type == obligation.Type);
		public bool HasAvailable(Obligation obligation) => All.Value.Available.Any(o => o.Type == obligation.Type);
		public int CountForbidden(Obligation obligation) => All.Value.Forbidden.Count(o => o.Type == obligation.Type);

		public bool Trigger(
			Obligation obligation,
			IModel source
		)
		{
			if (!RemoveForbidden(obligation, false)) return false;

			if (!bindings.TryGetValue(obligation.Type, out var callbacks))
			{
				// Debug.LogError("No listeners are bound to obligation type: "+obligation.Type);
				return false;
			}

			// Assigning to an array so we know it's not modified...
			foreach (var callback in callbacks.ToArray())
			{
				try { callback(obligation, source); }
				catch (Exception e) { Debug.LogException(e); }
			}

			return true;
		}

		#region Binding
		public void AddCallback(
			Obligation obligation,
			ObligationComplete callback
		)
		{
			if (!bindings.TryGetValue(obligation.Type, out var callbacks))
			{
				bindings.Add(
					obligation.Type,
					callbacks = new List<ObligationComplete>()
				);
			}
			
			callbacks.Add(callback);
		}
		
		public void RemoveCallback(
			Obligation obligation,
			ObligationComplete callback
		)
		{
			if (!bindings.TryGetValue(obligation.Type, out var callbacks)) return;

			callbacks.Remove(callback);
		}

		public void ClearCallbacks() => bindings.Clear();
		
		public void ClearCallbacks(string type)
		{
			if (!bindings.TryGetValue(type, out var callbacks)) return;
			callbacks.Clear();
		}
		#endregion

		public void Reset()
		{
			Id.Value = App.M.CreateUniqueId();
			ClearCallbacks();
			All.Value = new State();
		}

		public override string ToString()
		{
			var result = "Obligations: ";
			foreach (var obligation in All.Value.Available)
			{
				result += "\n - [ available ] " + obligation;
			}
				
			foreach (var obligation in All.Value.Forbidden)
			{
				result += "\n - [ forbidden ] " + obligation;
			}

			return result;
		}
	}
}