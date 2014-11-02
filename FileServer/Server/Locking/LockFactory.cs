using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace FileServer.Server.Locking
{
    internal class LockFactory<T> : IDisposable
    {
        private readonly IDictionary<T, ResourceLock<T>> _resourceLocks;
        private readonly object _syncObj = new object();
        private volatile bool _disposed;

        public LockFactory()
        {
            _resourceLocks = new ConcurrentDictionary<T, ResourceLock<T>>();
        }

        public bool IsDisposed
        {
            get { return _disposed; }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            foreach (var resourceLock in _resourceLocks.Values)
            {
                resourceLock.Dispose();
            }
            _resourceLocks.Clear();
        }

        public ResourceLock<T> Get(T resource)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(ToString());
            }
            if (!_resourceLocks.ContainsKey(resource))
            {
                lock (_syncObj)
                {
                    if (!_resourceLocks.ContainsKey(resource))
                    {
                        _resourceLocks[resource] = new ResourceLock<T>(resource);
                    }
                }
            }
            return _resourceLocks[resource];
        }

        ~LockFactory()
        {
            Dispose();
        }
    }
}