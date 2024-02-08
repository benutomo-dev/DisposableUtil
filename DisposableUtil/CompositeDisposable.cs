using System.Collections;
using System.Collections.ObjectModel;

namespace Benutomo.DisposableUtil;

public class CompositeDisposable : ICollection<IDisposable>, IDisposable
{
    public int Count
    {
        get
        {
            var count = Volatile.Read(ref _count);
            if (count < 0)
            {
                throw new ObjectDisposedException(null);
            }
            return count;
        }
    }

    public bool IsReadOnly => false;


    private List<IDisposable>? _disposables;

    private ReadOnlyCollection<IDisposable>? _enumerableCache;

    private int _count;

    public CompositeDisposable(params IDisposable[] disposables)
    {
        _disposables = disposables.ToList();
        _count = _disposables.Count;
    }

    public void Dispose()
    {
        var disposables = Interlocked.Exchange(ref _disposables, null);

        if (disposables is null) return;

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

    public void Add(IDisposable item)
    {
        var gate = Volatile.Read(ref _disposables);

        if (gate is null)
        {
            throw new ObjectDisposedException(null);
        }

        lock(gate)
        {
            if (_disposables is null)
            {
                throw new ObjectDisposedException(null);
            }

            _enumerableCache = null;
            _disposables.Add(item);
        }
    }

    public bool Remove(IDisposable item)
    {
        var gate = Volatile.Read(ref _disposables);

        if (gate is null)
        {
            throw new ObjectDisposedException(null);
        }

        lock (gate)
        {
            if (_disposables is null)
            {
                throw new ObjectDisposedException(null);
            }

            _enumerableCache = null;
            return _disposables.Remove(item);
        }
    }

    public void Clear()
    {
        var gate = Volatile.Read(ref _disposables);

        if (gate is null)
        {
            throw new ObjectDisposedException(null);
        }

        lock (gate)
        {
            if (_disposables is null)
            {
                throw new ObjectDisposedException(null);
            }

            _enumerableCache = null;
            _disposables.Clear();
        }
    }

    public bool Contains(IDisposable item)
    {
        var gate = Volatile.Read(ref _disposables);

        if (gate is null)
        {
            throw new ObjectDisposedException(null);
        }

        lock (gate)
        {
            if (_disposables is null)
            {
                throw new ObjectDisposedException(null);
            }

            return _disposables.Contains(item);
        }
    }

    public void CopyTo(IDisposable[] array, int arrayIndex)
    {
        var gate = Volatile.Read(ref _disposables);

        if (gate is null)
        {
            throw new ObjectDisposedException(null);
        }

        lock (gate)
        {
            if (_disposables is null)
            {
                throw new ObjectDisposedException(null);
            }

            _disposables.CopyTo(array, arrayIndex);
        }
    }

    public IEnumerator<IDisposable> GetEnumerator()
    {
        var gate = Volatile.Read(ref _disposables);

        if (gate is null)
        {
            throw new ObjectDisposedException(null);
        }

        ReadOnlyCollection<IDisposable> enumerable;
        lock (gate)
        {
            if (_disposables is null)
            {
                throw new ObjectDisposedException(null);
            }

            if (_enumerableCache is null)
            {
                _enumerableCache = new ReadOnlyCollection<IDisposable>(_disposables.ToArray());
            }

            enumerable = _enumerableCache;
        }

        return enumerable.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
