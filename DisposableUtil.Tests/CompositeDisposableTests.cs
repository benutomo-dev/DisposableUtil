using Xunit.Abstractions;

namespace DisposableUtil.Tests;

public class CompositeDisposableTests
{
    public ITestOutputHelper Output { get; }

    public CompositeDisposableTests(ITestOutputHelper output)
    {
        Output = output;
    }


    [Fact]
    [SuppressMessage("Assertions", "xUnit2013:Do not use equality check to check for collection size.", Justification = "検証対象となるプロパティを明示したい")]
    public void PureEmpty()
    {
        var compositeDisposable = new CompositeDisposable();

        Assert.False(compositeDisposable.IsReadOnly);
        Assert.Equal(0, compositeDisposable.Count);

        compositeDisposable.Dispose();
        compositeDisposable.Dispose();
        compositeDisposable.Dispose();
    }

    [Fact]
    public void EmptyAfterModified()
    {
        var compositeDisposable = new CompositeDisposable();

        Assert.False(compositeDisposable.IsReadOnly);
        Assert.Empty(compositeDisposable);

        var disposable = Disposable.Empty;

        compositeDisposable.Add(disposable);

        Assert.Single(compositeDisposable);

        compositeDisposable.Remove(disposable);

        Assert.Empty(compositeDisposable);

        compositeDisposable.Dispose();
        compositeDisposable.Dispose();
        compositeDisposable.Dispose();
    }

    [Fact]
    [SuppressMessage("Assertions", "xUnit2017:Do not use Contains() to check if a value exists in a collection", Justification = "検証対象となるメソッドを明示したい")]
    public void MultipleDispose()
    {
        var compositeDisposable = new CompositeDisposable();

        int disposedCount1 = 0;
        int disposedCount2 = 0;

        var disposable1 = Disposable.Create(() => disposedCount1++);
        var disposable2 = Disposable.Create(() => disposedCount2++);

        compositeDisposable.Add(disposable1);
        compositeDisposable.Add(Disposable.Empty);
        compositeDisposable.Add(Disposable.Empty);
        compositeDisposable.Add(disposable2);

        Assert.Equal(4, compositeDisposable.Count);
        Assert.True(compositeDisposable.Contains(Disposable.Empty));

        var disposableArray = new IDisposable[5];
        compositeDisposable.CopyTo(disposableArray, 1);

        Assert.Null(disposableArray[0]);
        Assert.Equal(disposableArray.Skip(1), compositeDisposable);

        compositeDisposable.Remove(Disposable.Empty);

        Assert.True(compositeDisposable.Contains(Disposable.Empty));
        Assert.Equal(3, compositeDisposable.Count);

        compositeDisposable.Remove(Disposable.Empty);

        Assert.False(compositeDisposable.Contains(Disposable.Empty));
        Assert.Equal(2, compositeDisposable.Count);

        compositeDisposable.Dispose();

        Assert.Equal(1, disposedCount1);
        Assert.Equal(1, disposedCount2);

        Assert.Throws<ObjectDisposedException>(() => compositeDisposable.Count);
        Assert.Throws<ObjectDisposedException>(() => compositeDisposable.Add(Disposable.Empty));
        Assert.Throws<ObjectDisposedException>(() => compositeDisposable.Remove(Disposable.Empty));
        Assert.Throws<ObjectDisposedException>(() => compositeDisposable.Contains(disposable1));
        Assert.Throws<ObjectDisposedException>(() => compositeDisposable.CopyTo(new IDisposable[2], 0));
        Assert.Throws<ObjectDisposedException>(() => compositeDisposable.Clear());
        Assert.Throws<ObjectDisposedException>(() => ((IEnumerable<IDisposable>)compositeDisposable).GetEnumerator());
        Assert.Throws<ObjectDisposedException>(() => ((System.Collections.IEnumerable)compositeDisposable).GetEnumerator());

        compositeDisposable.Dispose();

        Assert.Equal(1, disposedCount1);
        Assert.Equal(1, disposedCount2);
    }

