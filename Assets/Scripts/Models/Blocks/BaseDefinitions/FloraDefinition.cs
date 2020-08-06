using System;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Presenters;
using Lunra.NumberDemon;

namespace Lunra.Hothouse.Models
{
	[FloraDefinition]
	public abstract class FloraDefinition
	{
		public string Type { get; private set; }
		public virtual string[] PrefabIds { get; private set; }
		protected GameModel Game { get; private set; }
		protected Demon Generator { get; private set; }
		
		public virtual FloatRange AgeDuration => new FloatRange(14f, 20f);
		public virtual FloatRange ReproductionDuration => new FloatRange(7f, 14f);
		public virtual FloatRange ReproductionRadius => new FloatRange(0.5f, 1f);
		public virtual int ReproductionFailureLimit => 40;
		public virtual float HealthMaximum => 100f;
		public virtual float SpreadDamage => 50f;
		public virtual bool AttacksBuildings => false;
		public virtual (Inventory.Types Type, int Minimum, int Maximum)[] ItemDrops => new (Inventory.Types Type, int Minimum, int Maximum)[0];
		public virtual IntegerRange ClusterPerRoom => new IntegerRange(0, 4);
		public virtual IntegerRange CountPerCluster => new IntegerRange(6, 12);
		public virtual float SpawnDistanceNormalizedMinimum => 0f;
		public virtual bool RequiredInSpawn => true;
		public virtual bool AllowedInSpawn => true;

		public virtual ModifierDefinition[] AgeModifiers => new[]
		{
			ModifierDefinition.NoStacking(Tags.Farm.Sown, 0.25f),
			ModifierDefinition.NoStacking(Tags.Farm.Tended, 0.25f),
			ModifierDefinition.NoStacking(Tags.Water.Applied, 0.5f),
		};
		
		public virtual ModifierDefinition[] ReproductionModifiers => new[]
		{
			ModifierDefinition.NoStacking(Tags.Farm.Sown, -0.25f),
			ModifierDefinition.NoStacking(Tags.Farm.Tended, -0.5f),
			ModifierDefinition.NoStacking(Tags.Water.Applied, 0.5f),
		};

		public void Initialize(
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

			if (PrefabIds.None()) throw new Exception("No views with prefab ids starting with " + Type + " could be found");
		}

		public virtual string GetPrefabId(Demon demon = null) => (demon ?? Generator).GetNextFrom(PrefabIds);
		
		public virtual void Reset(FloraModel model)
		{
			model.Type.Value = Type;
			model.Farm.Value = InstanceId.Null();
			model.Age.Value = Interval.WithMaximum(AgeDuration.Evaluate(DemonUtility.NextFloat));
			model.ReproductionElapsed.Value = Interval.WithMaximum(ReproductionDuration.Evaluate(DemonUtility.NextFloat));
			model.ReproductionRadius.Value = ReproductionRadius;
			model.ReproductionFailures.Value = 0;
			model.ReproductionFailureLimit.Value = ReproductionFailureLimit;
			model.SpreadDamage.Value = SpreadDamage;
			model.AttacksBuildings.Value = AttacksBuildings;
			model.Health.ResetToMaximum(HealthMaximum);
			model.Enterable.Reset();
			model.Obligations.Reset();
			model.Clearable.ItemDrops.Value = new Inventory(
				ItemDrops.ToDictionary(
					e => e.Type,
					e => DemonUtility.GetNextInteger(e.Minimum, e.Maximum + 1)
				)
			);
			model.Tags.Reset(Game);
			
			model.AgeModifier.Reset(AgeModifiers);
			model.ReproductionModifier.Reset(ReproductionModifiers);
		}

		public virtual void Instantiate(FloraModel model) => new FloraPresenter(Game, model);
	}
}