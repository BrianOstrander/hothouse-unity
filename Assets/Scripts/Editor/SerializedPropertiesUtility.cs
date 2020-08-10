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
			public bool FileMissingNewtonsoftUsing;
			
			public string Message;
		}
		
		public static void Validate()
		{
			var files = new List<FileInfo>();
			AddFilesInDirectory(
				new DirectoryInfo(Application.dataPath),
				files,
				f =>
				{
					if (f.Extension != ".cs") return false;
					if (!f.FullName.Contains("/Models/")) return false;
					if (f.FullName.Contains("/Models/Blocks/BaseDefinition")) return false;
					if (f.FullName.Contains("ModelMediator.cs")) return false;

					return true;
				}
			);

			var results = new List<Entry>();

			foreach (var file in files) AnalyzeFile(file, results);

			var filesWithResults = results.Select(r => r.File).Distinct().ToArray();

			var message = $"Found {results.Count} result(s) in {filesWithResults.Length} file(s)";

			var suggestionFailuresCount = results.Count(r => !r.SuggestedCorrectionFound);

			if (0 < suggestionFailuresCount)
			{
				message = (message + $" found {suggestionFailuresCount} issues!").WrapColor(Color.red);

				Debug.LogError(message);
				
				// Debug Idk handle this here...
				
				return;
			}

			var description = message;
			
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

			var dialogResult = EditorUtility.DisplayDialogComplex(
				"Invalid Serialized Properties",
				description,
				"Overwrite", "Cancel", "Copy Deltas to Clipboard"
			);

			switch (dialogResult)
			{
				case 0:
					if (EditorUtility.DisplayDialog("Confirm", $"This is a destructive operation, are you sure you still want to overwrite all {filesWithResults.Length} file(s)?", "Yes", "No"))
					{
						OverwriteFiles(results);			
					}
					break;
				case 2:
					EditorGUIUtility.systemCopyBuffer = message;
					Debug.Log("Copied validation results to clipboard");
					break;
			}
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
			var hasNewtonsoftUsing = false;

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

				if (!hasNewtonsoftUsing && lineNoWhiteSpace.Contains("usingNewtonsoft.Json;")) hasNewtonsoftUsing = true;
				
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

				if (!line.Contains("JsonProperty"))
				{
					lineResult = lineResult.Replace(visibility, "[JsonProperty] " + visibility);
					if (!hasNewtonsoftUsing) result.FileMissingNewtonsoftUsing = true;
				}
				
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

		static void OverwriteFiles(List<Entry> elements)
		{
			foreach (var file in elements.Select(e => e.File).Distinct())
			{
				var replacementsForFile = elements
					.Where(e => e.File == file)
					.OrderBy(e => e.LineIndex)
					.ToList();

				var missingNewtonsoftUsing = replacementsForFile.Any(r => r.FileMissingNewtonsoftUsing);
				
				var result = string.Empty;

				void appendLine(string lineToAppend, int lineIndexToAppend)
				{
					if (lineIndexToAppend == 0) result = lineToAppend;
					else result += "\n" + lineToAppend;
				}
				
				var lineIndex = -1;

				foreach (var line in File.ReadLines(file))
				{
					lineIndex++;

					if (replacementsForFile.None() || replacementsForFile.First().LineIndex != lineIndex)
					{
						appendLine(line, lineIndex);
						continue;
					}

					var lineReplacement = replacementsForFile.First();
					replacementsForFile.RemoveAt(0);

					appendLine(lineReplacement.SuggestedCorrection, lineIndex);
				}

				if (missingNewtonsoftUsing) result = "using Newtonsoft.Json;\n" + result;
				
				File.WriteAllText(file, result);
			}
			
			Debug.Log("Done replacing invalid serialized properties");
		}
	}
}