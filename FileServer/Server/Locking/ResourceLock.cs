using System;
using System.Threading;

namespace FileServer.Server.Locking
{
    internal class ResourceLock<T> : IDisposable
    {
        private readonly ReaderWriterLockSlim _lock;
        private readonly T _resource;
        private volatile bool _disposed;

        public ResourceLock(T resource)
        {
            _resource = resource;
            _lock = new ReaderWriterLockSlim();
        }

        public T Resource
        {
            get
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(ToString());
                }
                return _resource;
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            _lock.Dispose();
        }

        public void Acquire(LockKind kind)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(ToString());
            }
            switch (kind)
            {
                case LockKind.Write:
                {
                    _lock.EnterWriteLock();
                    break;
                }
                case LockKind.Read:
                {
                    _lock.EnterReadLock();
                    break;
                }
            }
        }

        public void Release(LockKind lockKind)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(ToString());
            }
            switch (lockKind)
            {
                case LockKind.Write:
                    _lock.ExitWriteLock();
                    break;
                case LockKind.Read:
                    _lock.ExitReadLock();
                    break;
            }
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (GetType() != obj.GetType())
            {
                return false;
            }
            return _resource.Equals(((ResourceLock<T>) obj).Resource);
        }

        public override int GetHashCode()
        {
            return _resource.GetHashCode();
        }

        ~ResourceLock()
        {
            Dispose();
        }
    }
}