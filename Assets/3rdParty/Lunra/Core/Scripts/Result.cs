using System;
using Lunra.Core;
using UnityEngine;

namespace Lunra.Core
{
	public struct Result
	{
		public readonly ResultStatus Status;
		public readonly string Error;

		public static Result Success() => new Result(ResultStatus.Success);

		public static Result Failure(Exception exception) => new Result(ResultStatus.Failure, exception.Message);
		public static Result Failure(string error) => new Result(ResultStatus.Failure, error);

		public Result(
			ResultStatus status,
			string error = null
		)
		{
			Status = status;
			Error = error;
		}

		public Result LogIfNotSuccess(string message = null)
		{
			switch (Status)
			{
				case ResultStatus.Success: break;
				default: Log(message); break;
			}
			return this;
		}
		
		public Result Log(string message = null)
		{
			message = string.IsNullOrEmpty(message) ? string.Empty : (message + "\n");
			switch (Status)
			{
				case ResultStatus.Failure: Debug.LogError(message + this); break;
				case ResultStatus.Cancel: Debug.LogWarning(message + this); break;
				case ResultStatus.Success: Debug.Log(message + this); break;
				default: Debug.LogError(message + "Unrecognized RequestStatus: " + Status + ", result:\n" + this); break;
			}
			return this;
		}

		public override string ToString()
		{
			var result = "Result.Status : " + Status;
			switch (Status)
			{
				case ResultStatus.Success: break;
				default:
					result += " - " + Error;
					break;
			}
			return result;
		}
	}
	
	public struct Result<T>
	{
		public readonly ResultStatus Status;
		public readonly T Payload;
		public readonly string Error;

		public static Result<T> Success(T payload)
		{
			return new Result<T>(
				ResultStatus.Success,
				payload
			);
		}

		public static Result<T> Failure(string error, T payload = default)
		{
			return new Result<T>(
				ResultStatus.Failure,
				payload,
				error
			);
		}

		Result(
			ResultStatus status,
			T payload,
			string error = null
		)
		{
			Status = status;
			Payload = payload;
			Error = error;
		}
		
		public Result<T> Log(string message = null)
		{
			message = string.IsNullOrEmpty(message) ? string.Empty : (message + "\n");
			switch (Status)
			{
				case ResultStatus.Failure: Debug.LogError(message + this); break;
				case ResultStatus.Cancel: Debug.LogWarning(message + this); break;
				case ResultStatus.Success: Debug.Log(message + this); break;
				default: Debug.LogError(message + "Unrecognized RequestStatus: " + Status + ", result:\n" + this); break;
			}
			return this;
		}

		public override string ToString()
		{
			var result = "Result<" + typeof(T).Name + ">.Status : " + Status;
			switch (Status)
			{
				case ResultStatus.Success: break;
				default:
					result += " - " + Error;
					break;
			}

			try { return result + "\n- Payload -\n" + Payload.ToReadableJson(); }
			catch { return result + "\n- Payload -\n<CANNOT PARSE TO JSON>"; }
		}
	}
}