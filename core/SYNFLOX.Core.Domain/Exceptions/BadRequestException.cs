namespace Domain.Exceptions
{
    /// <summary>
    /// Represents errors that result from invalid user input.
    /// </summary>
    public class BadRequestException : Exception
    {
        public BadRequestException(string message) : base(message)
        {
        }
    }
}
