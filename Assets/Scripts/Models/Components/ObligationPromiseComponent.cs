using System.Collections.Generic;
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
		#endregion

		public ObligationPromiseComponent()
		{
			All = new StackProperty<ObligationPromise>(all);
		}

		public void BreakRemainingPromises(
			GameModel game	
		)
		{
			Debug.LogWarning("Handle unfulfilled inventory promises here");	
		}

		public void Reset()
		{
			All.Clear();
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