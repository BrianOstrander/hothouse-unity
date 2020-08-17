using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public interface ITagModel : IRoomTransformModel
	{
		TagComponent Tags { get; }
	}

	public class TagComponent : ComponentModel<ITagModel>
	{
		public enum DuplicateBehaviours
		{
			Unknown = 0,
			Append = 10,
			Ignore = 30
		}
	
		public class Entry
		{
			[JsonProperty] public string Tag { get; private set; }
			[JsonProperty] public DayTime Expiration { get; private set; }
			[JsonProperty] public bool FromPrefab { get; private set; }

			public Entry(
				string tag,
				DayTime expiration,
				bool fromPrefab
			)
			{
				Tag = tag;
				Expiration = expiration;
				FromPrefab = fromPrefab;
			}

			public override string ToString()
			{
				var result = Tag;

				if (Expiration < DayTime.MaxValue) result += " - " + Expiration;
				else result += " - No Expiration";

				return result;
			}
		}
	
		#region Serialized
		[JsonProperty] Entry[] all = new Entry[0];
		readonly ListenerProperty<Entry[]> allListener;
		[JsonIgnore] public ReadonlyProperty<Entry[]> All { get; }
		
		[JsonProperty] DayTime nextExpiration = DayTime.MaxValue;
		readonly ListenerProperty<DayTime> nextExpirationListener;
		[JsonIgnore] public ReadonlyProperty<DayTime> NextExpiration { get; }
		#endregion
		
		#region NonSerialized
		#endregion
		
		public TagComponent()
		{
			All = new ReadonlyProperty<Entry[]>(
				value => all = value,
				() => all,
				out allListener
			);
			
			NextExpiration = new ReadonlyProperty<DayTime>(
				value => nextExpiration = value,
				() => nextExpiration,
				out nextExpirationListener
			);
		}

		public override void Bind() => Game.SimulationUpdate += OnGameSimulationUpdate;

		public override void UnBind() => Game.SimulationUpdate -= OnGameSimulationUpdate;
		
		public bool Contains(string tag) => All.Value.Any(t => t.Tag == tag);

		public void AddTag(
			string tag,
			DuplicateBehaviours duplicateBehaviour = DuplicateBehaviours.Append
		)
		{
			AddTag(tag, DayTime.MaxValue, duplicateBehaviour);
		}

		public void AddTags(
			string[] tags,
			DayTime expiration,
			DuplicateBehaviours duplicateBehaviour = DuplicateBehaviours.Append,
			bool fromPrefab = false
		)
		{
			foreach (var tag in tags)
			{
				AddTag(
					tag,
					expiration,
					duplicateBehaviour,
					fromPrefab
				);
			}
		}

		public void AddTag(
			string tag,
			DayTime expiration,
			DuplicateBehaviours duplicateBehaviour = DuplicateBehaviours.Append,
			bool fromPrefab = false
		)
		{
			if (All.Value.None(t => t.Tag == tag) || duplicateBehaviour == DuplicateBehaviours.Append)
			{
				allListener.Value = All.Value
					.Append(new Entry(tag, expiration, fromPrefab))
					.OrderBy(t => t.Expiration.TotalTime)
					.ToArray();
				
				nextExpirationListener.Value = All.Value.First().Expiration;
				return;
			}
			
			switch (duplicateBehaviour)
			{
				case DuplicateBehaviours.Ignore:
					break;
				default:
					Debug.LogError("Unrecognized behaviour: "+duplicateBehaviour);
					break;
			}
		}

		#region GameModel Events
		void OnGameSimulationUpdate()
		{
			if (Game.SimulationTime.Value < NextExpiration.Value) return;

			allListener.Value = All.Value
				.Where(t => Game.SimulationTime.Value < t.Expiration)
				.ToArray();
		}
		#endregion
		
		public void Reset()
		{
			allListener.Value = allListener.Value
				.Where(t => !t.FromPrefab)
				.ToArray();
			
			nextExpirationListener.Value = DayTime.MaxValue;
		}

		public override string ToString()
		{
			var result = "Tags: ";

			if (All.Value.None()) return result + "None";

			result += All.Value.Length;
			
			foreach (var tag in All.Value)
			{
				result += $"\n\t{tag}";
			}

			return result;
		}
	}
}