namespace Benutomo.DisposableUtil;

public static class CompositeDisposableExtensions
{
    public static T WithAddTo<T>(this T disposable, CompositeDisposable compositeDisposable) where T : IDisposable
    {
        compositeDisposable.Add(disposable);
        return disposable;
    }
}
