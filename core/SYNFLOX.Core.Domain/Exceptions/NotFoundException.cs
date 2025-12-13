namespace Domain.Exceptions
{
    /// <summary>
    /// Represents errors when a requested entity is not found.
    /// </summary>
    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message)
        {
        }
    }
}
