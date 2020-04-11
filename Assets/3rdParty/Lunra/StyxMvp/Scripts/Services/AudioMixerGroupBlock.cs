using System;

using UnityEngine.Audio;

namespace Lunra.StyxMvp.Services
{
	[Serializable]
	public struct AudioMixerGroupBlock
	{
		public Audio.Groups Group;
		public AudioMixerGroup Target;
	}
}