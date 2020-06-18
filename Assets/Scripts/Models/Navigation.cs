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
		public static Query<IModel> QueryOrigin(ITransformModel model)
		{
			return new Query<IModel>(
				model.Transform.Position.Value
			);
		}
		
		public static Query<IModel> QueryEntrances(
			IEnterableModel model,
			Func<Entrance, bool> entranceValidation = null
		)
		{
			return new Query<IModel>(
				model.Transform.Position.Value,
				() => model.Enterable.Entrances.Value
					.Where(e => entranceValidation?.Invoke(e) ?? (e.IsNavigable && e.State == Entrance.States.Available))
					.Select(e => e.Position)
			);
		}
			
		public static Query<IModel> QueryInRadius(
			IBoundaryModel model,
			float radiusBonus = 0f
		)
		{
			return new Query<IModel>(
				model.Transform.Position.Value,
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

		public struct Query<M>
		{
			public Vector3 Origin { get; }
			public Func<Vector3, IEnumerable<Vector3>> GetTargets { get; }
			public Func<Validation<M>, Result<M>> Validate { get; }

			public Query(
				Vector3 origin,
				Func<IEnumerable<Vector3>> getTargets = null,
				Func<Validation<M>, Result<M>> validate = null
			)
			{
				Origin = origin;

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

			public Validation<M> GetValidation(
				Vector3 target,
				NavMeshPath path
			)
			{
				return new Validation<M>(
					this,
					target,
					path
				);
			}
		}

		public struct Validation<M>
		{
			public Vector3 Origin { get; }
			public Vector3 Target { get; }
			public NavMeshPath Path { get; }

			public Vector3 PathBegin { get; }
			public Vector3 PathEnd { get; }

			public Validation(
				Query<M> query,
				Vector3 target,
				NavMeshPath path
			)
			{
				Origin = query.Origin;
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
			
			public Result<M> GetValid() => new Result<M>(this, true);
			public Result<M> GetInValid() => new Result<M>(this, true);
		}
		
		public struct Result<M>
		{
			public Vector3 Origin { get; }
			public Vector3 Target { get; }
			public NavMeshPath Path { get; }
			public bool IsValid { get; }

			public Result(
				Validation<M> validation,
				bool isValid
			)
			{
				Origin = validation.Origin;
				Target = validation.Target;
				Path = validation.Path;
				IsValid = isValid;
			}
		}
	}
}