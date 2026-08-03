namespace Journal.Services.Interfaces
{
    public interface ILoadingService
    {
        bool IsLoading { get; }

        event Action? Changed;

        // Ref-counted so nested/overlapping operations don't hide the spinner early -
        // dispose the returned handle when the operation finishes.
        IDisposable BeginLoading();
    }
}
