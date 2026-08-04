namespace Journal.Exceptions
{
    public class IncompatibleBackupException : Exception
    {
        public IncompatibleBackupException(string message) : base(message)
        {
        }
    }
}
