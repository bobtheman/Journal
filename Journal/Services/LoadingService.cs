using Journal.Services.Interfaces;

namespace Journal.Services
{
    // Ref-counted global busy indicator so overlapping async operations (e.g. opening one
    // dialog while a background task is still finishing) don't clear the spinner too early.
    public class LoadingService : ILoadingService
    {
        private int _count;

        public bool IsLoading => _count > 0;

        public event Action? Changed;

        public IDisposable BeginLoading()
        {
            Interlocked.Increment(ref _count);
            Changed?.Invoke();
            return new LoadingScope(this);
        }

        private void EndLoading()
        {
            if (Interlocked.Decrement(ref _count) < 0)
            {
                Interlocked.Exchange(ref _count, 0);
            }

            Changed?.Invoke();
        }

        private sealed class LoadingScope : IDisposable
        {
            private readonly LoadingService _owner;
            private bool _disposed;

            public LoadingScope(LoadingService owner)
            {
                _owner = owner;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _owner.EndLoading();
            }
        }
    }
}
