using System;
using System.Linq;
using Lunra.Core;
using Lunra.NumberDemon;

namespace Lunra.Hothouse.Models
{
	public abstract class BaseDefinition<M>
		where M : IPrefabModel
	{
		public string Type { get; private set; }
		protected GameModel Game { get; private set; }
		public virtual string[] PrefabIds { get; private set; }
		protected virtual string DefaultPrefabId => null;
		protected Demon Generator { get; private set; }
		
		public virtual void Initialize(
			GameModel game,
			string[] prefabIds
		)
		{
			Game = game;
			Generator = new Demon();
			
			Type = GetType().Name
				.Replace("Definition", String.Empty)
				.ToSnakeCase();

			PrefabIds = prefabIds
				.Where(s => s.StartsWith(Type))
				.ToArray();

			if (PrefabIds.None())
			{
				if (!string.IsNullOrEmpty(DefaultPrefabId) && prefabIds.Any(s => s == DefaultPrefabId))
				{
					PrefabIds = DefaultPrefabId.WrapInArray();
				}
				else throw new Exception("No views with prefab ids starting with " + Type + " could be found");
			}
		}

		public virtual string GetPrefabId(Demon generator = null) => (generator ?? Generator).GetNextFrom(PrefabIds);

		public abstract void Instantiate(M model);
	}
}