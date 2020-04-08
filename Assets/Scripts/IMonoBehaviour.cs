using UnityEngine;

namespace LunraGames.SubLight {

	public interface IMonoBehaviour {

		Transform transform { get; }
		GameObject gameObject { get; }
	}
}