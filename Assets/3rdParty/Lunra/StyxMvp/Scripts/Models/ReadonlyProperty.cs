using System;
using System.Collections.Generic;
using Lunra.Core;

namespace Lunra.StyxMvp.Models
{
    public class ReadonlyProperty<T>
    {
        ListenerProperty<T> property;

        public event Action<T> Changed = ActionExtensions.GetEmpty<T>();

        public T Value => property.Value;

        public ReadonlyProperty(
            Action<T> set,
            Func<T> get,
            out ListenerProperty<T> property,
            params Action<T>[] listeners
        ) : this(
            set,
            get,
            value => EqualityComparer<T>.Default.Equals(get(), value),
            out property,
            listeners
        ) { }
        
        public ReadonlyProperty(
            Action<T> set,
            Func<T> get,
            Func<T, bool> equalityComparer,
            out ListenerProperty<T> property,
            params Action<T>[] listeners
        )
        {
            this.property = new ListenerProperty<T>(set, get, equalityComparer, listeners);
            this.property.Changed += value => Changed(value);
            property = this.property;
        }
    }
}