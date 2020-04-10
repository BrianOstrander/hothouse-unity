﻿using UnityEngine;

namespace Lunra.Core
{
	public static class PlayerPrefsExtensions
	{
		public static bool GetBool(string key, bool defaultValue = false)
		{
			return PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) == 1;
		}

		public static void SetBool(string key, bool value)
		{
			PlayerPrefs.SetInt(key, value ? 1 : 0);
		}
	}
}