using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Lunra.Core;

namespace Lunra.StyxMvp.Models
{
	public struct ModelResult<M> : IResult
		where M : SaveModel
	{
		public ResultStatus Status { get; }
		public SaveModel Model { get; }
		public M TypedModel { get; }
		public string Error { get; }
		public string ReadableType => "ModelResult<" + typeof(M).Name + ">";

		public Type SaveModelType => typeof(M);
		
		public static ModelResult<M> Success(SaveModel model, M typedModel)
		{
			return new ModelResult<M>(
				ResultStatus.Success,
				model,
				typedModel
			);
		}

		public static ModelResult<M> Failure(SaveModel model, M typedModel, string error)
		{
			return new ModelResult<M>(
				ResultStatus.Failure,
				model,
				typedModel,
				error
			);
		}

		ModelResult(
			ResultStatus status,
			SaveModel model,
			M typedModel,
			string error = null
		)
		{
			Status = status;
			Model = model;
			TypedModel = typedModel;
			Error = error;
		}
		
		public override string ToString() => this.ResultToString();
	}
	
	public struct ModelArrayResult<M> : IResult
		where M : SaveModel
	{
		public ResultStatus Status { get; }
		public string Error { get; }
		public ModelResult<M>[] Models { get; }
		public string ReadableType => "ModelArrayResult<" + typeof(M).Name + ">";

		public static ModelArrayResult<M> Success(
			ModelResult<M>[] models
		)
		{
			return new ModelArrayResult<M>(
				ResultStatus.Success,
				models
			);
		}

		public static ModelArrayResult<M> Failure(
			ModelResult<M>[] models,
			string error
		)
		{
			return new ModelArrayResult<M>(
				ResultStatus.Failure,
				models,
				error
			);
		}

		ModelArrayResult(
			ResultStatus status,
			ModelResult<M>[] models,
			string error = null
		)
		{
			Status = status;
			Models = models;
			Error = error;
		}

		public M[] TypedModels => Models.Select(m => m.TypedModel).ToArray();
		
		public override string ToString() => this.ResultToString();
	}

	public struct ModelIndexResult<M> : IResult
		where M : SaveModel
	{
		public ResultStatus Status { get; }
		public SaveModel[] Models { get; }
		public string Error { get; }
		public string ReadableType => "ModelIndexResult<" + typeof(M).Name + ">";
		public int Length { get; }

		public static ModelIndexResult<M> Success(SaveModel[] models)
		{
			return new ModelIndexResult<M>(
				ResultStatus.Success,
				models
			);
		}

		public static ModelIndexResult<M> Failure(string error)
		{
			return new ModelIndexResult<M>(
				ResultStatus.Failure,
				null,
				error
			);
		}

		ModelIndexResult(
			ResultStatus status,
			SaveModel[] models,
			string error = null
		)
		{
			Status = status;
			Models = models;
			Error = error;
			Length = models?.Length ?? 0;
		}
		
		public override string ToString() => this.ResultToString();
	}

	public struct ReadWriteRequest : IResult
	{
		public ResultStatus Status { get; }
		public string Path { get; }
		public byte[] Bytes { get; }
		public string Error { get; }
		public string ReadableType => "ReadWriteRequest";

		public static ReadWriteRequest Success(string path, byte[] bytes)
		{
			return new ReadWriteRequest(
				ResultStatus.Success,
				path,
				bytes
			);
		}

		public static ReadWriteRequest Failure(string path, string error)
		{
			return new ReadWriteRequest(
				ResultStatus.Failure,
				path,
				null,
				error
			);
		}

		ReadWriteRequest(
			ResultStatus status,
			string path,
			byte[] bytes,
			string error = null
		)
		{
			Status = status;
			Path = path;
			Bytes = bytes;
			Error = error;
		}
		
		public override string ToString() => this.ResultToString();
	}
	
	public abstract class ModelMediator : IModelMediator
	{
		protected virtual bool SuppressErrorLogging => false;
		
		protected virtual int CurrentVersion => Convert.ToInt32(Application.version);

		public abstract void Initialize(Action<Result> done);

		protected abstract string GetUniquePath(Type type, string id);

		protected SaveModelMeta GetSaveModelMeta(Type type) => (SaveModelMeta)Attribute.GetCustomAttribute(type, typeof(SaveModelMeta));
		
		protected bool IsSupportedVersion(Type type, int version)
		{
			var min = GetSaveModelMeta(type).MinimumSupportedVersion;
			// If min is -1, then it means we can only load saves that equal 
			// this version.
			if (min < 0) min = CurrentVersion;
			return min <= version;
		}