    [Fact]
    public void MultipleDisposeWithDisposingException()
    {
        int disposedCount1 = 0;
        int disposedCount2 = 0;
        int disposedCount3 = 0;
        int disposedCount4 = 0;

        var disposable1 = Disposable.Create(() => disposedCount1++);
        var disposable2 = Disposable.Create(() => { disposedCount2++; throw new InvalidOperationException("Thrown from disposable2."); });
        var disposable3 = Disposable.Create(() => { disposedCount3++; throw new InvalidOperationException("Thrown from disposable3."); });
        var disposable4 = Disposable.Create(() => disposedCount4++);

        var compositeDisposable = new CompositeDisposable
        {
            disposable1,
            disposable2,
            disposable3,
            disposable4,
        };

        Assert.Equal(4, compositeDisposable.Count);

        var aggregateException = Assert.Throws<AggregateException>(() => compositeDisposable.Dispose());

        Assert.Equal(1, disposedCount1);
        Assert.Equal(1, disposedCount2);
        Assert.Equal(1, disposedCount3);
        Assert.Equal(1, disposedCount4);

        Assert.StartsWith("Exception was thrown in the call to Dispose.", aggregateException.Message);

        Assert.Equal(2, aggregateException.InnerExceptions.Count);
        Assert.IsType<InvalidOperationException>(aggregateException.InnerExceptions[0]);
        Assert.Equal("Thrown from disposable2.", aggregateException.InnerExceptions[0].Message);
        Assert.IsType<InvalidOperationException>(aggregateException.InnerExceptions[1]);
        Assert.Equal("Thrown from disposable3.", aggregateException.InnerExceptions[1].Message);

        compositeDisposable.Dispose();

        Assert.Equal(1, disposedCount1);
        Assert.Equal(1, disposedCount2);
        Assert.Equal(1, disposedCount3);
        Assert.Equal(1, disposedCount4);
    }


    [Fact]
    public void AccessAfterBeginDispose()
    {
        using var manualResetEvent1 = new ManualResetEvent(false);
        using var manualResetEvent2 = new ManualResetEvent(false);

        int disposedCount1 = 0;
        int disposedCount2 = 0;

        var disposable1 = Disposable.Create(() =>
        {
            disposedCount1++;
            manualResetEvent1.Set();
            manualResetEvent2.WaitOne();
        });

        var disposable2 = Disposable.Create(() =>
        {
            disposedCount2++;
        });

        var compositeDisposable = new CompositeDisposable
        {
            disposable1,
            disposable2,
        };

        using var enumerator1 = compositeDisposable.GetEnumerator();
        using var enumerator2 = compositeDisposable.GetEnumerator();
        using var enumerator3 = compositeDisposable.GetEnumerator();

        Assert.True(enumerator2.MoveNext());
        Assert.True(enumerator3.MoveNext());
        Assert.True(enumerator3.MoveNext());

        var disposeThread = new Thread(() => compositeDisposable.Dispose());
        disposeThread.Start();
        manualResetEvent1.WaitOne();

        Assert.Equal(1, disposedCount1);
        Assert.Equal(0, disposedCount2);

        Assert.Throws<ObjectDisposedException>(() => compositeDisposable.Count);
        Assert.Throws<ObjectDisposedException>(() => compositeDisposable.Add(Disposable.Empty));
        Assert.Throws<ObjectDisposedException>(() => compositeDisposable.Remove(Disposable.Empty));
        Assert.Throws<ObjectDisposedException>(() => compositeDisposable.Contains(disposable1));
        Assert.Throws<ObjectDisposedException>(() => compositeDisposable.CopyTo(new IDisposable[2], 0));
        Assert.Throws<ObjectDisposedException>(() => compositeDisposable.Clear());
        Assert.Throws<ObjectDisposedException>(() => ((IEnumerable<IDisposable>)compositeDisposable).GetEnumerator());
        Assert.Throws<ObjectDisposedException>(() => ((System.Collections.IEnumerable)compositeDisposable).GetEnumerator());

        Assert.Equal(1, disposedCount1);
        Assert.Equal(0, disposedCount2);

        manualResetEvent2.Set();
        disposeThread.Join();

        Assert.Equal(1, disposedCount1);
        Assert.Equal(1, disposedCount2);

        // 現在の実装ではDispose開始前に取得したenumerableは有効なまま使用可能
        Assert.True(enumerator1.MoveNext());
        Assert.Same(disposable1, enumerator1.Current);
        Assert.True(enumerator1.MoveNext());
        Assert.Same(disposable2, enumerator1.Current);
        Assert.False(enumerator1.MoveNext());
        enumerator1.Reset();
        Assert.True(enumerator1.MoveNext());
        Assert.Same(disposable1, enumerator1.Current);
        Assert.True(enumerator1.MoveNext());
        Assert.Same(disposable2, enumerator1.Current);
        Assert.False(enumerator1.MoveNext());

        Assert.True(enumerator2.MoveNext());
        Assert.Same(disposable2, enumerator2.Current);
        Assert.False(enumerator2.MoveNext());

        Assert.False(enumerator3.MoveNext());
    }

