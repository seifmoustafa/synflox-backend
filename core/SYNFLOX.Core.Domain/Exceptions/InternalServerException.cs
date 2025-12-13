namespace Domain.Exceptions
{
    /// <summary>
    /// Represents internal server errors that occur during processing
    /// </summary>
    public class InternalServerException : Exception
    {
        public InternalServerException(string message) : base(message)
        {
        }

        public InternalServerException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
