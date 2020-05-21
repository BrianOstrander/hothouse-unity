using System;
using Lunra.StyxMvp;

namespace Lunra.Hothouse.Views
{
	public class ToolbarView : View
	{
		#region Serialized
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null
		#endregion

		#region Bindings
		public event Action GatherClick;
		public event Action BuildFireClick;
		public event Action BuildBedClick;
		#endregion

		public override void Reset()
		{
			base.Reset();

			GatherClick = null;
			BuildFireClick = null;
			BuildBedClick = null;
		}

		#region Events
		public void OnGatherClick() => GatherClick?.Invoke();
		public void OnBuildFireClick() => BuildFireClick?.Invoke();
		public void OnBuildBedClick() => BuildBedClick?.Invoke();
		#endregion
	}

}