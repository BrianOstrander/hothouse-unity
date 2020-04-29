using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.WildVacuum.Models
{
	public class SelectionModel : Model
	{
		public enum States
		{
			Unknown = 0,
			Deselected = 10,
			Highlighting = 20,
			Selected = 30
		}
		
		public struct Selection
		{
			public static Selection Deselected() => new Selection(Vector3.zero, Vector3.zero, States.Deselected);
			public static Selection Highlighting(Vector3 begin, Vector3 end) => new Selection(begin, end, States.Highlighting);
			public static Selection Selected(Vector3 begin, Vector3 end) => new Selection(begin, end, States.Selected);
			
			public readonly Vector3 Begin;
			public readonly Vector3 End;
			public readonly States State;

			public readonly Plane Surface;

			Selection(
				Vector3 begin,
				Vector3 end,
				States state
			)
			{
				Begin = begin;
				End = end;
				State = state;
				
				Surface = new Plane(Vector3.up, begin);
			}
			
			public Selection NewState(States state) => new Selection(Begin, End, state);

			public bool Contains(Vector3 position) => Vector3.Distance(Begin, position) < Vector3.Distance(Begin, End);
		}
		
		#region Serialized
		[JsonProperty] Selection current = Selection.Deselected();
		[JsonIgnore] public readonly ListenerProperty<Selection> Current;
		#endregion
		
		#region Non Serialized
		#endregion
		
		public SelectionModel()
		{
			Current = new ListenerProperty<Selection>(value => current = value, () => current);
		}
	}
}