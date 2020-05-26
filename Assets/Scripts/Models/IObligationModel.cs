using Lunra.StyxMvp.Models;

namespace Lunra.Hothouse.Models
{
	public interface IObligationModel : IEnterable
	{
		ListenerProperty<Obligation[]> Obligations { get; }
	}
}