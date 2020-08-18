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
		
		public M FirstOrDefault<M>()
			where M : IModel
		{
			foreach (var element in all)
			{
				if (!typeof(M).IsAssignableFrom(element.ModelType)) continue;
				try
				{
					if (element.GetModels().First() is M result) return result;
				}
				catch (InvalidOperationException) {}
			}

			return default;
		}
		
		public M FirstOrDefault<M>(Func<M, bool> predicate)
			where M : IModel
		{
			foreach (var element in all)
			{
				if (!typeof(M).IsAssignableFrom(element.ModelType)) continue;
				try
				{
					foreach (var model in element.GetModels())
					{
						if (model is M modelTyped && predicate(modelTyped)) return modelTyped;
					}
				}
				catch (InvalidOperationException) {}
			}

			return default;
		}
		
		public M FirstOrDefault<M>(string id)
			where M : IModel
		{
			return FirstOrDefault<M>(m => m.Id.Value == id);
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