		public M Create<M>(string id) where M : SaveModel, new()
		{
			var result = new M();
			result.Id.Value = id;
			result.SupportedVersion.Value = true;
			result.Version.Value = CurrentVersion;
			result.AbsolutePath.Value = GetUniquePath(typeof(M), id);
			result.Created.Value = DateTime.MinValue;
			result.Modified.Value = DateTime.MinValue;
			return result;
		}

		public void Load<M>(SaveModel model, Action<ModelResult<M>> done) where M : SaveModel
		{
			if (model == null) throw new ArgumentNullException(nameof(model));
			if (done == null) throw new ArgumentNullException(nameof(done));

			if (!model.SupportedVersion.Value) 
			{
				done(ModelResult<M>.Failure(
					model,
					null,
					"Version " + model.Version.Value + " of " + typeof(M).Name + " is not supported."
				));
				return;
			}

			try { OnLoad(model, done); }
			catch (Exception exception) 
			{
				Debug.LogException(exception);
				done(ModelResult<M>.Failure(
					model,
					null,
					exception.Message
				));
			}
		}

		public void Load<M>(string modelId, Action<ModelResult<M>> done) where M : SaveModel
		{
			if (string.IsNullOrEmpty(modelId)) throw new ArgumentException("modelId cannot be null or empty", nameof(modelId));
			if (done == null) throw new ArgumentNullException(nameof(done));

			Index<M>(indexResults => OnLoadIndexed(indexResults, modelId, done));
		}

		void OnLoadIndexed<M>(ModelIndexResult<SaveModel> results, string modelId, Action<ModelResult<M>> done) where M : SaveModel
		{
			if (results.Status != ResultStatus.Success)
			{
				Debug.LogError("Indexing models failed with status: " + results.Status + " and error: " + results.Error);
				done(ModelResult<M>.Failure(
					null,
					null,
					results.Error
				));
				return;
			}
			var result = results.Models.FirstOrDefault(m => m.Id.Value == modelId);

			if (result == null)
			{
				var error = "A model with Id \"" + modelId + "\" was not found";
				if (!SuppressErrorLogging) Debug.LogError(error);
				done(ModelResult<M>.Failure(
					null,
					null,
					error
				));
				return;
			}

			Load(result, done);
		}

		protected abstract void OnLoad<M>(SaveModel model, Action<ModelResult<M>> done) where M : SaveModel;

		public void LoadAll<M>(
			Action<ModelArrayResult<M>> done,
			Func<SaveModel, bool> predicate = null
		)
			where M : SaveModel
		{
			if (done == null) throw new ArgumentNullException(nameof(done));
			
			Index<M>(indexResults => OnLoadAllIndexed(indexResults, done, predicate));
		}

		void OnLoadAllIndexed<M>(
			ModelIndexResult<SaveModel> results,
			Action<ModelArrayResult<M>> done,
			Func<SaveModel, bool> predicate
		)
			where M : SaveModel
		{
			if (results.Status != ResultStatus.Success)
			{
				Debug.LogError("Indexing models failed with status: " + results.Status + " and error: " + results.Error);
				done(ModelArrayResult<M>.Failure(
					null,
					results.Error
				));
				return;
			}
			var remaining = results.Models.ToList();
			if (predicate != null) remaining = remaining.Where(predicate).ToList();

			OnLoadAllNext(
				null,
				remaining,
				new List<ModelResult<M>>(), 
				done
			);
		}

		void OnLoadAllNext<M>(
			ModelResult<M>? loadResult,
			List<SaveModel> remaining,
			List<ModelResult<M>> results,
			Action<ModelArrayResult<M>> done
		)
			where M : SaveModel
		{
			if (loadResult.HasValue)
			{
				results.Add(loadResult.Value);
				if (loadResult.Value.Status != ResultStatus.Success) Debug.LogError("Loading model failed with status: " + loadResult.Value.Status + " and error: " + loadResult.Value.Error);
			}

			if (remaining.Count == 0)
			{
				if (results.Any(r => r.Status != ResultStatus.Success))
				{
					var error = "Loading all models encountered at least one error";
					Debug.LogError(error);
					done(
						ModelArrayResult<M>.Failure(
							results.ToArray(),
							error
						)
					);
					return;
				}
				done(ModelArrayResult<M>.Success(results.ToArray()));
				return;
			}

			var next = remaining[0];
			remaining.RemoveAt(0);
			
			Load<M>(
				next,
				nextLoadResult => OnLoadAllNext(
					nextLoadResult,
					remaining,
					results,
					done
				)
			);
		}

