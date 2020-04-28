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
			None = 10,
			Highlight = 20,
			Select = 30,
			Deselect = 40
		}
		
		public struct Selection
		{
			public static Selection None() => new Selection(Vector3.zero, Vector3.zero, States.None);
			public static Selection Highlight(Vector3 begin, Vector3 end) => new Selection(begin, end, States.Highlight);
			public static Selection Select(Vector3 begin, Vector3 end) => new Selection(begin, end, States.Select);
			public static Selection Deselect(Vector3 begin, Vector3 end) => new Selection(begin, end, States.Deselect);
			
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
		}
		
		#region Serialized
		[JsonProperty] Selection current = Selection.None();
		public readonly ListenerProperty<Selection> Current;
		#endregion
		
		#region Non Serialized
		#endregion
		
		public SelectionModel()
		{
			Current = new ListenerProperty<Selection>(value => current = value, () => current);
		}
	}
}