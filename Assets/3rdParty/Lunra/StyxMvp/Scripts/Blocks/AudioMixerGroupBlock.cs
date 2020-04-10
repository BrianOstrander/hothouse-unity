using System;

using UnityEngine.Audio;

namespace Lunra.StyxMvp
{
	[Serializable]
	public struct AudioMixerGroupBlock
	{
		public AudioService.Groups Group;
		public AudioMixerGroup Target;
	}
}