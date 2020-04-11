using System;
using Lunra.Core;

namespace Lunra.StyxMvp.Models
{
    public class ReadonlyProperty<T>
    {
        public readonly string Name;
        ListenerProperty<T> property;

        public Action<T> Changed = ActionExtensions.GetEmpty<T>();

        public T Value => property.Value;

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

        public ReadonlyProperty(Action<T> set, Func<T> get, out ListenerProperty<T> property, params Action<T>[] listeners) : this(set, get, out property, null, listeners) {}
    }
}