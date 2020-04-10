using System;
using Lunra.Core;
using Newtonsoft.Json;

namespace Lunra.StyxMvp.Models
{
	public interface IModel 
	{
		ListenerProperty<string> Id { get; }
	}

	[Serializable]
	public abstract class Model : IModel
	{
		[JsonProperty] string id;
		[JsonIgnore] readonly ListenerProperty<string> idListener;
		/// <summary>
		/// Id used to identify serialized models.
		/// </summary>
		[JsonIgnore] public ListenerProperty<string> Id => idListener;

		public Model()
		{
			idListener = new ListenerProperty<string>(value => id = value, () => id);
		}

		public override string ToString() => this.ToReadableJson();
	}
}
