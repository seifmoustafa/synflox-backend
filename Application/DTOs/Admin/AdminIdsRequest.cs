using System;
using System.Collections.Generic;

namespace Application.DTOs.Admin;

public class AdminIdsRequest
{
    public IEnumerable<Guid> AdminIds { get; set; } = new List<Guid>();
}
