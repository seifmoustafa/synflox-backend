using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Application.DTOs.User;

public class UsersIdsRequest
{
    [Required]
    public IEnumerable<Guid> UsersIds { get; set; } = Enumerable.Empty<Guid>();
}
