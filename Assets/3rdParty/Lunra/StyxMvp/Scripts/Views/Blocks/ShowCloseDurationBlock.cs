using System;

namespace Lunra.StyxMvp
{
	[Serializable]
	public struct ShowCloseDurationBlock
	{
		public bool OverrideShow;
		public bool OverrideClose;

		public float ShowDuration;
		public float CloseDuration;
	}
}