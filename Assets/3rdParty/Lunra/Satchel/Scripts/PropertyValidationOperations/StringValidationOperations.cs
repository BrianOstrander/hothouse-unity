namespace Lunra.Satchel
{
	public class StringValidationOperations
	{
		public abstract class StringOperation : PropertyValidationOperation<string> {}
		
		public class DefinedStringOperation : StringOperation
		{
			public override PropertyValidation.Types OperationType => PropertyValidation.Types.Defined;
			protected override bool IsValid(RequestPayload request) => request.IsDefined;
			protected override bool IsUnDefinedValidationsPermitted(RequestPayload request) => true;
		}
		
		public class EqualToStringOperation : StringOperation
		{
			public override PropertyValidation.Types OperationType => PropertyValidation.Types.EqualTo;
			protected override bool IsValid(RequestPayload request)
			{
				request.Validation.TryGetOperands(out string operand);

				return request.Value == operand;
			}
		}
		
		public class ContainsStringOperation : StringOperation
		{
			public override PropertyValidation.Types OperationType => PropertyValidation.Types.Contains;
			protected override bool IsValid(RequestPayload request)
			{
				request.Validation.TryGetOperands(out string operand);

				return request.Value.Contains(operand);
			}
		}
		
		public class StartsWithStringOperation : StringOperation
		{
			public override PropertyValidation.Types OperationType => PropertyValidation.Types.StartsWith;
			protected override bool IsValid(RequestPayload request)
			{
				request.Validation.TryGetOperands(out string operand);

				return request.Value.StartsWith(operand);
			}
		}
		
		public class EndsWithStringOperation : StringOperation
		{
			public override PropertyValidation.Types OperationType => PropertyValidation.Types.EndsWith;
			protected override bool IsValid(RequestPayload request)
			{
				request.Validation.TryGetOperands(out string operand);

				return request.Value.EndsWith(operand);
			}
		}
	}
}