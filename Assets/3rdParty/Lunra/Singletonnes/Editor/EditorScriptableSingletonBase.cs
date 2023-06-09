﻿using UnityEngine;
using System;

namespace Lunra.Editor.Singletonnes
{
	public abstract class EditorScriptableSingletonBase : ScriptableObject
	{
		public readonly Type CurrentType;

		protected EditorScriptableSingletonBase(Type type)
		{
			CurrentType = type;
		}
	}
}