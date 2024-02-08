namespace Benutomo.DisposableUtil;

public static class Disposable
{
    /// <summary>
    /// 何もしないダミー用<see cref="IDisposable"/>。
    /// </summary>
    public static IDisposable Empty { get; } = new EmptyDisposable();

    /// <summary>
    /// <see cref="IDisposable.Dispose"/>で<paramref name="disposeAction"/>を呼び出す<see cref="IDisposable"/>を作成する。
    /// </summary>
    /// <param name="disposeAction"><see cref="IDisposable.Dispose"/>で呼びされる処理</param>
    /// <returns><see cref="IDisposable"/></returns>
    public static IDisposable Create(Action disposeAction)
    {
        return new ActionDisposable(disposeAction);
    }

    /// <summary>
    /// usingブロックまたはusing宣言用にその場でクリーンアップ処理を呼び出すref structのDisposableを作成する。
    /// </summary>
    /// <typeparam name="T">クリーンアップ処理のパラメータの型</typeparam>
    /// <param name="disposeAction">クリーンアップ処理</param>
    /// <param name="actionArg">クリーンアップ処理のパラメータ</param>
    /// <returns>クリーンアップ処理を呼び出すref structのDisposable</returns>
    public static RefStructDisposable<T> UsingWithCleanup<T>(Action<T> disposeAction, T actionArg)
    {
        return new RefStructDisposable<T>(disposeAction, actionArg);
    }

    private class EmptyDisposable : IDisposable
    {
        public void Dispose() { }
    }

    private class ActionDisposable : IDisposable
    {
        public Action? _disposeAction;

        public ActionDisposable(Action? disposeAction)
        {
            _disposeAction = disposeAction;
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref _disposeAction, null)?.Invoke();
        }
    }

    /// <summary>
    /// クリーンアップ処理を呼び出すためのref struct
    /// </summary>
    /// <typeparam name="T">クリーンアップ処理のパラメータの型</typeparam>
    public ref struct RefStructDisposable<T>
    {
        public Action<T>? _disposeAction;
        public T _actionArg;

        internal RefStructDisposable(Action<T> disposeAction, T actionArg)
        {
            _disposeAction = disposeAction ?? throw new ArgumentNullException(nameof(disposeAction));
            _actionArg = actionArg;
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref _disposeAction, null)?.Invoke(_actionArg);
        }
    }
}
