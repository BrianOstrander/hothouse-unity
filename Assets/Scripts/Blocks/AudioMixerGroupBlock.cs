using System;

using UnityEngine.Audio;

namespace LunraGames.SubLight
{
	[Serializable]
	public struct AudioMixerGroupBlock
	{
		public AudioService.Groups Group;
		public AudioMixerGroup Target;
	}
}