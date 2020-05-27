using Lunra.StyxMvp.Models;

namespace Lunra.Hothouse.Models
{
	public interface IObligationModel : IEnterableModel
	{
		ListenerProperty<Obligation[]> Obligations { get; }
	}
}