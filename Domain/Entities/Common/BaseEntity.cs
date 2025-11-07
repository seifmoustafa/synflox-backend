using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Common
{
    public class BaseEntity<TKey> : IBaseEntity where TKey : struct
    {
        public TKey Id { get; set; }

        public bool IsDeleted { get; set; } = false;
        public bool IsActive { get; set; } = true;

        [StringLength(100)]
        public string? Notes { get; set; }
    }
}
