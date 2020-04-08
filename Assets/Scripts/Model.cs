using System;

using Newtonsoft.Json;

namespace LunraGames.SubLight
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
		[JsonIgnore] public ListenerProperty<string> Id { get { return idListener; } }

		public Model()
		{
			idListener = new ListenerProperty<string>(value => id = value, () => id);
		}

		public override string ToString() => this.ToReadableJson();
	}
}
