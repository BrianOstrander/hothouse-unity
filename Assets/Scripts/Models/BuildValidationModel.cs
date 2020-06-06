using Lunra.StyxMvp.Models;
using Newtonsoft.Json;

namespace Lunra.Hothouse.Models
{
	public class BuildValidationModel
	{
		public enum ValidationStates
		{
			Unknown = 0,
			None = 10,
			Valid = 20,
			Invalid = 30
		}
		
		public struct Validation
		{
			public static Validation None() => new Validation(ValidationStates.None, Models.Interaction.GenericVector3.Default());
			public static Validation Valid(Interaction.GenericVector3 interaction, string message = null) => new Validation(ValidationStates.Valid, interaction, message);
			public static Validation Invalid(Interaction.GenericVector3 interaction, string message = null) => new Validation(ValidationStates.Invalid, interaction, message);
			
			public readonly ValidationStates State;
			public readonly Interaction.GenericVector3 Interaction;
			public readonly string Message;

			Validation(
				ValidationStates state,
				Interaction.GenericVector3 interaction,
				string message = null
			)
			{
				State = state;
				Interaction = interaction;
				Message = message;
			}
		}
		
		#region Non Serialized
		Validation current = Validation.None();
		[JsonIgnore] public ListenerProperty<Validation> Current { get; }
		#endregion

		public BuildValidationModel()
		{
			Current = new ListenerProperty<Validation>(value => current = value, () => current);
		}
	}
}