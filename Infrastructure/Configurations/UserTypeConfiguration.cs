using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities.Authentication;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Configurations
{
    public class UserTypeConfiguration : IEntityTypeConfiguration<AdminType>
    {
        public void Configure(EntityTypeBuilder<AdminType> builder)
        {
            builder.ToTable("AdmUserTypes");
        }
    }
}
