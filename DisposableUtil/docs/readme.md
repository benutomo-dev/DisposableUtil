# DisposableUtil

A utility library of Disposable.

## Sample Code

### Disposable.Create

Creates an IDisposable object from a lambda function.

```cs
IDisposable CreateDisposable()
{
    return Disposable.Create(() => Console.WriteLine("Called IDisposable.Dipose()."));
}
```

### Disposable.Finally

The following nested try-finally can be easily described by using.

```cs
// Nested try-finally pattern

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
```

```cs
// Disposable.AsFinally pattern

using (Disposable.AsFinally(someObject3, v => v.SomeMethodC()))
using (Disposable.AsFinally(someObject2, v => v.SomeMethodB()))
using (Disposable.AsFinally(someObject1, v => v.SomeMethodA()))
{
    // Something...
}
```

## CompositeDisposable

A collection that combines multiple IDisposable into one, and an extension method to assist with additions.

```cs
using (var compositDisposable = new CompositeDisposable())
{
    DisposableA disposableA = new DisposableA();
    compositDisposable.Add(disposableA);

    DisposableB disposableB = new DisposableB().WithAddTo(compositDisposable);


    observable
        .Subscribe(v => Console.WriteLine(v))
        .WithAddTo(compositDisposable);
} // Disposed: disposableA, disposableB, subscriber of observable
```
