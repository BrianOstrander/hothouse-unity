using System;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;

using Newtonsoft.Json;
using System.IO;
using IoPath = System.IO.Path;

namespace LunraGames.SubLight.Models
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

		bool supportedVersion;
		string path;
		List<string> specifiedSiblings = new List<string>();
		// TODO: Can I now have this just be a property with a default value set (using new C# 7 features?)
		Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();

		[JsonProperty] bool ignore;
		[JsonProperty] int version;
		[JsonProperty] Dictionary<string, string> metaKeyValues = new Dictionary<string, string>();
		[JsonProperty] DateTime created;
		[JsonProperty] DateTime modified;

		/// <summary>
		/// Gets the type of the save.
		/// </summary>
		/// <value>The type of the save.</value>
		[JsonProperty]
		public SaveTypes SaveType { get; protected set; }

		/// <summary>
		/// How are sibling files consumed? If None is specified, no sibling
		/// folder is even created.
		/// </summary>
		/// <value>The sibling behaviour.</value>
		[JsonProperty]
		public SiblingBehaviours SiblingBehaviour { get; protected set; }
		/// <summary>
		/// Is this loadable, or is the version too old.
		/// </summary>
		[JsonIgnore]
		public readonly ListenerProperty<bool> SupportedVersion;
		/// <summary>
		/// The path of this save, depends on the SaveLoadService in use.
		/// </summary>
		[JsonIgnore]
		public readonly ListenerProperty<string> Path;

		/// <summary>
		/// If true, this should be ignored.
		/// </summary>
		[JsonIgnore]
		public readonly ListenerProperty<bool> Ignore;
		/// <summary>
		/// The version of the app this was saved under.
		/// </summary>
		[JsonIgnore]
		public readonly ListenerProperty<int> Version;
		/// <summary>
		/// More identifying data.
		/// </summary>
		/// <remarks>
		/// Editing the dictionary returned won't modify the original one.
		/// </remarks>
		[JsonIgnore]
		public readonly ListenerProperty<Dictionary<string, string>> MetaKeyValues;
		/// <summary>
		/// When this was created and saved.
		/// </summary>
		/// <remarks>
		/// If this is equal to DateTime.MinValue it has never been saved.
		/// </remarks>
		[JsonIgnore]
		public readonly ListenerProperty<DateTime> Created;
		/// <summary>
		/// When this was last modified and saved.
		/// </summary>
		/// <remarks>
		/// If this is equal to DateTime.MinValue it has never been saved.
		/// </remarks>
		[JsonIgnore]
		public readonly ListenerProperty<DateTime> Modified;

		[JsonIgnore]
		public bool IsInternal { get { return Path.Value.StartsWith(Application.dataPath); } }
		[JsonIgnore]
		public string InternalPath
		{
			get
			{
				if (!IsInternal) return null;
				return "Assets" + Path.Value.Substring(Application.dataPath.Length);
			}
		}

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
				if (Path.Value == null || !HasSiblingDirectory) return null;
				return IoPath.Combine(Directory.GetParent(Path.Value).FullName, IoPath.GetFileNameWithoutExtension(Path.Value)+IoPath.DirectorySeparatorChar);
			}
		}
		[JsonIgnore]
		public string InternalSiblingDirectory
		{
			get
			{
				if (!HasSiblingDirectory || !IsInternal) return null;
				return "Assets" + SiblingDirectory.Substring(Application.dataPath.Length);
			}
		}

		[JsonIgnore]
		public Dictionary<string, Texture2D> Textures { get { return textures; } }

		public Texture2D GetTexture(string name)
		{
			Texture2D result = null;
			Textures.TryGetValue(name, out result);
			return result;
		}

		public SaveModel()
		{
			SiblingBehaviour = SiblingBehaviours.None;
			SupportedVersion = new ListenerProperty<bool>(value => supportedVersion = value, () => supportedVersion);
			Path = new ListenerProperty<string>(value => path = value, () => path);
			Ignore = new ListenerProperty<bool>(value => ignore = value, () => ignore);
			Version = new ListenerProperty<int>(value => version = value, () => version);
			MetaKeyValues = new ListenerProperty<Dictionary<string, string>>(OnSetKeyValues, OnGetMetaKeyValues);
			Created = new ListenerProperty<DateTime>(value => created = value, () => created);
			Modified = new ListenerProperty<DateTime>(value => modified = value, () => modified);
		}

		#region Utility
		/// <summary>
		/// Sets the meta key. If set to null, the value is removed completely.
		/// </summary>
		/// <param name="key">Key.</param>
		/// <param name="value">Value.</param>
		public string SetMetaKey(string key, string value)
		{
			if (key == null) throw new ArgumentNullException("key");

			string currentValue;
			if (metaKeyValues.TryGetValue(key, out currentValue))
			{
				if (value == null) OnRemoveKeyValue(key);
				else if (currentValue != value) OnSetKeyValue(key, value);
			}
			else if (value != null) OnSetKeyValue(key, value);

			return value;
		}

		/// <summary>
		/// Gets the meta key. If it doesn't exist, null is returned.
		/// </summary>
		/// <returns>The meta key.</returns>
		/// <param name="key">Key.</param>
		public string GetMetaKey(string key)
		{
			if (key == null) throw new ArgumentNullException("key");

			string currentValue;
			if (metaKeyValues.TryGetValue(key, out currentValue)) return currentValue;
			return null;
		}

		protected void AddSiblings(params string[] siblingNames)
		{
			foreach (var name in siblingNames.Where(s => !specifiedSiblings.Contains(s))) specifiedSiblings.Add(name);
		}
		#endregion

		#region Events
		void OnSetKeyValues(Dictionary<string, string> newMetaKeyValues) { metaKeyValues = new Dictionary<string, string>(newMetaKeyValues); }
		Dictionary<string, string> OnGetMetaKeyValues() { return new Dictionary<string, string>(metaKeyValues); }

		void OnRemoveKeyValue(string key)
		{
			var newMetaKeyValues = MetaKeyValues.Value;
			newMetaKeyValues.Remove(key);
			MetaKeyValues.Value = newMetaKeyValues;
		}

		void OnSetKeyValue(string key, string value)
		{
			var newMetaKeyValues = MetaKeyValues.Value;
			newMetaKeyValues[key] = value;
			MetaKeyValues.Value = newMetaKeyValues;
		}

		public void PrepareTexture(string name, Texture2D texture)
		{
			OnPrepareTexture(name, texture);
		}

		protected virtual void OnPrepareTexture(string name, Texture2D texture) {}
		#endregion
	}
}