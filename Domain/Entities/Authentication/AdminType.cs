using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities.Common;

namespace Domain.Entities.Authentication
{
    public class AdminType : BaseEntity<Guid>
    {
        [Required]
        [StringLength(100)]
        public required string AdminTypeName { get; set; }

        public ICollection<Admin> Admins { get; set; }
    }
}
