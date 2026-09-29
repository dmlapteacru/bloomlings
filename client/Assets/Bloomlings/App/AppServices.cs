using System;
using System.Collections.Generic;

namespace Bloomlings.Client.App
{
    /// <summary>
    /// A minimal service registry, filled once by <see cref="Boot"/> (no third-party DI). Services are registered by
    /// their interface type and looked up by the same type.
    /// </summary>
    public sealed class AppServices
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        /// <summary>The registry of the running app; null until <see cref="Boot"/> has finished.</summary>
        public static AppServices? Current { get; private set; }

        public static bool IsReady => Current != null;

        public void Register<T>(T service)
            where T : class
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            if (_services.ContainsKey(typeof(T)))
            {
                throw new InvalidOperationException($"{typeof(T).Name} is already registered.");
            }

            _services.Add(typeof(T), service);
        }

        public T Get<T>()
            where T : class
        {
            if (TryGet(out T? service))
            {
                return service!;
            }

            throw new InvalidOperationException($"{typeof(T).Name} is not registered. Start the game from the Boot scene.");
        }

        public bool TryGet<T>(out T? service)
            where T : class
        {
            if (_services.TryGetValue(typeof(T), out object? value) && value != null)
            {
                service = (T)value;
                return true;
            }

            service = null;
            return false;
        }

        /// <summary>Publishes a fully built registry. Called once by <see cref="Boot"/>; tests may call it with fakes.</summary>
        public static void MakeCurrent(AppServices services) => Current = services;

        /// <summary>Clears the current registry (tests only).</summary>
        public static void ResetForTests() => Current = null;
    }
}
