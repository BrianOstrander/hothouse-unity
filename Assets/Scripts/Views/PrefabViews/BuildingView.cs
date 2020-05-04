using System.Linq;
using UnityEngine;

namespace Lunra.WildVacuum.Views
{
	public abstract class BuildingView : PrefabView
	{
		#region Serialized
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
		[SerializeField] Transform[] entrances = new Transform[0];		
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null
		#endregion

		#region Reverse Bindings
		public Vector3[] Entrances => entrances.Select(e => e.position).ToArray();
		#endregion

		public override void Reset()
		{
			base.Reset();
		
		}

	}
}