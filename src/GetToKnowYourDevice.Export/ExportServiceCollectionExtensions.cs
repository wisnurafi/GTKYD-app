using GetToKnowYourDevice.Core.Export;
using GetToKnowYourDevice.Export.Exporters;
using Microsoft.Extensions.DependencyInjection;

namespace GetToKnowYourDevice.Export;

public static class ExportServiceCollectionExtensions
{
    /// <summary>Registers JSON, CSV, and PDF exporters plus the export service facade.</summary>
    public static IServiceCollection AddReportExport(this IServiceCollection services)
    {
        services.AddSingleton<IReportExporter, JsonReportExporter>();
        services.AddSingleton<IReportExporter, CsvReportExporter>();
        services.AddSingleton<IReportExporter, PdfReportExporter>();
        services.AddSingleton<IReportExportService, ReportExportService>();
        return services;
    }
}
