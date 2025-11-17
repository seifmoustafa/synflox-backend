using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Common
{
    public class AuditEntity<TKey> : BaseEntity<TKey> where TKey : struct
    {
        // Timestamps
        public DateTime CreatedTimestamp { get; set; }
        public DateTime? UpdatedTimestamp { get; set; }
        public DateTime? DeletedTimestamp { get; set; }

        // Admin who performed the action (nullable for system-generated or initial data)
        public Guid? CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }
        public Guid? DeletedBy { get; set; }
    }
}
