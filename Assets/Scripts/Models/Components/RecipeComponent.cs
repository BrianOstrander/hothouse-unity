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


		public enum Iterations
		{
			Unknown = 0,
			Count = 10,
			Desired = 20,
			Infinite = 30
		}

		public struct RecipeIteration
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
					count,
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
					Iterations.Count,
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
			
			public States State { get; }
			public Iterations Iteration { get; }
			public Recipe Recipe { get; }

			public int Count;
			public int CountRemaining;
			public int DesiredMultiplier;

			RecipeIteration(
				States state,
				Iterations iteration,
				Recipe recipe,
				int count,
				int countRemaining,
				int desiredMultiplier
			)
			{
				State = state;
				Iteration = iteration;
				Recipe = recipe;
				Count = count;
				CountRemaining = countRemaining;
				DesiredMultiplier = desiredMultiplier;
			}

			public RecipeIteration New(
				States state
			)
			{
				var newCountRemaining = Count;
				
				switch (Iteration)
				{
					case Iterations.Count:
						if (State == States.Idle)
						{
							if (state != States.Gathering) Debug.LogError("Expected to go from " + States.Idle + " to " + States.Gathering + " but went to " + state + " instead");
							else newCountRemaining = Mathf.Max(0, newCountRemaining - 1);
						}
						break;
					case Iterations.Infinite:
					case Iterations.Desired:
						break;
					default:
						Debug.LogError("Unrecognized Iteration: "+Iteration);
						break;
				}
			
				return new RecipeIteration(
					state,
					Iteration,
					Recipe,
					Count,
					newCountRemaining,
					DesiredMultiplier
				);
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
						result += " [ " + Count + " / " + CountRemaining + " ]";
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

		[JsonProperty] Queue<RecipeIteration> queue = new Queue<RecipeIteration>();
		[JsonIgnore] public QueueProperty<RecipeIteration> Queue { get; }

		[JsonProperty] RecipeIteration current;
		readonly ListenerProperty<RecipeIteration> currentListener;
		[JsonIgnore] public ReadonlyProperty<RecipeIteration> Current { get; }
		
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
			Queue = new QueueProperty<RecipeIteration>(queue);
			Current = new ReadonlyProperty<RecipeIteration>(
				value => current = value,
				() => current,
				out currentListener
			);
		}

		public void ProcessRecipe(GameModel game, IRecipeModel model) => TryProcessRecipe(game, model, out _);
		
		bool TryProcessRecipe(
			GameModel game,
			IRecipeModel model,
			out RecipeIteration result	
		)
		{
			var previousState = Current.Value.State;
			result = Current.Value;
			
			switch (Current.Value.State)
			{
				case States.Idle:
					if (Queue.TryDequeue(out var next))
					{
						model.Inventory.Desired.Value = InventoryDesire.UnCalculated(next.Recipe.InputItems);
						currentListener.Value = (result = next.New(States.Gathering));
					}
					break;
				case States.Gathering:
					if (model.Inventory.Available.Value.Intersects(Current.Value.Recipe.InputItems))
					{
						var isOutputCapacityAvailable = model.Inventory.AvailableCapacity.Value
							.GetCapacityFor(model.Inventory.Available.Value)
							.Contains(Current.Value.Recipe.OutputItems);

						if (isOutputCapacityAvailable)
						{
							model.Inventory.Desired.Value = InventoryDesire.UnCalculated(Inventory.Empty);
							model.Inventory.Remove(Current.Value.Recipe.InputItems);
							model.Obligations.Add(ObligationCategories.Craft.Recipe);
							currentListener.Value = (result = Current.Value.New(States.Ready));
						}
					}
					break;
				case States.Ready:
					currentListener.Value = (result = Current.Value.New(States.Crafting));
					break;
				case States.Crafting:
					//model.Inventory.Desired.Value = InventoryDesire.UnCalculated(Inventory.Empty);
					model.Inventory.Add(Current.Value.Recipe.OutputItems);

					var returnToQueue = false;
					switch (Current.Value.Iteration)
					{
						case Iterations.Count:
							returnToQueue = 0 < Current.Value.CountRemaining;
							break;
						case Iterations.Infinite:
							returnToQueue = true;
							break;
						case Iterations.Desired:
							returnToQueue = !game.Cache.Value.GlobalInventory.Available.Value.Contains(
								Current.Value.Recipe.OutputItems * Current.Value.DesiredMultiplier
							);
							break;
						default:
							Debug.LogError("Unrecognized Reload Option: " + Current.Value.Iteration);
							break;
					}

					if (returnToQueue)
					{
						queue.Enqueue(Current.Value.New(States.Idle));
					}
					
					currentListener.Value = (result = RecipeIteration.Default());
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
			currentListener.Value = RecipeIteration.Default();
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
			
			switch (Current.Value.State)
			{
				case States.Idle:
				case States.Gathering:
				case States.Ready:
				case States.Crafting:
					appendRecipeIteration(nameof(Current) + "." + Current.Value.State, Current.Value);
					break;
				default:
					Debug.LogError("Unrecognized Recipe State: " + Current.Value.State);
					break;
			}
			
			var index = 0;
			foreach (var next in Queue.PeekAll())
			{
				appendRecipeIteration(nameof(Queue) + " ] [ " + index, next);
				index++;
			}

			foreach (var next in Available.Value) appendRecipe(nameof(Available), next);

			return result;
		}
	}
}