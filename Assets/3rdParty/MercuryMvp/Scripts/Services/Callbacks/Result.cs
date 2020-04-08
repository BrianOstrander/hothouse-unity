using System.Net;
using UnityEngine;

namespace LunraGames.SubLight
{
	public struct Result
	{
		public readonly RequestStatus Status;
		public readonly string Error;

		public static Result Success() => new Result(RequestStatus.Success);

		public static Result Failure(string error) => new Result(RequestStatus.Failure, error);

		public Result(
			RequestStatus status,
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
				case RequestStatus.Success: break;
				default: Log(message); break;
			}
			return this;
		}
		
		public Result Log(string message = null)
		{
			message = string.IsNullOrEmpty(message) ? string.Empty : (message + "\n");
			switch (Status)
			{
				case RequestStatus.Failure: Debug.LogError(message + this); break;
				case RequestStatus.Cancel: Debug.LogWarning(message + this); break;
				case RequestStatus.Success: Debug.Log(message + this); break;
				default: Debug.LogError(message + "Unrecognized RequestStatus: " + Status + ", result:\n" + this); break;
			}
			return this;
		}

		public override string ToString()
		{
			var result = "Result.Status : " + Status;
			switch (Status)
			{
				case RequestStatus.Success: break;
				default:
					result += " - " + Error;
					break;
			}
			return result;
		}
	}
	
	public struct Result<T>
	{
		public readonly RequestStatus Status;
		public readonly T Payload;
		public readonly string Error;

		public static Result<T> Success(T payload)
		{
			return new Result<T>(
				RequestStatus.Success,
				payload
			);
		}

		public static Result<T> Failure(string error, T payload = default)
		{
			return new Result<T>(
				RequestStatus.Failure,
				payload,
				error
			);
		}

		Result(
			RequestStatus status,
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
				case RequestStatus.Failure: Debug.LogError(message + this); break;
				case RequestStatus.Cancel: Debug.LogWarning(message + this); break;
				case RequestStatus.Success: Debug.Log(message + this); break;
				default: Debug.LogError(message + "Unrecognized RequestStatus: " + Status + ", result:\n" + this); break;
			}
			return this;
		}

		public override string ToString()
		{
			var result = "Result<" + typeof(T).Name + ">.Status : " + Status;
			switch (Status)
			{
				case RequestStatus.Success: break;
				default:
					result += " - " + Error;
					break;
			}

			try { return result + "\n- Payload -\n" + Payload.ToReadableJson(); }
			catch { return result + "\n- Payload -\n<CANNOT PARSE TO JSON>"; }
		}
		
		public static implicit operator Result(Result<T> r)
		{
			return new Result(r.Status, r.Error);
		}
	}
}