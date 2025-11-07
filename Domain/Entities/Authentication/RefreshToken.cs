using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities.Common;


namespace Domain.Entities.Authentication
{
    public class RefreshToken : AuditEntity<int>
    {
        public string Token { get; set; }
        public DateTime Expires { get; set; }
        public bool IsExpired => DateTime.UtcNow >= Expires;

        //public bool IsValid {  get; set; }

        public Guid? AdminId { get; set; }

        public virtual Admin? Admin { get; set; }
    }
}
