using System;
using System.Collections.Generic;
using Lunra.Core;

namespace Lunra.StyxMvp.Models
{
    public class ListenerProperty<T>
    {
        public readonly string Name;

        readonly Action<T> set;
        readonly Func<T> get;

        public event Action<T> Changed = ActionExtensions.GetEmpty<T>();
        public event Action<T, PropertySources> ChangedSource = ActionExtensions.GetEmpty<T, PropertySources>();

        public T Value
        {
            get => get();
            set => SetValue(value);
        }

        public bool SetValue(T value, PropertySources source = PropertySources.Unknown)
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