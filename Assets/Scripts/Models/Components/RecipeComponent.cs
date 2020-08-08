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

	public class RecipeComponent : ComponentModel<IRecipeModel>
	{
		public enum States
		{
			Unknown = 0,
			Idle = 10,
			Gathering = 20,
			Ready = 30,
			Crafting = 40
		}


		public enum Iterations
		{
			Unknown = 0,
			Count = 10,
			Desired = 20,
			Infinite = 30
		}
		
		public class RecipeIteration
		{
			public static RecipeIteration Default()
			{
				return new RecipeIteration(
					States.Idle,
					Iterations.Unknown,
					null,
					0,
					0,
					0
				);
			}

			public static RecipeIteration ForCount(
				Recipe recipe,
				int count
			)
			{
				return new RecipeIteration(
					States.Idle,
					Iterations.Count,
					recipe,
					0,
					count,
					0
				);
			}
			
			public static RecipeIteration ForDesired(
				Recipe recipe,
				int desiredMultiplier
			)
			{
				return new RecipeIteration(
					States.Idle,
					Iterations.Desired,
					recipe,
					0,
					0,
					desiredMultiplier
				);
			}
			
			public static RecipeIteration ForInfinity(
				Recipe recipe
			)
			{
				return new RecipeIteration(
					States.Idle,
					Iterations.Infinite,
					recipe,
					0,
					0,
					0
				);
			}
			
			public States State { get; private set; }
			public Iterations Iteration { get; }
			public Recipe Recipe { get; }

			public int Count { get; private set; }
			public int CountTarget { get; }
			public int DesiredMultiplier { get; }

			RecipeIteration(
				States state,
				Iterations iteration,
				Recipe recipe,
				int count,
				int countTarget,
				int desiredMultiplier
			)
			{
				State = state;
				Iteration = iteration;
				Recipe = recipe;
				Count = count;
				CountTarget = countTarget;
				DesiredMultiplier = desiredMultiplier;
			}

			public void Process(
				States state
			)
			{
				var newCount = Count;
				
				switch (Iteration)
				{
					case Iterations.Count:
						if (State == States.Idle)
						{
							if (state != States.Gathering) Debug.LogError("Expected to go from " + States.Idle + " to " + States.Gathering + " but went to " + state + " instead");
							else newCount = Mathf.Min(CountTarget, newCount + 1);
						}
						break;
					case Iterations.Infinite:
					case Iterations.Desired:
						break;
					default:
						Debug.LogError("Unrecognized Iteration: "+Iteration);
						break;
				}

				State = state;
				Count = newCount;
			}

			public bool IsDone(GameModel game)
			{
				switch (Iteration)
				{
					case Iterations.Count:
						return CountTarget <= Count;
					case Iterations.Infinite:
						return false;
					case Iterations.Desired:
						return game.Cache.Value.GlobalInventory.Available.Value
							.Contains(Recipe.OutputItems * DesiredMultiplier);
					default:
						Debug.LogError("Unrecognized Reload Option: " + Iteration);
						return true;
				}
			}

			public override string ToString()
			{
				if (Recipe == null)
				{
					return "NoRecipe." + State;
				}

				var result = Recipe.Name + "." + State;

				switch (Iteration)
				{
					case Iterations.Count:
						result += " [ " + Count + " / " + CountTarget + " ]";
						break;
					case Iterations.Desired:
						result += " [ Desired x " + DesiredMultiplier + " ]";
						break;
					case Iterations.Infinite:
						result += " [ Infinite ]";
						break;
					default:
						Debug.LogError("Unrecognized Iteration: " + Iteration);
						break;
				}

				return result;
			}
		}
		
		#region Serialized
		[JsonProperty] Recipe[] available = new Recipe[0];
		protected readonly ListenerProperty<Recipe[]> availableListener;
		[JsonIgnore] public ReadonlyProperty<Recipe[]> Available { get; }

		[JsonProperty] RecipeIteration[] queue = new RecipeIteration[0];
		[JsonIgnore] public ListenerProperty<RecipeIteration[]> Queue { get; }

		[JsonProperty] int? currentIndex;
		readonly ListenerProperty<int?> currentIndexListener;
		[JsonIgnore] public ReadonlyProperty<int?> CurrentIndex { get; }
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
			Queue = new ListenerProperty<RecipeIteration[]>(
				value => queue = value,
				() => queue
			);
			CurrentIndex = new ReadonlyProperty<int?>(
				value => currentIndex = value,
				() => currentIndex,
				out currentIndexListener
			);
		}

		public void ProcessRecipe() => TryProcessRecipe(out _);
		
		public bool TryGetCurrent(out RecipeIteration current)
		{
			if (CurrentIndex.Value.HasValue && Queue.Value.Length <= CurrentIndex.Value.Value)
			{
				current = null;
				return false;
			}
			
			current = CurrentIndex.Value.HasValue ? Queue.Value[CurrentIndex.Value.Value] : null;
			return CurrentIndex.Value.HasValue;
		}
		
		bool TryProcessRecipe(
			out RecipeIteration result	
		)
		{
			if (!TryGetCurrent(out result)) result = RecipeIteration.Default(); 
			var previousState = result.State;
			
			switch (result.State)
			{
				case States.Idle:
					var noneFound = true;
					for (var i = 0; i < Queue.Value.Length; i++)
					{
						if (Queue.Value[i].IsDone(Game)) continue;
						
						result = Queue.Value[i];
						Model.Inventory.Desired.Value = InventoryDesire.UnCalculated(result.Recipe.InputItems);
						result.Process(States.Gathering);
						currentIndexListener.Value = i;
						noneFound = false;
						break;
					}

					if (noneFound)
					{
						result = RecipeIteration.Default();
						currentIndexListener.Value = null;
						Model.Inventory.Desired.Value = InventoryDesire.UnCalculated(Inventory.Empty);
					}
					break;
				case States.Gathering:
					if (Model.Inventory.Available.Value.Intersects(result.Recipe.InputItems))
					{
						var isOutputCapacityAvailable = Model.Inventory.AvailableCapacity.Value
							.GetCapacityFor(Model.Inventory.Available.Value)
							.Contains(result.Recipe.OutputItems);

						if (isOutputCapacityAvailable)
						{
							Model.Inventory.Desired.Value = InventoryDesire.UnCalculated(Inventory.Empty);
							Model.Inventory.Remove(result.Recipe.InputItems);
							Model.Obligations.Add(ObligationCategories.Craft.Recipe);
							result.Process(States.Ready);
						}
					}
					break;
				case States.Ready:
					result.Process(States.Crafting);
					break;
				case States.Crafting:
					Model.Inventory.Add(result.Recipe.OutputItems);
					result.Process(States.Idle);
					break;
				default:
					Debug.LogError("Unrecognized Recipe State: "+result.State);
					break;
			}

			return previousState != result.State;
		}
		
		public void Reset(
			params Recipe[] available
		)
		{
			availableListener.Value = available;
			Queue.Value = new RecipeIteration[0];
			currentIndexListener.Value = null;
		}

		public override string ToString()
		{
			var result = "Recipes: ";

			void appendRecipeIteration(string prefix, RecipeIteration recipeIteration)
			{
				result += "\n - [ " + prefix + " ] " + recipeIteration;
			}
			
			void appendRecipe(string prefix, Recipe recipe)
			{
				result += "\n - [ " + prefix + " ] " + recipe;
			}

			if (TryGetCurrent(out var current))
			{
				switch (current.State)
				{
					case States.Idle:
					case States.Gathering:
					case States.Ready:
					case States.Crafting:
						appendRecipeIteration("Current" + "." + current.State, current);
						break;
					default:
						Debug.LogError("Unrecognized Recipe State: " + current.State);
						break;
				}	
			}
			
			var index = 0;
			foreach (var next in Queue.Value)
			{
				var isCurrent = index == (CurrentIndex.Value ?? -1);
				appendRecipeIteration(nameof(Queue) + " ] " + (isCurrent ? "*" : String.Empty) + " [ " + index, next);
				index++;
			}

			foreach (var next in Available.Value) appendRecipe(nameof(Available), next);

			return result;
		}
	}
}