using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public interface IObligationPromiseModel : IRoomTransformModel
	{
		ObligationPromiseComponent ObligationPromises { get; }
	}

	public class ObligationPromiseComponent : Model
	{
		#region Serialized
		[JsonProperty] Stack<ObligationPromise> all = new Stack<ObligationPromise>();
		[JsonIgnore] public StackProperty<ObligationPromise> All { get; }
		#endregion
		
		#region Non Serialized
		[JsonIgnore] public Action<Obligation> Complete { get; set; } = ActionExtensions.GetEmpty<Obligation>();
		public bool HasAny() => all.Any();
		#endregion

		public ObligationPromiseComponent()
		{
			All = new StackProperty<ObligationPromise>(all);
		}

		public void BreakRemainingPromises(
			GameModel game	
		)
		{
			foreach (var promise in All.PeekAll())
			{
				if (!promise.Target.TryGetInstance<IObligationModel>(game, out var target)) continue;

				target.Obligations.RemoveForbidden(promise.Obligation);
			}	
		}

		public void Reset()
		{
			All.Clear();
			Complete = ActionExtensions.GetEmpty<Obligation>();
		}

		public override string ToString()
		{
			var result = "Obligation Promises:";
			var elements = All.PeekAll();
			
			if (elements.None()) return result + " None";

			foreach (var element in elements)
			{
				result += "\n - " + element;
			}

			return result;
		}
	}
}