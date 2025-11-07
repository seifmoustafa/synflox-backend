using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.BaseEntityDto
{
    public class BaseEntityGetDto
    {
        public Guid Id { get; set; } // معرف القسم
        public string Name { get; set; } // اسم القسم
        public bool Active { get; set; }
        public bool IsDeleted { get; set; }
    }
}
