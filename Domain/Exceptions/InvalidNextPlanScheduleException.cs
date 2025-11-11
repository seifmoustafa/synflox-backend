using System;

namespace Domain.Exceptions;

public class InvalidNextPlanScheduleException : Exception
{
    public InvalidNextPlanScheduleException(string message) : base(message)
    {
    }
}
