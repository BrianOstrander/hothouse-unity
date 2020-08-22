namespace Lunra.Satchel
{
	public abstract class BoolOperation : PropertyValidationOperation<bool> {}
	
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