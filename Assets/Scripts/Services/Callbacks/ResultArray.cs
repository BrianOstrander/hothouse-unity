using System.Linq;

using UnityEngine;

namespace LunraGames.SubLight
{
	public struct ResultArray<T>
	{
		public readonly RequestStatus Status;
		public readonly Result<T>[] Results;
		public readonly string Error;

		public T[] Payloads => Results.Select(r => r.Payload).ToArray();

		public static ResultArray<T> Success(Result<T>[] results)
		{
			return new ResultArray<T>(
				RequestStatus.Success,
				results
			);
		}

		public static ResultArray<T> Failure(string error, Result<T>[] results = null)
		{
			return new ResultArray<T>(
				RequestStatus.Failure,
				results,
				error
			);
		}

		ResultArray(
			RequestStatus status,
			Result<T>[] results,
			string error = null
		)
		{
			Status = status;
			Results = results;
			Error = error;
		}

		public ResultArray<T> Log(string message = null)
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
		
		public ResultArray<T> LogIfNotSuccess(string message = null)
		{
			switch (Status)
			{
				case RequestStatus.Success: break;
				default: Log(message); break;
			}
			return this;
		}
		
		public override string ToString()
		{
			var result = "ResultArray<" + typeof(T).Name + ">.Status : " + Status;
			switch (Status)
			{
				case RequestStatus.Success: break;
				default:
					result += " - " + Error;
					break;
			}
			result += "\n--- Payloads ---";

			foreach (var entry in Results) result += "\n" + entry.ToString() + "\n~~~~~~~~~~~~~~~~~";

			return result;
		}
	}
}