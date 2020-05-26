using Lunra.StyxMvp.Models;

namespace Lunra.Hothouse.Models
{
	public interface IEnterable : IModel, IRoomTransform
	{
		ListenerProperty<Entrance[]> Entrances { get; }
	}
}