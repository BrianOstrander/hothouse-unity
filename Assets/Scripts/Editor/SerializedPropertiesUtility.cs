using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lunra.Core;
using Lunra.Editor.Core;
using UnityEditor;
using UnityEngine;

namespace Lunra.Hothouse.Editor
{
	public static class SerializedPropertiesUtility
	{
		public enum Issues
		{
			Unknown = 0,
			Get = 10,
			GetPrivateSet = 20,
			PrivateSetGet = 30
		}
		
		public class Entry
		{
			public Issues Issue;
			public string File;
			public int LineIndex;
			public string Line;

			public string SuggestedCorrection;
			public bool SuggestedCorrectionFound;
			public string SuggestedCorrectionNotFoundMessage;
			
			public string Message;
		}
		
		public static void Validate()
		{
			var files = new List<FileInfo>();
			AddFilesInDirectory(
				new DirectoryInfo(Application.dataPath),
				files,
				f => f.Extension == ".cs" && f.FullName.Contains("/Models/") && !f.FullName.Contains("/Models/Blocks/BaseDefinition")
			);

			var results = new List<Entry>();

			foreach (var file in files) AnalyzeFile(file, results);

			var filesWithResults = results.Select(r => r.File).Distinct().ToArray();

			var message = $"Found {results.Count} result(s) in {filesWithResults.Length} file(s)";

			var suggestionFailuresCount = results.Count(r => !r.SuggestedCorrectionFound);

			if (0 < suggestionFailuresCount)
			{
				message = (message + $" found {suggestionFailuresCount} issues!").WrapColor(Color.red);
			}
			
			foreach (var file in filesWithResults)
			{
				message += $"\n\n==== <b>{Path.GetFileName(file)}</b>\n\t{file}";
				message += "\n\t\t\t\t\t\t\t |";
				foreach (var result in results.Where(r => r.File == file))
				{
					message += $"\n [ {result.LineIndex + 1} ] ";

					if (result.LineIndex < 9) message += "\t";
					
					message += $"\t{result.Issue} \t";
					
					switch (result.Issue)
					{
						case Issues.Get:
							message += "\t\t";
							break;
					}

					message += $" | \t {result.Line.Trim()}";

					if (result.SuggestedCorrectionFound)
					{
						message += $"\n\t\t\t\t\t\t\t | \t {result.SuggestedCorrection.Trim()}";
					}
					else
					{
						message += $"\n\t\t\t\t\t\t\t | \t No suggestion for reason: {result.SuggestedCorrectionNotFoundMessage}";
					}

					message += "\n";
				}
			}
			
			Debug.Log(message);
			
			EditorGUIUtility.systemCopyBuffer = message;
		}

		static void AddFilesInDirectory(
			DirectoryInfo directory,
			List<FileInfo> files,
			Func<FileInfo, bool> predicate
		)
		{
			files.AddRange(
				directory.GetFiles().Where(predicate)
			);
			
			foreach (var subDirectory in directory.GetDirectories()) AddFilesInDirectory(subDirectory, files, predicate);
		}

		static void AnalyzeFile(
			FileInfo file,
			List<Entry> results
		)
		{
			var lineIndex = -1;
			var isInsideInterface = false;

			foreach (var line in File.ReadLines(file.FullName))
			{
				lineIndex++;

				if (isInsideInterface)
				{
					if (line.Contains(" class "))
					{
						isInsideInterface = false;
					}
					else continue;
				}

				if (line.Contains(" interface "))
				{
					isInsideInterface = true;
					continue;
				}

				var lineNoWhiteSpace = line.Replace(" ", "");
				
				if (lineNoWhiteSpace.StartsWith("//")) continue;
				if (lineNoWhiteSpace.Contains("JsonIgnore")) continue;

				var visibility = string.Empty;
				
				if (line.IndexOf("public ") == -1)
				{
					if (line.IndexOf("protected ") == -1) continue;
					
					visibility = "protected ";
				}
				else visibility = "public ";

				var issue = Issues.Unknown;

				if (lineNoWhiteSpace.Contains("{get;}")) issue = Issues.Get;
				else if (lineNoWhiteSpace.Contains("{privateset;get;")) issue = Issues.PrivateSetGet;
				else if (lineNoWhiteSpace.Contains("{get;privateset;")) issue = Issues.GetPrivateSet;
				else continue;

				switch (issue)
				{
					case Issues.GetPrivateSet:
					case Issues.PrivateSetGet:
						if (lineNoWhiteSpace.Contains("[JsonProperty]")) continue;
						break;
				}
				
				var result = new Entry();

				result.Issue = issue;
				result.File = file.FullName;
				result.LineIndex = lineIndex;
				result.Line = line;
				result.SuggestedCorrectionFound = true;

				var lineResult = line;
				
				if (!line.Contains("JsonProperty")) lineResult = lineResult.Replace(visibility, "[JsonProperty] " + visibility);
				
				switch (issue)
				{
					case Issues.Get:
						if (lineResult.Contains("{ get; }"))
						{
							lineResult = lineResult.Replace("{ get; }", "{ get; private set; }");
						}
						else
						{
							result.SuggestedCorrectionFound = false;
							result.SuggestedCorrectionNotFoundMessage = "Could not find { get; }";
						}
						break;
				}

				result.SuggestedCorrection = lineResult;
				
				results.Add(result);
			}
		}
	}
}