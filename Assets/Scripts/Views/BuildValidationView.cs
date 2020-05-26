using Lunra.StyxMvp;
using UnityEngine;

namespace Lunra.Hothouse.Views
{
	public class BuildValidationView : View
	{
		#region Serialized
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
		[SerializeField] GameObject invalidWidget;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null
		#endregion
  
		#region Bindings
		public bool IsInvalid { set => invalidWidget.SetActive(value); }
		#endregion
  
		public override void Reset()
		{
			base.Reset();

			IsInvalid = false;
		}
  
		#region Events
		#endregion
	}
   
}