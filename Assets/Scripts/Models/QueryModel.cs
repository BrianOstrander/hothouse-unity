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
		
		public IEnumerable<T> All<T>()
			where T : IModel
		{
			return all
				.Where(p => typeof(T).IsAssignableFrom(p.ModelType))
				.SelectMany(p => p.GetModels())
				.Cast<T>();
		}
	}
}