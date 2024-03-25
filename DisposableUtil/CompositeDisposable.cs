using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace Benutomo.DisposableUtil;

public class CompositeDisposable : ICollection<IDisposable>, IDisposable
{
    public int Count
    {
        get
        {
            var disposables = Volatile.Read(ref _disposables);
            if (disposables is null)
            {
                ThrowObjectDisposedException();
            }
            return disposables.Count;
        }
    }

    public bool IsReadOnly => false;


    private List<IDisposable>? _disposables;

    private ReadOnlyCollection<IDisposable>? _enumerableCache;

    public CompositeDisposable()
    {
        _disposables = new List<IDisposable>();
    }

    public CompositeDisposable(params IDisposable[] disposables)
    {
        _disposables = disposables.ToList();
    }

    public CompositeDisposable(IEnumerable<IDisposable> disposables)
    {
        _disposables = disposables.ToList();
    }

    public void Dispose()
    {
        var gate = Interlocked.Exchange(ref _disposables, null);

        if (gate is null) return;

        lock (gate)
        {
            var disposables = gate;

            List<Exception>? exceptions = null;

            foreach (var disposable in disposables)
            {
                try
                {
                    disposable?.Dispose();
                }
                catch (Exception ex)
                {
                    exceptions ??= new List<Exception>();
                    exceptions.Add(ex);
                }
            }

            if (exceptions is not null)
            {
                throw new AggregateException("Exception was thrown in the call to Dispose.", exceptions);
            }
        }
    }

    public void Add(IDisposable item)
    {
        var gate = Volatile.Read(ref _disposables);

        if (gate is null)
        {
            ThrowObjectDisposedException();
        }

        lock(gate)
        {
            var disposables = _disposables;
            if (disposables is null)
            {
                item.Dispose();
                return;
            }

            _enumerableCache = null;
            disposables.Add(item);
        }
    }

    public bool Remove(IDisposable item)
    {
        var gate = Volatile.Read(ref _disposables);

        if (gate is null)
        {
            ThrowObjectDisposedException();
        }

        lock (gate)
        {
            var disposables = _disposables;
            if (disposables is null)
            {
                ThrowObjectDisposedException();
            }

            _enumerableCache = null;
            return disposables.Remove(item);
        }
    }

    public void Clear()
    {
        var gate = Volatile.Read(ref _disposables);

        if (gate is null)
        {
            ThrowObjectDisposedException();
        }

        lock (gate)
        {
            var disposables = _disposables;
            if (disposables is null)
            {
                ThrowObjectDisposedException();
            }

            _enumerableCache = null;
            disposables.Clear();
        }
    }

    public bool Contains(IDisposable item)
    {
        var gate = Volatile.Read(ref _disposables);

        if (gate is null)
        {
            ThrowObjectDisposedException();
        }

        lock (gate)
        {
            var disposables = _disposables;
            if (disposables is null)
            {
                ThrowObjectDisposedException();
            }

            return disposables.Contains(item);
        }
    }

    public void CopyTo(IDisposable[] array, int arrayIndex)
    {
        var gate = Volatile.Read(ref _disposables);

        if (gate is null)
        {
            ThrowObjectDisposedException();
        }

        lock (gate)
        {
            var disposables = _disposables;
            if (disposables is null)
            {
                ThrowObjectDisposedException();
            }

            disposables.CopyTo(array, arrayIndex);
        }
    }

    public IEnumerator<IDisposable> GetEnumerator()
    {
        var gate = Volatile.Read(ref _disposables);

        if (gate is null)
        {
            ThrowObjectDisposedException();
        }

        ReadOnlyCollection<IDisposable> enumerable;
        lock (gate)
        {
            var disposables = _disposables;
            if (disposables is null)
            {
                ThrowObjectDisposedException();
            }

            if (_enumerableCache is null)
            {
                _enumerableCache = new ReadOnlyCollection<IDisposable>(disposables.ToArray());
            }

            enumerable = _enumerableCache;
        }

        return enumerable.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    [DoesNotReturn]
    private void ThrowObjectDisposedException() => throw new ObjectDisposedException(null);
}
