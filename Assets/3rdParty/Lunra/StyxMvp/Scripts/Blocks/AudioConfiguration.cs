﻿using System;

using UnityEngine;
using UnityEngine.Audio;

namespace Lunra.StyxMvp
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