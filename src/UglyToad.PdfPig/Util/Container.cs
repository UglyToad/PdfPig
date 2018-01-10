namespace UglyToad.PdfPig.Util
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    internal class Container : IContainer
    {
        private readonly Dictionary<Type, object> objects = new Dictionary<Type, object>();
        
        public void Register<T>(T obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj), "Cannot register a null object with the container. Type was: " + typeof(T));
            }

            objects[typeof(T)] = obj;
            var interfaces = typeof(T).GetInterfaces();

            foreach (var @interface in interfaces)
            {
                objects[@interface] = obj;
            }
        }

        [DebuggerStepThrough]
        public T Get<T>()
        {
            if (!objects.TryGetValue(typeof(T), out var obj))
            {
                throw new InvalidOperationException($"The type {typeof(T)} was not registered with the container.");
            }

            return (T) obj;
        }
    }
}