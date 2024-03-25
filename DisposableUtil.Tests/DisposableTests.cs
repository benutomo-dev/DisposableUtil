namespace DisposableUtil.Tests;

public class DisposableTests
{
    [Fact]
    public void DisposeEmpty()
    {
        Disposable.Empty.Dispose();
        Disposable.Empty.Dispose();
        Disposable.Empty.Dispose();
    }

    [Fact]
    public void ActionDisposable_Parallel()
    {
        var calledCount = 0;

        using var manualResetEvent = new ManualResetEvent(false);

        var disposable = Disposable.Create(() =>
        {
            calledCount++;
            manualResetEvent.WaitOne();
        });

        var th1 = new Thread(_ => { disposable.Dispose(); });
        var th2 = new Thread(_ => { disposable.Dispose(); });

        th1.Start();
        th2.Start();

        manualResetEvent.Set();

        th1.Join();
        th2.Join();

        Assert.Equal(1, calledCount);
    }

    [Fact]
    public void ActionDisposable_Throwing()
    {
        var calledCount = 0;

        var disposable = Disposable.Create(() =>
        {
            calledCount++;
            throw new InvalidOperationException("Thrown at disposing.");
        });

        try
        {
            disposable.Dispose();
            Assert.Fail("Not Thrown.");
        }
        catch (Exception ex)
        {
            Assert.IsType<InvalidOperationException>(ex);
            Assert.Equal("Thrown at disposing.", ex.Message);
        }

        Assert.Equal(1, calledCount);

        disposable.Dispose(); // not thrown

        Assert.Equal(1, calledCount);
    }

    [Fact]
    public void ActionTDisposable_Parallel()
    {
        var calledCount = 0;
        var actualArg = null as string;

        using var manualResetEvent = new ManualResetEvent(false);

        var disposable = Disposable.Create("action arg", arg =>
        {
            calledCount++;
            actualArg = arg;
            manualResetEvent.WaitOne();
        });

        var th1 = new Thread(_ => { disposable.Dispose(); });
        var th2 = new Thread(_ => { disposable.Dispose(); });

        th1.Start();
        th2.Start();

        manualResetEvent.Set();

        th1.Join();
        th2.Join();

        Assert.Equal(1, calledCount);
        Assert.Equal("action arg", actualArg);
    }

    [Fact]
    public void ActionTDisposable_Throwing()
    {
        var calledCount = 0;
        var actualArg = null as string;

        var disposable = Disposable.Create("action arg", arg =>
        {
            calledCount++;
            actualArg = arg;
            throw new InvalidOperationException("Thrown at disposing.");
        });

        try
        {
            disposable.Dispose();
            Assert.Fail("Not Thrown.");
        }
        catch (Exception ex)
        {
            Assert.IsType<InvalidOperationException>(ex);
            Assert.Equal("Thrown at disposing.", ex.Message);
        }

        Assert.Equal(1, calledCount);
        Assert.Equal("action arg", actualArg);

        disposable.Dispose(); // not thrown

        Assert.Equal(1, calledCount);
    }
}