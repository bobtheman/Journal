namespace Journal.Services.Interfaces
{
    public interface ISessionState
    {
        bool IsAuthenticated { get; }

        event Action? Changed;

        void SetAuthenticated(bool value);
    }
}
