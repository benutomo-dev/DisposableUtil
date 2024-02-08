using Benutomo.DisposableUtil;
using System.Reactive;
using System.Reactive.Linq;

Console.WriteLine("Hello, World!");

var observable = Observable.Empty<Unit>();

using (var compositDisposable = new CompositeDisposable())
{
    var disposableA = new DisposableA();
    compositDisposable.Add(disposableA);

    var disposableB = new DisposableB().WithAddTo(compositDisposable);

    observable
        .Subscribe(v => Console.WriteLine(v))
        .WithAddTo(compositDisposable);
} // Disposed: disposableA, disposableB, subscriber of observable


var someObject1 = new SomeClass();
var someObject2 = new SomeClass();
var someObject3 = new SomeClass();

try
{
    try
    {
        try
        {
            // Something...
        }
        finally
        {
            someObject1.SomeMethodA();
        }
    }
    finally
    {
        someObject2.SomeMethodB();
    }
}
finally
{
    someObject3.SomeMethodC();
}

using (Disposable.AsFinally(someObject3, v => v.SomeMethodC()))
using (Disposable.AsFinally(someObject2, v => v.SomeMethodB()))
using (Disposable.AsFinally(someObject1, v => v.SomeMethodA()))
using (CreateDisposable())
{
    // Something...
}


IDisposable CreateDisposable()
{
    return Disposable.Create(() => Console.WriteLine("Called IDisposable.Dipose()."));
}

class SomeClass
{
    public void SomeMethodA() => Console.WriteLine("SomeMethodA()");
    public void SomeMethodB() => Console.WriteLine("SomeMethodB()");
    public void SomeMethodC() => Console.WriteLine("SomeMethodC()");
}

class DisposableA : IDisposable
{
    public void Dispose()
    {
        Console.WriteLine("DisposableA.Dispose()");
    }
}

class DisposableB : IDisposable
{
    public void Dispose()
    {
        Console.WriteLine("DisposableB.Dispose()");
    }
}
