using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.StyxMvp.Models;
using UnityEngine;
using UnityEngine.AI;

namespace Lunra.Hothouse.Models
{
	public static class Navigation
	{
		public static bool TryQuery(
			IModel model,
			out Query query
		)
		{
			switch (model)
			{
				case IEnterableModel modelEnterable:
					query = QueryEntrances(modelEnterable);
					break;
				case IAgentInventoryModel modelAgent:
					query = QueryOrigin(modelAgent);
					break;
				default:
					Debug.LogError("Unrecognized type: "+model.GetType());
					query = default;
					return false;
			}

			return true;
		}
		
		public static Query QueryOrigin(ITransformModel model)
		{
			return new Query(
				model.Transform.Position.Value,
				model
			);
		}
		
		public static Query QueryEntrances(
			IEnterableModel model,
			Func<Entrance, bool> entranceValidation = null
		)
		{
			return new Query(
				model.Transform.Position.Value,
				model,
				getTargets: () => model.Enterable.Entrances.Value
					.Where(e => entranceValidation?.Invoke(e) ?? (e.IsNavigable && e.State == Entrance.States.Available))
					.Select(e => e.Position)
			);
		}
			
		public static Query QueryInRadius(
			IBoundaryModel model,
			float radiusBonus = 0f
		)
		{
			return new Query(
				model.Transform.Position.Value,
				model,
				model.Boundary.Radius.Value + radiusBonus,
				validate: validation =>
				{
					switch (validation.Path.status)
					{
						case NavMeshPathStatus.PathComplete:
							return validation.GetValid();
						case NavMeshPathStatus.PathInvalid:
							return validation.GetInValid();
					}
						
					if (Vector3.Distance(validation.PathEnd, validation.Target) < (model.Boundary.Radius.Value + radiusBonus))
					{
						return validation.GetValid();
					}

					return validation.GetInValid();
				}
			);
		}

		public struct Query
		{
			public Vector3 Origin { get; }
			public IModel TargetModel { get; }
			public float MaximumRadius { get; }
			public Func<Vector3, IEnumerable<Vector3>> GetTargets { get; }
			public Func<Validation, Result> Validate { get; }

			public Query(
				Vector3 origin,
				IModel targetModel,
				float maximumRadius = 0f,
				Func<IEnumerable<Vector3>> getTargets = null,
				Func<Validation, Result> validate = null
			)
			{
				Origin = origin;
				TargetModel = targetModel;
				MaximumRadius = maximumRadius;

				if (getTargets == null)
				{
					GetTargets = beginPosition => origin.ToEnumerable();
				}
				else
				{
					GetTargets = beginPosition => getTargets()
						.OrderBy(targetPosition => Vector3.Distance(beginPosition, targetPosition));
				}
				
				Validate = validate;
			}

			public float GetMinimumTargetDistance(Vector3 beginPosition) => Vector3.Distance(beginPosition, GetTargets(beginPosition).First());

			public Validation GetValidation(
				Vector3 target,
				NavMeshPath path
			)
			{
				return new Validation(
					this,
					target,
					path
				);
			}
		}

		public struct Validation
		{
			public Vector3 Origin { get; }
			public IModel TargetModel { get; }
			public Vector3 Target { get; }
			public NavMeshPath Path { get; }

			public Vector3 PathBegin { get; }
			public Vector3 PathEnd { get; }

			public Validation(
				Query query,
				Vector3 target,
				NavMeshPath path
			)
			{
				Origin = query.Origin;
				TargetModel = query.TargetModel;
				Target = target;
				Path = path;

				switch (path.status)
				{
					case NavMeshPathStatus.PathComplete:
					case NavMeshPathStatus.PathPartial:
						PathBegin = path.corners.FirstOrDefault();
						PathEnd = path.corners.LastOrDefault();
						break;
					default:
						PathBegin = default;
						PathEnd = default;
						break;
				}
			}
			
			public Result GetValid() => new Result(this, true);
			public Result GetInValid() => new Result(this, false);
		}
		
		public struct Result
		{
			public Vector3 Origin { get; }
			public IModel TargetModel { get; }
			public Vector3 Target { get; }
			public NavMeshPath Path { get; }
			public bool IsValid { get; }

			public Result(
				Validation validation,
				bool isValid
			)
			{
				Origin = validation.Origin;
				TargetModel = validation.TargetModel;
				Target = validation.Target;
				Path = validation.Path;
				IsValid = isValid;
			}
		}
	}
}