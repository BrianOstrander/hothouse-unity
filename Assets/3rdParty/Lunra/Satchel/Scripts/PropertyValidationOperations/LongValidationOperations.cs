namespace Lunra.Satchel
{
	public abstract class LongOperation : PropertyValidationOperation<long> {}
	
	public class DefinedLongOperation : LongOperation
	{
		public override PropertyValidation.Types OperationType => PropertyValidation.Types.Defined;
		protected override bool IsValid(RequestPayload request) => request.IsDefined;
		protected override bool IsUnDefinedValidationsPermitted(RequestPayload request) => true;
	}
	
	public class EqualToLongOperation : LongOperation
	{
		public override PropertyValidation.Types OperationType => PropertyValidation.Types.EqualTo;
		protected override bool IsValid(RequestPayload request)
		{
			request.Validation.TryGetOperands(out long operand);

			return request.Value == operand;
		}
	}
	
	public class LessThanLongOperation : LongOperation
	{
		public override PropertyValidation.Types OperationType => PropertyValidation.Types.LessThan;
		protected override bool IsValid(RequestPayload request)
		{
			request.Validation.TryGetOperands(out long operand);

			return request.Value < operand;
		}
	}
	
	public class LessThanOrEqualToLongOperation : LongOperation
	{
		public override PropertyValidation.Types OperationType => PropertyValidation.Types.LessThan | PropertyValidation.Types.EqualTo;
		protected override bool IsValid(RequestPayload request)
		{
			request.Validation.TryGetOperands(out long operand);

			return request.Value <= operand;
		}
	}
	
	public class GreaterThanLongOperation : LongOperation
	{
		public override PropertyValidation.Types OperationType => PropertyValidation.Types.GreaterThan;
		protected override bool IsValid(RequestPayload request)
		{
			request.Validation.TryGetOperands(out long operand);

			return request.Value > operand;
		}
	}
	
	public class GreaterThanOrEqualToLongOperation : LongOperation
	{
		public override PropertyValidation.Types OperationType => PropertyValidation.Types.GreaterThan | PropertyValidation.Types.EqualTo;
		protected override bool IsValid(RequestPayload request)
		{
			request.Validation.TryGetOperands(out long operand);

			return request.Value >= operand;
		}
	}
}