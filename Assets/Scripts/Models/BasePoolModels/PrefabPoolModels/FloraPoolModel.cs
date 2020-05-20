/*
using System;
using Lunra.Hothouse.Presenters;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public class FloraPoolModel : BasePrefabPoolModel<FloraModel>
	{
		public new void Initialize(GameModel game)
		{
			base.Initialize(
				model => new FloraPresenter(game, model)	
			);
		}

		public FloraModel Activate(
			FloraSpecies species,
			string roomId,
			Vector3 position,
			Quaternion rotation
		)
		{
			
		}

		public new void InActivate(params FloraModel[] models)
		{
			base.InActivate(models);
		}
	}
}
*/