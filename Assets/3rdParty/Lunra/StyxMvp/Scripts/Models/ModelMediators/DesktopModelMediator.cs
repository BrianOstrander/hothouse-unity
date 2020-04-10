using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using Lunra.StyxMvp.Models;
using Lunra.Core;
using UnityEngine;
using Newtonsoft.Json;
using Assembly = System.Reflection.Assembly;

namespace Lunra.StyxMvp
{
	public class DesktopModelMediator : ModelMediator
	{
		const string Extension = ".json";
		static string PersistentPath => Path.Combine(Application.persistentDataPath, "styx_persistent");
		static string StreamingPath => Path.Combine(Application.streamingAssetsPath, "styx_streaming");

		bool readableSaves;

		public DesktopModelMediator(bool readableSaves = false)
		{
			this.readableSaves = readableSaves;
		}

		public override void Initialize(Action<RequestStatus> done)
		{
			try
			{
				foreach(Type type in Assembly.GetExecutingAssembly().GetTypes())
				{
					var current = type.GetCustomAttribute(typeof(SaveModelMeta), true);
					if (current == null) continue;

					var path = (current as SaveModelMeta)?.Path;

					if (string.IsNullOrEmpty(path))
					{
						Debug.LogError("A null or empty path was provided for "+type.Name);
						continue;
					}
					
					Directory.CreateDirectory(Path.Combine(PersistentPath, path));
				}
				
				done(RequestStatus.Success);
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
				done(RequestStatus.Failure);
			}
		}

		string GetPath(Type saveType)
		{
			var saveData = saveType.GetCustomAttribute(typeof(SaveModelMeta), true) as SaveModelMeta;
			if (saveData == null) throw new ArgumentException(nameof(saveType) + " has meta data attribute");
			return Path.Combine(PersistentPath, saveData.Path);
		}

		protected override string GetUniquePath(Type saveType, string id)
		{
			if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
			if (!id.Replace("_", "").Any(Char.IsLetterOrDigit)) throw new ArgumentException("Id \"" + id + "\" contains non alphanumeric characters", nameof(id));
			
			var path = Path.Combine(GetPath(saveType), id + Extension);

			if (File.Exists(path)) throw new Exception("Id is not unique, a file exists at path: " + path);
			
			return path;
		}

		protected override void OnLoad<M>(SaveModel model, Action<ModelResult<M>> done)
		{
			var result = Serialization.DeserializeJson<M>(File.ReadAllText(model.AbsolutePath.Value));
			if (result == null)
			{
				done(ModelResult<M>.Failure(model, null, "Null result"));
				return;
			}

			result.SupportedVersion.Value = model.SupportedVersion.Value;
			result.AbsolutePath.Value = model.AbsolutePath.Value;

			if (result.HasSiblingDirectory) LoadSiblingFiles(model, result, done);
			else done(ModelResult<M>.Success(model, result));
		}

		protected override void OnSave<M>(M model, Action<ModelResult<M>> done)
		{
			File.WriteAllText(model.AbsolutePath.Value, model.Serialize(formatting: readableSaves ? Formatting.Indented : Formatting.None));
			if (model.HasSiblingDirectory) Directory.CreateDirectory(model.SiblingDirectory);
			done(ModelResult<M>.Success(model, model));
		}

		protected override void OnIndex<M>(Action<ModelIndexResult<SaveModel>> done)
		{
			var path = GetPath(typeof(M));
			var results = new List<SaveModel>();
			foreach (var file in Directory.GetFiles(path))
			{
				try
				{
					if (Path.GetExtension(file) != Extension) continue;
					var result = Serialization.DeserializeJson<SaveModel>(File.ReadAllText(file));
					if (result == null) continue;

					result.SupportedVersion.Value = IsSupportedVersion(typeof(M), result.Version.Value);
					result.AbsolutePath.Value = file;
					results.Add(result);
				}
				catch (Exception exception)
				{
					Debug.LogException(exception);
				}
			}
			var array = results.ToArray();
			done(ModelIndexResult<SaveModel>.Success(array));
		}

		protected override void OnDelete<M>(M model, Action<ModelResult<M>> done)
		{
			File.Delete(model.AbsolutePath.Value);
			done(ModelResult<M>.Success(model, model));
		}

		protected override void OnRead(string path, Action<ReadWriteRequest> done)
		{
			var data = File.ReadAllBytes(path);

			// Apparently File.ReadAllBytes never returns null so we don't have to check...

			done(ReadWriteRequest.Success(path, data));
		}

		#region Sibling Loading
		void LoadSiblingFiles<M>(SaveModel model, M result, Action<ModelResult<M>> done)
			where M : SaveModel
		{
			OnLoadSiblingFiles(Directory.GetFiles(result.SiblingDirectory).ToList(), model, result, done);
		}

		void OnLoadSiblingFiles<M>(List<string> remainingFiles, SaveModel model, M result, Action<ModelResult<M>> done)
			where M : SaveModel
		{
			if (remainingFiles.None())
			{
				done(ModelResult<M>.Success(model, result));
				return;
			}

			var nextFile = remainingFiles.First();
			remainingFiles.RemoveAt(0);

			void onDone() => OnLoadSiblingFiles(remainingFiles, model, result, done);

			switch (Path.GetExtension(nextFile))
			{
				case ".png":
					Read(nextFile, textureResult => OnReadSiblingTexture(textureResult, result, onDone));
					break;
				default:
					onDone();
					break;
			}
		}

		void OnReadSiblingTexture<M>(ReadWriteRequest textureResult, M result, Action done)
			where M : SaveModel
		{
			var textureName = Path.GetFileNameWithoutExtension(textureResult.Path);

			void onError(string error)
			{
				Debug.LogError(error);
				done();
			}

			if (string.IsNullOrEmpty(textureName))
			{
				onError("Name or path of texture returned null or empty.");
				return;
			}

			if (textureResult.Status != RequestStatus.Success)
			{
				onError("Unable to read bytes at \"" + textureResult.Path + "\", returning null.");
				return;
			}

			try
			{
				var target = new Texture2D(1, 1);
				result.PrepareTexture(textureName, target);
				target.LoadImage(textureResult.Bytes);
				result.Textures.Add(textureName, target);
				done();
			}
			catch (Exception e)
			{
				onError("Encountered the following exception while loading bytes into texture:\n" + e.Message);
			}
		}
		#endregion
	}
}