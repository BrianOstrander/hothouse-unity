namespace Lunra.Satchel
{
	public abstract class BoolOperation : PropertyValidationOperation<bool> {}
	
	public class DefinedBoolOperation : BoolOperation
	{
		public override PropertyValidation.Types OperationType => PropertyValidation.Types.Defined;
		protected override bool IsValid(RequestPayload request) => request.IsDefined;
		protected override bool IsUnDefinedValidationsPermitted(RequestPayload request) => true;
	}
	
	public class EqualToBoolOperation : BoolOperation
	{
		public override PropertyValidation.Types OperationType => PropertyValidation.Types.EqualTo;
		protected override bool IsValid(RequestPayload request)
		{
			request.Validation.TryGetOperands(out bool operand);

			return request.Value == operand;
		}
	}
}