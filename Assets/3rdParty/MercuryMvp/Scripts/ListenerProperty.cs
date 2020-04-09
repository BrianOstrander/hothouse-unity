using System;
using System.Collections.Generic;

namespace LunraGames.SubLight
{
    public enum ListenerPropertySources
    {
        Unknown = 0,
        Internal = 10,
        External = 20
    }

    public class ListenerProperty<T>
    {
        public string Name { get; private set; }

        Action<T> set;
        Func<T> get;
        ListenerPropertySources source = ListenerPropertySources.Unknown;

        public Action<T> Changed = ActionExtensions.GetEmpty<T>();
        public Action<T, ListenerPropertySources> ChangedSource = ActionExtensions.GetEmpty<T, ListenerPropertySources>();

        public T Value
        {
            get { return get(); }
            set { SetValue(value, ListenerPropertySources.Unknown); }
        }

        public bool SetValue(T value, ListenerPropertySources source = ListenerPropertySources.Unknown)
        {
            if (EqualityComparer<T>.Default.Equals(get(), value)) return false;
            set(value);
            Changed(value);
            ChangedSource(value, source);
            return true;
        }

        public ListenerProperty(
            Action<T> set,
            Func<T> get,
            string name,
            params Action<T>[] listeners
        )
        {
            Name = name;
            this.set = set;
            this.get = get;

            foreach (var listener in listeners) Changed += listener;
        }

        public ListenerProperty(
            Action<T> set,
            Func<T> get,
            params Action<T>[] listeners
        ) : this (
            set,
            get,
            null,
            listeners
        ) {}
    }
}