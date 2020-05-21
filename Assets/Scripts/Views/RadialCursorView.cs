using System;
using Lunra.StyxMvp;
using UnityEngine;

namespace Lunra.Hothouse.Views
{
	public class RadialCursorView : View
	{
		#region Serialized
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
		[SerializeField] GameObject widget;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null
		#endregion

		#region Bindings
		public void Interaction(
			Models.Interaction.States state,
			Models.Interaction.Vector3Delta position
		)
		{
			switch (state)
			{
				case Models.Interaction.States.Idle:
					break;
				case Models.Interaction.States.Begin:
				case Models.Interaction.States.Active:
					widget.transform.position = position.Begin;
					widget.transform.localScale = Vector3.one * (Vector3.Distance(position.Begin, position.End) * 2f);
					widget.SetActive(true);
					break;
				case Models.Interaction.States.End:
					widget.SetActive(false);
					break;
				default:
					Debug.LogError("Unrecognized Interaction.State: "+state);
					break;
			}
		}
		#endregion
		
		public override void Reset()
		{
			base.Reset();
			
			Interaction(
				Models.Interaction.States.End,
				Models.Interaction.Vector3Delta.Default()
			);
		}
	}

}