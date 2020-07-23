using System;
using System.Collections.Generic;
using Lunra.Core;

namespace Lunra.StyxMvp.Models
{
    public class ListenerProperty<T>
    {
        readonly Action<T> set;
        readonly Func<T> get;
        readonly Func<T, bool> equalityComparer;

        public event Action<T> Changed = ActionExtensions.GetEmpty<T>();
        public event Action<T, object> ChangedSource = ActionExtensions.GetEmpty<T, object>();

        public T Value
        {
            get => get();
            set => SetValue(value);
        }

        public bool SetValue(T value, object source = default)
        {
            if (equalityComparer(value)) return false;
            set(value);
            Changed(value);
            ChangedSource(value, source);
            return true;
        }

        public ListenerProperty(
            Action<T> set,
            Func<T> get,
            params Action<T>[] listeners
        ) : this(
            set,
            get,
            value => EqualityComparer<T>.Default.Equals(get(), value),
            listeners
        ) { }
        
        public ListenerProperty(
            Action<T> set,
            Func<T> get,
            Func<T, bool> equalityComparer,
            params Action<T>[] listeners
        )
        {
            this.set = set;
            this.get = get;
            this.equalityComparer = equalityComparer;

            foreach (var listener in listeners) Changed += listener;
        }
    }
}