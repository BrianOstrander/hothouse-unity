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
	public interface IObligationPromiseModel : IRoomTransformModel
	{
		ObligationPromiseComponent ObligationPromises { get; }
	}

	public class ObligationPromiseComponent : ComponentModel<IObligationPromiseModel>
	{
		#region Serialized
		[JsonProperty] List<ObligationPromise> all = new List<ObligationPromise>();
		[JsonIgnore] public StackProperty<ObligationPromise> All { get; }
		#endregion
		
		#region Non Serialized
		[JsonIgnore] public Action<Obligation> Complete { get; set; } = ActionExtensions.GetEmpty<Obligation>();
		#endregion
		
		public bool HasAny() => all.Any();

		public ObligationPromiseComponent()
		{
			All = new StackProperty<ObligationPromise>(all);
		}

		public void BreakPromise()
		{
			if (!All.TryPop(out var promise)) return;
			if (!promise.Target.TryGetInstance<IObligationModel>(Game, out var target)) return;
			target.Obligations.RemoveForbidden(promise.Obligation);
		}
		
		public void BreakAllPromises()
		{
			while (All.Any()) BreakPromise();
		}

		public void Reset()
		{
			All.Clear();
			Complete = ActionExtensions.GetEmpty<Obligation>();
			ResetId();
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