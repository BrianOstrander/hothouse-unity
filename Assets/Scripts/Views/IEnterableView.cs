using Lunra.StyxMvp;
using UnityEngine;

namespace Lunra.Hothouse.Views
{
	public interface IEnterableView : IView
	{
		Transform[] Entrances { get; }
	}
}