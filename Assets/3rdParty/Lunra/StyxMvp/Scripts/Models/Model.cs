using System;
using Lunra.Core;
using Newtonsoft.Json;

namespace Lunra.StyxMvp.Models
{
	public interface IModel 
	{
		ListenerProperty<string> Id { get; }
	}

	public abstract class Model : IModel
	{
		[JsonProperty] string id;
		/// <summary>
		/// Id used to identify serialized models.
		/// </summary>
		[JsonIgnore] public ListenerProperty<string> Id { get; }

		public Model()
		{
			Id = new ListenerProperty<string>(value => id = value, () => id);
		}

		public override string ToString() => this.ToReadableJson();
	}
}
