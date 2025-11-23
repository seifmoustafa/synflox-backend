using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.ModuleDto;

public class UpdateModuleDto
{
    [StringLength(200, MinimumLength = 2)]
    public string? Name { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }

    [MaxLength(200)]
    public List<string>? Features { get; set; }
}
