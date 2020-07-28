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

	public class TagComponent : Model
	{
		public enum DuplicateBehaviours
		{
			Unknown = 0,
			Append = 10,
			Ignore = 30
		}
	
		public class Entry
		{
			public string Tag { get; }
			public DayTime Expiration { get; }

			public Entry(
				string tag,
				DayTime expiration
			)
			{
				Tag = tag;
				Expiration = expiration;
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

		public bool Containts(string tag) => All.Value.Any(t => t.Tag == tag);

		public void AddTag(
			string tag,
			DuplicateBehaviours duplicateBehaviour = DuplicateBehaviours.Append
		)
		{
			AddTag(tag, DayTime.MaxValue, duplicateBehaviour);
		}

		public void AddTag(
			string tag,
			DayTime expiration,
			DuplicateBehaviours duplicateBehaviour = DuplicateBehaviours.Append
		)
		{
			if (All.Value.None(t => t.Tag == tag) || duplicateBehaviour == DuplicateBehaviours.Append)
			{
				allListener.Value = All.Value
					.Append(new Entry(tag, expiration))
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

		public void Update(DayTime simulatedTime)
		{
			if (simulatedTime < NextExpiration.Value) return;

			allListener.Value = All.Value
				.Where(t => simulatedTime < t.Expiration)
				.ToArray();
		}
		
		public void Reset()
		{
			allListener.Value = new Entry[0];
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

	public static class TagGameModelExtensions
	{
		public static IEnumerable<ITagModel> GetTags(
			this GameModel game	
		)
		{
			return game.Flora.AllActive;
		}
	}
}