using Lunra.StyxMvp;
using UnityEngine;

namespace Lunra.Hothouse.Views
{
	public interface IEnterableView : IView
	{
		Vector3[] Entrances { get; }
	}
}