﻿using Lunra.Core;
using UnityEngine;

namespace Lunra.StyxMvp
{
	public class FollowTagAnimation : ViewAnimation
	{
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
		[SerializeField]
		string tag;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null

		public override void OnPrepare(IView view)
		{
			var tagObject = GameObjectExtensions.FindWithTagOrHandleMissingTag(tag, true);
			view.RootTransform.position = tagObject == null ? Vector3.zero : tagObject.transform.position;
		}
	}
}