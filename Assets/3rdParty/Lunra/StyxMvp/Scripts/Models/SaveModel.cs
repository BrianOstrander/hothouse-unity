using System;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;

using Newtonsoft.Json;
using System.IO;
using IoPath = System.IO.Path;

namespace Lunra.StyxMvp.Models
{
	public class SaveModel : Model
	{
		public enum SiblingBehaviours
		{
			Unknown = 0,
			None = 10,
			Specified = 20,
			All = 30
		}

		#region Non Serialized
		List<string> specifiedSiblings = new List<string>();
		
		bool supportedVersion;
		/// <summary>
		/// Is this loadable, or is the version too old.
		/// </summary>
		[JsonIgnore] public ListenerProperty<bool> SupportedVersion { get; }
		
		string absolutePath;
		/// <summary>
		/// The path of this save, depends on the SaveLoadService in use.
		/// </summary>
		[JsonIgnore] public ListenerProperty<string> AbsolutePath { get; }
		#endregion
		
		#region Serialized
		[JsonProperty] bool ignore;
		/// <summary>
		/// If true, this should be ignored.
		/// </summary>
		[JsonIgnore] public ListenerProperty<bool> Ignore { get; }
		
		[JsonProperty] int version;
		/// <summary>
		/// The version of the app this was saved under.
		/// </summary>
		[JsonIgnore] public ListenerProperty<int> Version { get; }

		[JsonProperty] DateTime created;
		/// <summary>
		/// When this was created and saved.
		/// </summary>
		/// <remarks>
		/// If this is equal to DateTime.MinValue it has never been saved.
		/// </remarks>
		[JsonIgnore] public ListenerProperty<DateTime> Created { get; }
		
		[JsonProperty] DateTime modified;
		/// <summary>
		/// When this was last modified and saved.
		/// </summary>
		/// <remarks>
		/// If this is equal to DateTime.MinValue it has never been saved.
		/// </remarks>
		[JsonIgnore] public ListenerProperty<DateTime> Modified { get; }

		/// <summary>
		/// How are sibling files consumed? If None is specified, no sibling
		/// folder is even created.
		/// </summary>
		/// <value>The sibling behaviour.</value>
		[JsonProperty]
		public SiblingBehaviours SiblingBehaviour { get; protected set; }

		#endregion
		
		[JsonIgnore] public bool IsStreaming => AbsolutePath.Value.StartsWith(Application.dataPath);

		[JsonIgnore] public string Path => IsStreaming ? "Assets" + AbsolutePath.Value.Substring(Application.dataPath.Length) : AbsolutePath.Value;

		/// <summary>
		/// Is there a directory with the same name as this file next to it where it's saved?
		/// </summary>
		/// <value>True if it should have a sibling directory.</value>
		[JsonIgnore] 
		public bool HasSiblingDirectory
		{
			get
			{
				switch (SiblingBehaviour)
				{
					case SiblingBehaviours.Unknown:
					case SiblingBehaviours.None:
						return false;
					case SiblingBehaviours.Specified:
					case SiblingBehaviours.All:
						return true;
					default:
						Debug.LogError("Unrecognized sibling behaviour: " + SiblingBehaviour);
						return false;
				}
			}
		}

		[JsonIgnore]
		public string SiblingDirectory
		{
			get
			{
				if (AbsolutePath.Value == null || !HasSiblingDirectory) return null;
				
				var path = IoPath.Combine(Directory.GetParent(AbsolutePath.Value).FullName, IoPath.GetFileNameWithoutExtension(AbsolutePath.Value)+IoPath.DirectorySeparatorChar);
				
				if (IsStreaming) return "Assets" + path.Substring(Application.dataPath.Length);
				
				return path;
			}
		}

		[JsonIgnore]
		public Dictionary<string, Texture2D> Textures { get; } = new Dictionary<string, Texture2D>();

		public Texture2D GetTexture(string name)
		{
			Textures.TryGetValue(name, out var result);
			return result;
		}

		public SaveModel()
		{
			SiblingBehaviour = SiblingBehaviours.None;
			SupportedVersion = new ListenerProperty<bool>(value => supportedVersion = value, () => supportedVersion);
			AbsolutePath = new ListenerProperty<string>(value => absolutePath = value, () => absolutePath);
			Ignore = new ListenerProperty<bool>(value => ignore = value, () => ignore);
			Version = new ListenerProperty<int>(value => version = value, () => version);
			Created = new ListenerProperty<DateTime>(value => created = value, () => created);
			Modified = new ListenerProperty<DateTime>(value => modified = value, () => modified);
		}

		#region Utility
		protected void AddSiblings(params string[] siblingNames)
		{
			foreach (var name in siblingNames.Where(s => !specifiedSiblings.Contains(s))) specifiedSiblings.Add(name);
		}
		#endregion

		#region Events
		public void PrepareTexture(string name, Texture2D texture) => OnPrepareTexture(name, texture);

		protected virtual void OnPrepareTexture(string name, Texture2D texture) {}
		#endregion
	}
}