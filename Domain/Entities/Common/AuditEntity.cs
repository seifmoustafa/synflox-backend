using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Common
{
    public class AuditEntity<TKey> : BaseEntity<TKey> where TKey : struct
    {
        public DateTime CreatedTimestamp { get; set; }
        public DateTime? UpdatedTimestamp { get; set; }
        public DateTime? DeletedTimestamp { get; set; }
    }
}
