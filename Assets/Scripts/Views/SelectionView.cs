using Lunra.StyxMvp;
using UnityEngine;

namespace Lunra.WildVacuum.Views
{
	public class SelectionView : View
	{
		#region Serialized
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
		[SerializeField] GameObject widget;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null
		#endregion

		#region Bindings
		public void None()
		{
			widget.SetActive(false);
		}
		
		public void Highlight(
			Vector3 begin,
			Vector3 end
		)
		{
			widget.transform.position = begin;
			widget.transform.localScale = Vector3.one * (Vector3.Distance(begin, end) * 2f);
			widget.SetActive(true);
		}

		public void Select(
			Vector3 begin,
			Vector3 end
		)
		{
			widget.SetActive(false);
		}
		#endregion
		
		public override void Reset()
		{
			base.Reset();
			
			None();
		}
	}

}