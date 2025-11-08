using Domain.Entities.Licensing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class CompanyGroupMemberConfiguration : IEntityTypeConfiguration<CompanyGroupMember>
{
    public void Configure(EntityTypeBuilder<CompanyGroupMember> builder)
    {
        // Composite unique index for company-group relationship
        builder.HasIndex(m => new { m.CompanyId, m.CompanyGroupId, m.IsDeleted })
            .HasDatabaseName("IX_CompanyGroupMembers_Company_Group_Deleted")
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        // Foreign keys
        builder.HasOne(m => m.Company)
            .WithMany()
            .HasForeignKey(m => m.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.CompanyGroup)
            .WithMany()
            .HasForeignKey(m => m.CompanyGroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}



