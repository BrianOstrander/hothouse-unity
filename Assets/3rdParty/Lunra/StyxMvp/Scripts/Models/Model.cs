using System;
using Lunra.Core;
using Newtonsoft.Json;

namespace Lunra.StyxMvp.Models
{
	public interface IModel 
	{
		#region Serialized
		ListenerProperty<string> Id { get; }
		#endregion
		
		#region Non Serialized
		string ShortId { get; }
		#endregion
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

		[JsonIgnore] public string ShortId => ShortenId(Id.Value);
		
		public static string ShortenId(string id) => StringExtensions.GetNonNullOrEmpty(
			id == null ? "< null Id >" : (id.Length < 4 ? id : id.Substring(0, 4)),
			"< empty Id >"
		); 
	}
}
