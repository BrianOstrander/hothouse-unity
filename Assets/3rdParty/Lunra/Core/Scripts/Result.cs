using System;
using Lunra.Core;
using UnityEngine;

namespace Lunra.Core
{
	public interface IResult
	{
		ResultStatus Status { get; }
		string Error { get; }
		string ReadableType { get; }
	}
	
	public struct Result : IResult
	{
		public ResultStatus Status { get; }
		public string Error { get; }
		public string ReadableType => "Result";

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
		
		public override string ToString() => this.ResultToString();
	}
	
	public struct Result<T> : IResult
	{
		public ResultStatus Status { get; }
		public T Payload { get; }
		public string Error { get; }
		public string ReadableType => "Result<" + typeof(T).Name + ">";

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
		
		public override string ToString() => this.ResultToString();
	}
}