using System;
using Lunra.Core;
using Newtonsoft.Json;

namespace Lunra.StyxMvp.Models
{
	public interface IModel 
	{
		ListenerProperty<string> Id { get; }
	}

	[JsonObject(MemberSerialization.OptIn)]
	public abstract class Model : IModel
	{
		[JsonProperty] string id;
		readonly ListenerProperty<string> idListener;
		/// <summary>
		/// Id used to identify serialized models.
		/// </summary>
		public ListenerProperty<string> Id => idListener;

		public Model()
		{
			idListener = new ListenerProperty<string>(value => id = value, () => id);
		}

		public override string ToString() => this.ToReadableJson();
	}
}
