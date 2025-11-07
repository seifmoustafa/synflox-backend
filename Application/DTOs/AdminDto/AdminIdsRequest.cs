using System;
using System.Collections.Generic;

namespace Application.DTOs.Admin;

public class AdminIdsRequest
{
    public List<Guid> AdminIds { get; set; } = new();
}

