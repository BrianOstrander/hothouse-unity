using Lunra.StyxMvp;
using UnityEngine;

namespace Lunra.WildVacuum.Views
{
	public class TestView : View
	{
		protected override void OnShown()
		{
			base.OnShown();
			
			Debug.Log("Shown this view!");
		}
	}

}