using System;

namespace Domain.Exceptions;

public class PlanTrialNotAllowedException : Exception
{
    public PlanTrialNotAllowedException(string message) : base(message)
    {
    }
}
