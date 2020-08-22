namespace Lunra.Satchel
{
	public abstract class IntOperation : PropertyValidationOperation<int> {}
	
	public class EqualToIntOperation : IntOperation
	{
		public override PropertyValidation.Types OperationType => PropertyValidation.Types.EqualTo;
		protected override bool IsValid(RequestPayload request)
		{
			request.Validation.TryGetOperands(out int operand);

			return request.Value == operand;
		}
	}
	
	public class LessThanIntOperation : IntOperation
	{
		public override PropertyValidation.Types OperationType => PropertyValidation.Types.LessThan;
		protected override bool IsValid(RequestPayload request)
		{
			request.Validation.TryGetOperands(out int operand);

			return request.Value < operand;
		}
	}
	
	public class LessThanOrEqualToIntOperation : IntOperation
	{
		public override PropertyValidation.Types OperationType => PropertyValidation.Types.LessThan | PropertyValidation.Types.EqualTo;
		protected override bool IsValid(RequestPayload request)
		{
			request.Validation.TryGetOperands(out int operand);

			return request.Value <= operand;
		}
	}
	
	public class GreaterThanIntOperation : IntOperation
	{
		public override PropertyValidation.Types OperationType => PropertyValidation.Types.GreaterThan;
		protected override bool IsValid(RequestPayload request)
		{
			request.Validation.TryGetOperands(out int operand);

			return request.Value > operand;
		}
	}
	
	public class GreaterThanOrEqualToIntOperation : IntOperation
	{
		public override PropertyValidation.Types OperationType => PropertyValidation.Types.GreaterThan | PropertyValidation.Types.EqualTo;
		protected override bool IsValid(RequestPayload request)
		{
			request.Validation.TryGetOperands(out int operand);

			return request.Value >= operand;
		}
	}
}