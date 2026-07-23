namespace Journal.Services
{
    public class SessionState
    {
        public bool IsAuthenticated { get; private set; }

        public event Action? Changed;

        public void SetAuthenticated(bool value)
        {
            IsAuthenticated = value;
            Changed?.Invoke();
        }
    }
}
