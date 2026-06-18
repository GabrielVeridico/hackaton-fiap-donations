using HackatonFiap.Donations.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HackatonFiap.Donations.Infrastructure.Persistence.Configurations;

public sealed class DonationConfiguration : IEntityTypeConfiguration<Donation>
{
    public void Configure(EntityTypeBuilder<Donation> builder)
    {
        builder.ToTable("Donations");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.CampaignId).IsRequired();
        builder.Property(d => d.Amount).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(d => d.Method).HasConversion<int>().IsRequired();
        builder.Property(d => d.Status).HasConversion<int>().IsRequired();
        builder.Property(d => d.DonorId).IsRequired();
        builder.Property(d => d.DonorEmail).HasMaxLength(256).IsRequired();
        builder.Property(d => d.DonorName).HasMaxLength(256).IsRequired();
        builder.Property(d => d.DeclineReason).HasMaxLength(512);
        builder.Property(d => d.CreatedAt).IsRequired();

        builder.HasIndex(d => d.CampaignId);
        builder.HasIndex(d => d.DonorId);
    }
}