    [Fact]
    public void ParallelAdd()
    {
        int totalDisposeSuccessed = 0;
        int totalObjectDisposedExceptionAtAdd = 0;

        Parallel.For(0, 1000, new ParallelOptions { MaxDegreeOfParallelism = -1 }, _ =>
        {
            using var manualResetEvent = new ManualResetEvent(false);

            bool disposeSuccessed = false;
            bool objectDisposedExceptionAtAdd = false;
            Exception? exceptionAtDispose = null;

            var disposable = Disposable.Create(() =>
            {
                Volatile.Write(ref disposeSuccessed, true);
            });

            var compositeDisposable = new CompositeDisposable(Disposable.Empty);

            var addThread = new Thread(() =>
            {
                manualResetEvent.WaitOne();
                try
                {
                    compositeDisposable.Add(disposable);
                }
                catch (ObjectDisposedException)
                {
                    objectDisposedExceptionAtAdd = true;
                }
            });

            var disposeThread = new Thread(() =>
            {
                manualResetEvent.WaitOne();
                try
                {
                    compositeDisposable.Dispose();
                }
                catch (Exception ex)
                {
                    exceptionAtDispose = ex;
                }
            });

            addThread.Start();
            disposeThread.Start();
            Thread.Sleep(1);
            manualResetEvent.Set();

            addThread.Join();
            disposeThread.Join();

            Interlocked.MemoryBarrier();

            try
            {
                Assert.Null(exceptionAtDispose);

                if (objectDisposedExceptionAtAdd)
                {
                    Interlocked.Increment(ref totalObjectDisposedExceptionAtAdd);

                    // Addが失敗したならば、追加仕様とした要素はまだDisposeされない。
                    Assert.False(disposeSuccessed);
                }
                else
                {
                    // Addが成功したならば、追加仕様とした要素はDisposeされる。
                    Assert.True(disposeSuccessed);
                }

                if (disposeSuccessed) Interlocked.Increment(ref totalDisposeSuccessed);
            }
            catch
            {
                Output.WriteLine($"disposeSuccessed: {disposeSuccessed}");
                Output.WriteLine($"objectDisposedExceptionAtAdd: {objectDisposedExceptionAtAdd}");
                Output.WriteLine($"");

                throw;
            }
        });

        Interlocked.MemoryBarrier();

        Output.WriteLine($"totalDisposeSuccessed: {totalDisposeSuccessed}");
        Output.WriteLine($"totalObjectDisposedExceptionAtAdd: {totalObjectDisposedExceptionAtAdd}");
    }

