using System;
using System.Collections.Generic;

namespace LunraGames.SubLight
{
	public class DerivedProperty<T, P>
	{
		Action<T> set;
		Func<T> get;
		Func<P, T> derive;

		public Action<T> Changed = ActionExtensions.GetEmpty<T>();

		public T Value 
		{ 
			get { return get(); } 
			private set 
			{
				if (EqualityComparer<T>.Default.Equals(get(), value)) return;
				set(value);
				Changed(value);
			}
		}

		public DerivedProperty(
			Action<T> set, 
			Func<T> get,
			Func<P, T> derive, 
			ListenerProperty<P> property, 
			params Action<T>[] listeners)
		{
			this.set = set;
			this.get = get;
			this.derive = derive;

			property.Changed += OnProperty;

			foreach (var listener in listeners) Changed += listener;
		}

		void OnProperty(P property) { Value = derive(property); }
	}

	public class DerivedProperty<T, P0, P1>
	{
		Action<T> set;
		Func<T> get;
		Func<P0, P1, T> derive;
		ListenerProperty<P0> property0;
		ListenerProperty<P1> property1;

		public Action<T> Changed = ActionExtensions.GetEmpty<T>();

		public T Value
		{
			get { return get(); }
			private set
			{
				if (EqualityComparer<T>.Default.Equals(get(), value)) return;
				set(value);
				Changed(value);
			}
		}

		public DerivedProperty(
			Action<T> set,
			Func<T> get,
			Func<P0, P1, T> derive,
			ListenerProperty<P0> property0,
			ListenerProperty<P1> property1,
			params Action<T>[] listeners)
		{
			this.set = set;
			this.get = get;
			this.derive = derive;
			this.property0 = property0;
			this.property1 = property1;

			property0.Changed += value => OnProperty();
			property1.Changed += value => OnProperty();

			foreach (var listener in listeners) Changed += listener;
		}

		void OnProperty() { Value = derive(property0.Value, property1.Value); }
	}

	public class DerivedProperty<T, P0, P1, P2>
	{
		Action<T> set;
		Func<T> get;
		Func<P0, P1, P2, T> derive;
		ListenerProperty<P0> property0;
		ListenerProperty<P1> property1;
		ListenerProperty<P2> property2;

		public Action<T> Changed = ActionExtensions.GetEmpty<T>();

		public T Value
		{
			get { return get(); }
			private set
			{
				if (EqualityComparer<T>.Default.Equals(get(), value)) return;
				set(value);
				Changed(value);
			}
		}

		public DerivedProperty(
			Action<T> set,
			Func<T> get,
			Func<P0, P1, P2, T> derive,
			ListenerProperty<P0> property0,
			ListenerProperty<P1> property1,
			ListenerProperty<P2> property2,
			params Action<T>[] listeners)
		{
			this.set = set;
			this.get = get;
			this.derive = derive;
			this.property0 = property0;
			this.property1 = property1;
			this.property2 = property2;

			property0.Changed += value => OnProperty();
			property1.Changed += value => OnProperty();
			property2.Changed += value => OnProperty();

			foreach (var listener in listeners) Changed += listener;
		}

		void OnProperty() { Value = derive(property0.Value, property1.Value, property2.Value); }
	}

	public class DerivedProperty<T, P0, P1, P2, P3>
	{
		Action<T> set;
		Func<T> get;
		Func<P0, P1, P2, P3, T> derive;
		ListenerProperty<P0> property0;
		ListenerProperty<P1> property1;
		ListenerProperty<P2> property2;
		ListenerProperty<P3> property3;

		public Action<T> Changed = ActionExtensions.GetEmpty<T>();

		public T Value
		{
			get { return get(); }
			private set
			{
				if (EqualityComparer<T>.Default.Equals(get(), value)) return;
				set(value);
				Changed(value);
			}
		}

		public DerivedProperty(
			Action<T> set,
			Func<T> get,
			Func<P0, P1, P2, P3, T> derive,
			ListenerProperty<P0> property0,
			ListenerProperty<P1> property1,
			ListenerProperty<P2> property2,
			ListenerProperty<P3> property3,
			params Action<T>[] listeners)
		{
			this.set = set;
			this.get = get;
			this.derive = derive;
			this.property0 = property0;
			this.property1 = property1;
			this.property2 = property2;
			this.property3 = property3;

			property0.Changed += value => OnProperty();
			property1.Changed += value => OnProperty();
			property2.Changed += value => OnProperty();
			property3.Changed += value => OnProperty();

			foreach (var listener in listeners) Changed += listener;
		}

		void OnProperty() { Value = derive(property0.Value, property1.Value, property2.Value, property3.Value); }
	}

	public class DerivedProperty<T, P0, P1, P2, P3, P4>
	{
		Action<T> set;
		Func<T> get;
		FuncExtensions.Func<P0, P1, P2, P3, P4, T> derive;
		ListenerProperty<P0> property0;
		ListenerProperty<P1> property1;
		ListenerProperty<P2> property2;
		ListenerProperty<P3> property3;
		ListenerProperty<P4> property4;

		public Action<T> Changed = ActionExtensions.GetEmpty<T>();

		public T Value
		{
			get { return get(); }
			private set
			{
				if (EqualityComparer<T>.Default.Equals(get(), value)) return;
				set(value);
				Changed(value);
			}
		}

		public DerivedProperty(
			Action<T> set,
			Func<T> get,
			FuncExtensions.Func<P0, P1, P2, P3, P4, T> derive,
			ListenerProperty<P0> property0,
			ListenerProperty<P1> property1,
			ListenerProperty<P2> property2,
			ListenerProperty<P3> property3,
			ListenerProperty<P4> property4,
			params Action<T>[] listeners)
		{
			this.set = set;
			this.get = get;
			this.derive = derive;
			this.property0 = property0;
			this.property1 = property1;
			this.property2 = property2;
			this.property3 = property3;
			this.property4 = property4;

			property0.Changed += value => OnProperty();
			property1.Changed += value => OnProperty();
			property2.Changed += value => OnProperty();
			property3.Changed += value => OnProperty();
			property4.Changed += value => OnProperty();

			foreach (var listener in listeners) Changed += listener;
		}

		void OnProperty() { Value = derive(property0.Value, property1.Value, property2.Value, property3.Value, property4.Value); }
	}
}