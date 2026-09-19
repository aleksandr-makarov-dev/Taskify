using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Taskify.WebApi.Domain;

namespace Taskify.WebApi.Persistence.Configurations;

public class ItemEntityConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.ToTable("Items");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.Description)
            .HasMaxLength(5000);

        builder.HasIndex(x => x.IsDeleted);

        builder.HasQueryFilter(QueryFilters.SoftDelete, x => !x.IsDeleted);
    }
}