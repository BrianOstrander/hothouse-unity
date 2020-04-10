using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;

namespace LunraGamesEditor
{
	public class TextureFarmer 
	{
		const int PixelBudget = 8000;

		class Entry
		{
			public Texture2D Target;
			public Color[] Replacements;
			public int YProgress;
			public Action Completed;
			public Action Updated;
		}

		static List<Entry> entries = new List<Entry>();

		[InitializeOnLoadMethod]
		static void Initialize()
		{
			EditorApplication.update += Farm;
		}

		static void Farm()
		{
			if (entries == null || entries.Count == 0) return;

			var remainingBudget = PixelBudget;
			var deletions = new List<Entry>();

			foreach (var entry in entries)
			{
				remainingBudget -= entry.Target.width;

				var pixels = new Color[entry.Target.width];
				var start = entry.YProgress * entry.Target.width;
				var end = start + entry.Target.width;

				for (var i = start; i < end; i++) pixels[i - start] = entry.Replacements[i];

				entry.Target.SetPixels(0, entry.YProgress, entry.Target.width, 1, pixels);
				entry.Target.Apply();

				entry.YProgress++;

				if (entry.YProgress == entry.Target.height) deletions.Add(entry);

				entry.Updated?.Invoke();

				if (remainingBudget <= 0) break;
			}

			foreach (var deletion in deletions) 
			{
				deletion.Completed?.Invoke();
				entries.Remove(deletion);
			}
		}

		public static void Queue(Texture2D target, Color[] replacements, Action completed = null, Action updated = null)
		{
			var entry = entries.FirstOrDefault(e => target == e.Target);

			if (entry == null)
			{
				entry = new Entry {
					Target = target,
					Replacements = replacements,
					Completed = completed,
					Updated = updated
				};
				entries.Add(entry);
			}
			else
			{
				entry.Replacements = replacements;
				entry.YProgress = 0;
				entry.Completed = completed;
				entry.Updated = updated;
			}
		}
	}
}