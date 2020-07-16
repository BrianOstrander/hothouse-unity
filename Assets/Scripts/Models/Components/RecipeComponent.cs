using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public interface IRecipeModel : IObligationModel, IInventoryModel
	{
		RecipeComponent Recipes { get; }
	}

	public class RecipeComponent : Model
	{
		public enum States
		{
			Unknown = 0,
			Idle = 10,
			Gathering = 20,
			Ready = 30,
			Crafting = 40
		}

		public struct RecipeState
		{
			public States State { get; }
			public Recipe Recipe { get; }

			public RecipeState(
				States state,
				Recipe recipe = null
			)
			{
				State = state;
				Recipe = recipe;
			}

			public RecipeState New(States state) => new RecipeState(state, Recipe);
		}
		
		#region Serialized
		[JsonProperty] Recipe[] available = new Recipe[0];
		protected readonly ListenerProperty<Recipe[]> availableListener;
		[JsonIgnore] public ReadonlyProperty<Recipe[]> Available { get; }

		[JsonProperty] Queue<Recipe> queue = new Queue<Recipe>();
		[JsonIgnore] public QueueProperty<Recipe> Queue { get; }

		[JsonProperty] RecipeState current;
		readonly ListenerProperty<RecipeState> currentListener;
		[JsonIgnore] public ReadonlyProperty<RecipeState> Current { get; }
		
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
			Current = new ReadonlyProperty<RecipeState>(
				value => current = value,
				() => current,
				out currentListener
			);
		}

		public void ProcessRecipe(IRecipeModel model) => TryProcessRecipe(model, out _);
		
		bool TryProcessRecipe(
			IRecipeModel model,
			out RecipeState result	
		)
		{
			var previousState = Current.Value.State;
			result = Current.Value;
			
			switch (Current.Value.State)
			{
				case States.Idle:
					if (Queue.TryDequeue(out var next))
					{
						model.Inventory.Desired.Value = InventoryDesire.UnCalculated(next.InputItems);
						currentListener.Value = (result = new RecipeState(States.Gathering, next));
					}
					break;
				case States.Gathering:
					if (model.Inventory.Available.Value.Intersects(Current.Value.Recipe.InputItems))
					{
						model.Inventory.Desired.Value = InventoryDesire.Ignored();
						model.Inventory.Remove(Current.Value.Recipe.InputItems);
						model.Obligations.Add(ObligationCategories.Craft.Recipe);
						currentListener.Value = (result = Current.Value.New(States.Ready));
					}
					break;
				case States.Ready:
					currentListener.Value = (result = Current.Value.New(States.Crafting));
					break;
				case States.Crafting:
					model.Inventory.Desired.Value = InventoryDesire.UnCalculated(Inventory.Empty);
					model.Inventory.Add(Current.Value.Recipe.OutputItems);
					currentListener.Value = (result = new RecipeState(States.Idle));
					break;
				default:
					Debug.LogError("Unrecognized Recipe State: "+Current.Value.State);
					break;
			}

			return previousState != result.State;
		}
		
		public void Reset(
			params Recipe[] available
		)
		{
			availableListener.Value = available;
			Queue.Clear();
			currentListener.Value = new RecipeState(States.Idle);
		}

		public override string ToString()
		{
			var result = "Recipes: ";

			void appendRecipe(string prefix, Recipe recipe)
			{
				result += "\n - [ " + prefix + " ] " + recipe;
			}
			
			switch (Current.Value.State)
			{
				case States.Idle:
				case States.Gathering:
				case States.Ready:
				case States.Crafting:
					appendRecipe(nameof(Current) + "." + Current.Value.State, Current.Value.Recipe);
					break;
				default:
					Debug.LogError("Unrecognized Recipe State: " + Current.Value.State);
					break;
			}
			
			var index = 0;
			foreach (var next in Queue.PeekAll())
			{
				appendRecipe(nameof(Queue) + " ] [ " + index, next);
				index++;
			}

			foreach (var next in Available.Value) appendRecipe(nameof(Available), next);

			return result;
		}
	}
}