using System;
using System.Collections.Generic;
using System.IO;
using Lunra.NumberDemon;
using UnityEngine;

namespace Lunra.Hothouse.Services
{
	public class NameGenerator
	{
		static class Paths
		{
			public const string Root = "PopulationData/";

			public static readonly string[] FirstNames =
			{
				"feminine_names",
				"masculine_names"
			};
			
			public static readonly string[] LastNames =
			{
				"surnames"
			};
		}

		Demon defaultGenerator = new Demon();
		List<string> firstNames = new List<string>();
		List<string> lastNames = new List<string>();
		
		public void Initialize(Action done)
		{
			foreach (var firstNamePath in Paths.FirstNames)
			{
				LoadFile(
					Path.Combine(Paths.Root, firstNamePath),
					entry => firstNames.Add(entry)
				);
			}
			
			foreach (var lastNamePath in Paths.LastNames)
			{
				LoadFile(
					Path.Combine(Paths.Root, lastNamePath),
					entry => lastNames.Add(entry)
				);
			}

			done();
		}

		void LoadFile(
			string path,
			Action<string> record
		)
		{
			var contents = Resources.Load<TextAsset>(path);
			foreach (var line in contents.text.Split('\n')) record(line);
			Resources.UnloadAsset(contents);
		}
		
		public string GetName(Demon generator = null)
		{
			generator = generator ?? defaultGenerator;
			
			return generator.GetNextFrom(firstNames);
		}
	}
}