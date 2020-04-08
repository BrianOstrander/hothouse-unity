using System;

using UnityEngine;
using UnityEngine.Audio;

namespace LunraGames.SubLight
{
	[Serializable]
	public struct AudioConfiguration
	{
		public AudioMixer MasterMixer;
		public AudioMixerGroupBlock[] AudioGroups;
		public AudioClip[] Music;
		public AudioListener DefaultAudioListener;
	}
}