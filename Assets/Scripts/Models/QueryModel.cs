using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.StyxMvp.Models;

namespace Lunra.Hothouse.Models
{
	public class QueryModel : Model
	{
		#region Serialized
		#endregion
		
		#region Non Serialized
		(Type ModelType, Func<IEnumerable<IModel>> GetModels)[] all;
		#endregion

		public QueryModel(
			params (Type ModelType, Func<IEnumerable<IModel>> GetModels)[] all	
		)
		{
			this.all = all;
		}
		
		public IEnumerable<M> All<M>()
			where M : IModel
		{
			return all
				.Where(p => typeof(M).IsAssignableFrom(p.ModelType))
				.SelectMany(p => p.GetModels())
				.Cast<M>();
		}
		
		public IEnumerable<M> All<M>(Func<M, bool> predicate)
			where M : IModel
		{
			return All<M>().Where(predicate);
		}

		public bool TryFindFirst<M>(
			out M result
		)
			where M : IModel
		{
			foreach (var element in all)
			{
				if (!typeof(M).IsAssignableFrom(element.ModelType)) continue;
				try
				{
					if (element.GetModels().First() is M modelTyped)
					{
						result = modelTyped;
						return true;
					}
				}
				catch (InvalidOperationException) {}
			}

			result = default;
			return false;
		}
		
		public bool TryFindFirst<M>(
			Func<M, bool> predicate,
			out M result
		)
			where M : IModel
		{
			foreach (var element in all)
			{
				if (!typeof(M).IsAssignableFrom(element.ModelType)) continue;
				try
				{
					foreach (var model in element.GetModels())
					{
						if (model is M modelTyped && predicate(modelTyped))
						{
							result = modelTyped;
							return true;
						}
					}
				}
				catch (InvalidOperationException) {}
			}

			result = default;
			return false;
		}
		
		public bool TryFindFirst<M>(
			string id,
			out M result
		)
			where M : IModel
		{
			return TryFindFirst(m => m.Id.Value == id, out result);
		}
		
		public M FirstOrDefault<M>()
			where M : IModel
		{
			return TryFindFirst(out M result) ? result : default;
		}
		
		public M FirstOrDefault<M>(Func<M, bool> predicate)
			where M : IModel
		{
			return TryFindFirst(predicate, out M result) ? result : default;
		}
		
		public M FirstOrDefault<M>(string id)
			where M : IModel
		{
			return TryFindFirst(id, out M result) ? result : default;
		}
		
		public bool Any<M>()
			where M : IModel
		{
			return FirstOrDefault<M>() != null;
		}
		
		public bool Any<M>(Func<M, bool> predicate)
			where M : IModel
		{
			return FirstOrDefault(predicate) != null;
		}
	}
}