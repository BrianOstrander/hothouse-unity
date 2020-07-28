using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Presenters;
using Lunra.Hothouse.Views;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public class DecorationPoolModel : BasePrefabPoolModel<DecorationModel>
	{
		GameModel game;
		List<DecorationRule> rules = new List<DecorationRule>();
	
		public override void Initialize(GameModel game)
		{
			this.game = game;
			
			foreach (var ruleType in ReflectionUtility.GetTypesWithAttribute<DecorationRuleAttribute, DecorationRule>())
			{
				if (ReflectionUtility.TryGetInstanceOfType<DecorationRule>(ruleType, out var ruleInstance))
				{
					try
					{
						ruleInstance.Initialize(game);
						rules.Add(ruleInstance);
					}
					catch (Exception e)
					{
						Debug.LogException(e);
					}
				}
			}
			
			Initialize(
				m => new DecorationPresenter(game, m)
			);
		}

		public DecorationModel Activate(
			string prefabId,
			DecorationRule.RoomInfo roomInfo,
			Vector3 position,
			Quaternion rotation
		)
		{
			var result = base.Activate(
				prefabId,
				roomInfo.Room.RoomTransform.Id.Value,
				position,
				rotation
			);
			
			roomInfo.RegisterTags(result.PrefabTags.Value);

			return result;
		}
		
		public DecorationView[] GetValidViews(
			DecorationView[] views,
			DecorationRule.RoomInfo roomInfo
		)
		{
			return views
				.Where(v => rules.All(r => !r.Applies(v) || r.Validate(v, roomInfo)))
				.ToArray();
		}
		
		public DecorationView[] GetValidViewsRequired(
			DecorationView[] views,
			DecorationRule.RoomInfo roomInfo
		)
		{
			var requiredTags = roomInfo.DecorationTagsRequiredForRoom.Keys.ToArray();
			
			return views
				.Where(v => v.PrefabTags.Any(t => requiredTags.Contains(t)))
				.Where(v => rules.All(r => !r.Applies(v) || r.Validate(v, roomInfo)))
				.ToArray();
		}
	}
}