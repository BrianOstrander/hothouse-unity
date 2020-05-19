using UnityEngine;

namespace Lunra.Core
{
	public static class ResultExtensions
	{
		public static bool IsSuccess<T>(this T result) where T : IResult
		{
			return result.Status == ResultStatus.Success;
		}
		
		public static bool IsNotSuccess<T>(this T result) where T : IResult
		{
			return result.Status != ResultStatus.Success;
		}
		
		public static T LogIfNotSuccess<T>(this T result, string message = null)
			where T : IResult
		{
			switch (result.Status)
			{
				case ResultStatus.Success: break;
				default: result.Log(message); break;
			}
			return result;
		}
		
		public static T Log<T>(this T result, string message = null)
			where T : IResult
		{
			message = string.IsNullOrEmpty(message) ? string.Empty : (message + "\n");
			switch (result.Status)
			{
				case ResultStatus.Failure: Debug.LogError(message + result.ResultToString()); break;
				case ResultStatus.Cancel: Debug.LogWarning(message + result.ResultToString()); break;
				case ResultStatus.Success: Debug.Log(message + result.ResultToString()); break;
				default: Debug.LogError(message + "Unrecognized RequestStatus: " + result.Status + ", result:\n" + result.ResultToString()); break;
			}
			return result;
		}

		public static string ResultToString<T>(this T result)
			where T : IResult
		{
			var toStringResult = result.ReadableType + ".Status : " + result.Status;
			switch (result.Status)
			{
				case ResultStatus.Success: break;
				default:
					toStringResult += " - " + result.Error;
					break;
			}
			return toStringResult;
		}
	}
}