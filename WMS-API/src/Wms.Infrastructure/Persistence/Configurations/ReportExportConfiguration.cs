using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.Domain.Entities;

namespace Wms.Infrastructure.Persistence.Configurations;

public sealed class ReportExportConfiguration : IEntityTypeConfiguration<ReportExport>
{
  public void Configure(EntityTypeBuilder<ReportExport> builder)
  {
    builder.ToTable("report_exports");

    builder.HasKey(reportExport => reportExport.ReportExportId);

    builder.Property(reportExport => reportExport.ReportExportId)
        .HasColumnName("report_export_id");

    builder.Property(reportExport => reportExport.ReportType)
        .HasColumnName("report_type")
        .IsRequired();

    builder.Property(reportExport => reportExport.Format)
        .HasColumnName("format")
        .IsRequired();

    builder.Property(reportExport => reportExport.GeneratedAt)
        .HasColumnName("generated_at")
        .IsRequired();

    builder.Property(reportExport => reportExport.FilePath)
        .HasColumnName("file_path")
        .HasMaxLength(512)
        .IsRequired();

    builder.OwnsOne(
        reportExport => reportExport.DateRange,
        dateRange =>
        {
          dateRange.Property(value => value.From)
                  .HasColumnName("range_from");

          dateRange.Property(value => value.To)
                  .HasColumnName("range_to");
        });

    builder.Navigation(reportExport => reportExport.DateRange)
        .IsRequired(false);
  }
}
