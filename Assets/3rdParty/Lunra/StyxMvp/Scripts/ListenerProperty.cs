using System;
using System.Collections.Generic;
using Lunra.Core;

namespace Lunra.StyxMvp
{
    public enum ListenerPropertySources
    {
        Unknown = 0,
        Internal = 10,
        External = 20
    }

    public class ListenerProperty<T>
    {
        public readonly string Name;

        Action<T> set;
        Func<T> get;

        public Action<T> Changed = ActionExtensions.GetEmpty<T>();
        public Action<T, ListenerPropertySources> ChangedSource = ActionExtensions.GetEmpty<T, ListenerPropertySources>();

        public T Value
        {
            get => get();
            set => SetValue(value);
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