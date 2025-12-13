using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Common
{
    public interface IBaseEntity
    {
        bool IsActive { get; set; }
        bool IsDeleted { get; set; }
    }
}
