using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;

namespace Lunra.Hothouse.Models
{
	public interface IRecipeModel : IObligationModel, IInventoryModel
	{
		RecipeComponent Recipes { get; }
	}

	public class RecipeComponent : Model
	{
		#region Serialized
		[JsonProperty] Recipe[] available = new Recipe[0];
		protected readonly ListenerProperty<Recipe[]> availableListener;
		[JsonIgnore] public ReadonlyProperty<Recipe[]> Available { get; }

		[JsonProperty] Queue<Recipe> queue = new Queue<Recipe>();
		[JsonIgnore] public QueueProperty<Recipe> Queue { get; }

		[JsonProperty] Recipe current;
		[JsonIgnore] public ListenerProperty<Recipe> Current { get; }
		#endregion
		
		#region Non Serialized 
		#endregion

		public RecipeComponent()
		{
			Available = new ReadonlyProperty<Recipe[]>(
					value => available = value,
					() => available,
					out availableListener
			);
			Queue = new QueueProperty<Recipe>(queue);
			Current = new ListenerProperty<Recipe>(value => current = value, () => current);
		}
		
		public void Reset(
			params Recipe[] available
		)
		{
			availableListener.Value = available;
			Queue.Clear();
			Current.Value = null;
		}
		
		public override string ToString()
		{
			var result = "Recipes: ";

			if (Current.Value != null)
			{
				result += "\n\tCURRENT\n\t"+Current.Value;
			}
			
			var resultForQueue = string.Empty;

			var queueIndex = 0;
			foreach (var next in Queue.PeekAll()) resultForQueue += "\n\t - " + next;

			if (!string.IsNullOrEmpty(resultForQueue)) resultForQueue = "\n\tQUEUED" + resultForQueue;
			
			if (Available.Value.None())
			{
				if (string.IsNullOrEmpty(resultForQueue)) result += "Empty";
				else result += "No Available Recipes but queue has entries, this is invalid...\n";
			}
			else
			{
				result += "\n\tAVAILABLE";
				foreach (var next in Available.Value) result += "\n\t - " + next;
			}
			
			result += resultForQueue; 
			
			return result;
		}
	}
}