    [Fact]
    public void ParallelRemove()
    {
        int totalRemoveSuccessed = 0;
        int totalDisposeSuccessed = 0;
        int totalObjectDisposedExceptionAtRemove = 0;

        Parallel.For(0, 1000, new ParallelOptions { MaxDegreeOfParallelism = -1 }, _ =>
        {
            using var manualResetEvent = new ManualResetEvent(false);

            bool disposeSuccessed = false;
            bool removeSuccessed = false;
            bool objectDisposedExceptionAtRemove = false;
            Exception? exceptionAtDispose = null;

            var disposable = Disposable.Create(() =>
            {
                Volatile.Write(ref disposeSuccessed, true);
            });

            var compositeDisposable = new CompositeDisposable(Disposable.Empty, disposable);

            var addThread = new Thread(() =>
            {
                manualResetEvent.WaitOne();
                try
                {
                    removeSuccessed = compositeDisposable.Remove(disposable);
                }
                catch (ObjectDisposedException)
                {
                    objectDisposedExceptionAtRemove = true;
                }
            });

            var disposeThread = new Thread(() =>
            {
                manualResetEvent.WaitOne();
                try
                {
                    compositeDisposable.Dispose();
                }
                catch (Exception ex)
                {
                    exceptionAtDispose = ex;
                }
            });

            addThread.Start();
            disposeThread.Start();
            Thread.Sleep(1);
            manualResetEvent.Set();

            addThread.Join();
            disposeThread.Join();

            Interlocked.MemoryBarrier();

            try
            {
                Assert.Null(exceptionAtDispose);

                if (removeSuccessed)
                {
                    Interlocked.Increment(ref totalRemoveSuccessed); ;

                    Assert.False(disposeSuccessed);
                    Assert.False(objectDisposedExceptionAtRemove);
                }
                else
                {
                    Assert.True(disposeSuccessed);
                    Assert.True(objectDisposedExceptionAtRemove);

                    if (disposeSuccessed) Interlocked.Increment(ref totalDisposeSuccessed);
                    if (objectDisposedExceptionAtRemove) Interlocked.Increment(ref totalObjectDisposedExceptionAtRemove);
                }
            }
            catch
            {
                Output.WriteLine($"disposeSuccessed: {disposeSuccessed}");
                Output.WriteLine($"removeSuccessed: {removeSuccessed}");
                Output.WriteLine($"objectDisposedExceptionAtRemove: {objectDisposedExceptionAtRemove}");
                Output.WriteLine($"");

                throw;
            }
        });

        Interlocked.MemoryBarrier();

        Output.WriteLine($"totalRemoveSuccessed: {totalRemoveSuccessed}");
        Output.WriteLine($"totalDisposeSuccessed: {totalDisposeSuccessed}");
        Output.WriteLine($"totalObjectDisposedExceptionAtRemove: {totalObjectDisposedExceptionAtRemove}");
    }

    [Fact]
    public void ParallelClear()
    {
        int totalDisposeSuccessed = 0;
        int totalObjectDisposedExceptionAtClear = 0;

        Parallel.For(0, 1000, new ParallelOptions { MaxDegreeOfParallelism = -1 }, _ =>
        {
            using var manualResetEvent = new ManualResetEvent(false);

            bool disposeSuccessed = false;
            bool objectDisposedExceptionAtClear = false;
            Exception? exceptionAtDispose = null;

            var disposable = Disposable.Create(() =>
            {
                Volatile.Write(ref disposeSuccessed, true);
            });

            var compositeDisposable = new CompositeDisposable(Disposable.Empty, disposable);

            var addThread = new Thread(() =>
            {
                manualResetEvent.WaitOne();
                try
                {
                    compositeDisposable.Clear();
                }
                catch (ObjectDisposedException)
                {
                    objectDisposedExceptionAtClear = true;
                }
            });

            var disposeThread = new Thread(() =>
            {
                manualResetEvent.WaitOne();
                try
                {
                    compositeDisposable.Dispose();
                }
                catch (Exception ex)
                {
                    exceptionAtDispose = ex;
                }
            });

            addThread.Start();
            disposeThread.Start();
            Thread.Sleep(1);
            manualResetEvent.Set();

            addThread.Join();
            disposeThread.Join();

            Interlocked.MemoryBarrier();

            try
            {
                Assert.Null(exceptionAtDispose);

                if (objectDisposedExceptionAtClear)
                {
                    Interlocked.Increment(ref totalObjectDisposedExceptionAtClear);

                    Assert.True(disposeSuccessed);
                }
                else
                {
                    Assert.False(disposeSuccessed);
                }

                if (disposeSuccessed) Interlocked.Increment(ref totalDisposeSuccessed);
            }
            catch
            {
                Output.WriteLine($"disposeSuccessed: {disposeSuccessed}");
                Output.WriteLine($"objectDisposedExceptionAtClear: {objectDisposedExceptionAtClear}");
                Output.WriteLine($"");

                throw;
            }
        });

        Interlocked.MemoryBarrier();

        Output.WriteLine($"totalDisposeSuccessed: {totalDisposeSuccessed}");
        Output.WriteLine($"totalObjectDisposedExceptionAtClear: {totalObjectDisposedExceptionAtClear}");
    }

    [Fact]
    public void WithAddTo()
    {
        var compositeDisposable = new CompositeDisposable();

        var disposable = Disposable.Empty;

        var returnedDisposable = disposable.WithAddTo(compositeDisposable);

        Assert.Single(compositeDisposable);
        Assert.Contains(disposable, compositeDisposable);

        Assert.Same(disposable, returnedDisposable);
    }
}
