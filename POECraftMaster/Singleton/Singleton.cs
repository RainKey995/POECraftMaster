namespace POECraftMaster.Singleton;

public interface ISingleton : IDisposable
{
    void Register();

    void Destroy();

    bool IsDisposed();
}

public abstract class Singleton<T> : ISingleton where T : Singleton<T>, new()
{
    private static readonly object SyncRoot = new();
    private static T? _instance;
    private bool _isDisposed;

    public static T Instance
    {
        get
        {
            lock (SyncRoot)
                return _instance ??= new T();
        }
    }

    void ISingleton.Register()
    {
        lock (SyncRoot)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(typeof(T).Name);
            if (_instance != null)
                throw new InvalidOperationException($"Singleton already registered: {typeof(T).Name}");
            _instance = (T)this;
        }
    }

    void ISingleton.Destroy()
    {
        lock (SyncRoot)
        {
            if (_isDisposed)
                return;
            _isDisposed = true;
            if (ReferenceEquals(_instance, this))
                _instance = null;
        }
        Dispose();
    }

    bool ISingleton.IsDisposed()
    {
        lock (SyncRoot)
            return _isDisposed;
    }

    public virtual void Dispose()
    {
        // TODO release managed resources here
    }
}