		public void Save<M>(M model, Action<ModelResult<M>> done = null, bool updateModified = true) where M : SaveModel
		{
			if (model == null) throw new ArgumentNullException(nameof(model));
			done = done ?? OnUnhandledError;

			var data = GetSaveModelMeta(typeof(M));
			
			if (!data.CanSave)
			{
				done(ModelResult<M>.Failure(
					model,
					model,
					"Cannot save a " + typeof(M).Name + " on this platform."
				));
				return;
			}

			if (!model.SupportedVersion.Value)
			{
				done(ModelResult<M>.Failure(
					model,
					model,
					"Version " + model.Version.Value + " of " + typeof(M).Name + " is not supported."
				));
				return;
			}

			if (string.IsNullOrEmpty(model.AbsolutePath.Value))
			{
				done(ModelResult<M>.Failure(
					model,
					model,
					"Path is null or empty."
				));
				return;
			}

			var wasCreated = model.Created.Value;
			var wasModified = model.Modified.Value;
			var wasVersion = model.Version.Value;

			if (updateModified || model.Created.Value == DateTime.MinValue)
			{
				model.Modified.Value = DateTime.Now;
				if (model.Created.Value == DateTime.MinValue) model.Created.Value = model.Modified.Value;
			}

			model.Version.Value = CurrentVersion;

			try { OnSave(model, done); }
			catch (Exception exception)
			{
				model.Modified.Value = wasModified;
				model.Created.Value = wasCreated;
				model.Version.Value = wasVersion;

				Debug.LogException(exception);
				done(ModelResult<M>.Failure(
					model,
					model,
					exception.Message
				));
			}
		}

		protected abstract void OnSave<M>(M model, Action<ModelResult<M>> done) where M : SaveModel;

		public void Index<M>(Action<ModelIndexResult<SaveModel>> done) where M : SaveModel
		{
			if (done == null) throw new ArgumentNullException(nameof(done));

			try { OnIndex<M>(done); }
			catch (Exception exception)
			{
				Debug.LogException(exception);
				done(ModelIndexResult<SaveModel>.Failure(
					exception.Message
				));
			}
		}

		protected abstract void OnIndex<M>(Action<ModelIndexResult<SaveModel>> done) where M : SaveModel;

		public void Delete<M>(M model, Action<ModelResult<M>> done) where M : SaveModel
		{
			if (model == null) throw new ArgumentNullException(nameof(model));
			done = done ?? OnUnhandledError;

			try { OnDelete(model, done); }
			catch (Exception exception)
			{
				Debug.LogException(exception);
				done(ModelResult<M>.Failure(
					model,
					null,
					exception.Message
				));
			}
		}

		protected abstract void OnDelete<M>(M model, Action<ModelResult<M>> done) where M : SaveModel;

		void OnUnhandledError<M>(ModelResult<M> result) where M : SaveModel
		{
			Debug.LogError("Unhandled error: " + result.Error);
		}

		protected void Read(string path, Action<ReadWriteRequest> done)
		{
			if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
			if (done == null) throw new ArgumentNullException(nameof(done));

			try { OnRead(path, done); }
			catch (Exception exception)
			{
				Debug.LogException(exception);
				done(ReadWriteRequest.Failure(
					path,
					exception.Message
				));
			}
		}

		public string CreateUniqueId() => Guid.NewGuid().ToString();

		protected abstract void OnRead(string path, Action<ReadWriteRequest> done);
	}

	public interface IModelMediator
	{
		void Initialize(Action<Result> done);
		M Create<M>(string id) where M : SaveModel, new();
		void Save<M>(M model, Action<ModelResult<M>> done = null, bool updateModified = true) where M : SaveModel;
		void Load<M>(SaveModel model, Action<ModelResult<M>> done) where M : SaveModel;
		void Load<M>(string modelId, Action<ModelResult<M>> done) where M : SaveModel;
		void LoadAll<M>(Action<ModelArrayResult<M>> done, Func<SaveModel, bool> predicate = null) where M : SaveModel;
		void Index<M>(Action<ModelIndexResult<SaveModel>> done) where M : SaveModel;
		void Delete<M>(M model, Action<ModelResult<M>> done) where M : SaveModel;

		string CreateUniqueId();
	}
}