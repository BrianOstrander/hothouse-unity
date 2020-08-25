using UnityEngine;

namespace Lunra.Satchel
{
	public abstract class FloatOperation : PropertyValidationOperation<float> {}
	
	public class DefinedFloatOperation : FloatOperation
	{
		public override PropertyValidation.Types OperationType => PropertyValidation.Types.Defined;
		protected override bool IsValid(RequestPayload request) => request.IsDefined;
		protected override bool IsUnDefinedValidationsPermitted(RequestPayload request) => true;
	}
	
	public class EqualToFloatOperation : FloatOperation
	{
		public override PropertyValidation.Types OperationType => PropertyValidation.Types.EqualTo;
		protected override bool IsValid(RequestPayload request)
		{
			request.Validation.TryGetOperands(out float operand);

			return Mathf.Approximately(request.Value, operand);
		}
	}
	
	public class LessThanFloatOperation : FloatOperation
	{
		public override PropertyValidation.Types OperationType => PropertyValidation.Types.LessThan;
		protected override bool IsValid(RequestPayload request)
		{
			request.Validation.TryGetOperands(out float operand);

			return request.Value < operand;
		}
	}
	
	public class LessThanOrEqualToFloatOperation : FloatOperation
	{
		public override PropertyValidation.Types OperationType => PropertyValidation.Types.LessThan | PropertyValidation.Types.EqualTo;
		protected override bool IsValid(RequestPayload request)
		{
			request.Validation.TryGetOperands(out float operand);

			return request.Value < operand || Mathf.Approximately(request.Value, operand);
		}
	}
	
	public class GreaterThanFloatOperation : FloatOperation
	{
		public override PropertyValidation.Types OperationType => PropertyValidation.Types.GreaterThan;
		protected override bool IsValid(RequestPayload request)
		{
			request.Validation.TryGetOperands(out float operand);

			return request.Value > operand;
		}
	}
	
	public class GreaterThanOrEqualToFloatOperation : FloatOperation
	{
		public override PropertyValidation.Types OperationType => PropertyValidation.Types.GreaterThan | PropertyValidation.Types.EqualTo;
		protected override bool IsValid(RequestPayload request)
		{
			request.Validation.TryGetOperands(out float operand);

			return request.Value > operand || Mathf.Approximately(request.Value, operand);
		}
	}
}