using System;

namespace LunraGames.SubLight
{
	public class ReadonlyProperty<T>
	{
		public string Name { get; private set; }
		ListenerProperty<T> property;

		public Action<T> Changed = ActionExtensions.GetEmpty<T>();

		public T Value { get { return property.Value; } }

		public ReadonlyProperty(
			Action<T> set,
			Func<T> get,
			out ListenerProperty<T> property,
			string name,
			params Action<T>[] listeners
		)
		{
			Name = name;
			this.property = new ListenerProperty<T>(set, get, name, listeners);
			this.property.Changed += value => Changed(value);
			property = this.property;
		}

		public ReadonlyProperty(Action<T> set, Func<T> get, out ListenerProperty<T> property, params Action<T>[] listeners) : this(set, get, out property, null, listeners) { }

		/// <summary>
		/// Converts the ModelProperty to the associated type.
		/// </summary>
		/// <remarks>
		/// Only one way casting is supported so that the callbacks bound to the Changed action aren't lost.
		/// </remarks>
		/// <returns>The implicit.</returns>
		/// <param name="p">P.</param>
		public static implicit operator T(ReadonlyProperty<T> p)
		{
			return p.Value;
		}
	}
}