using HackatonFiap.Donations.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HackatonFiap.Donations.Infrastructure.Persistence.Configurations;

public sealed class CampaignConfiguration : IEntityTypeConfiguration<Campaign>
{
    public void Configure(EntityTypeBuilder<Campaign> builder)
    {
        builder.ToTable("Campaigns");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Title).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(2000);
        builder.Property(c => c.Goal).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(c => c.AmountRaised).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(c => c.Status).HasConversion<int>().IsRequired();
        builder.Property(c => c.CompletionReason).HasConversion<int?>();
        builder.Property(c => c.CreatedById).IsRequired();
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.UpdatedAt).IsRequired();

        builder.OwnsOne(c => c.Period, p =>
        {
            p.Property(x => x.StartDate).HasColumnName("StartDate").IsRequired();
            p.Property(x => x.EndDate).HasColumnName("EndDate").IsRequired();
        });
        builder.Navigation(c => c.Period).IsRequired();

        builder.HasIndex(c => c.Status);
    }
}
