using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs.Reporting;
using Application.DTOs.Responses;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace WebAPI.Controllers;

/// <summary>
/// Controller for generating and managing reports.
/// </summary>
[ApiController]
[Route("api/reports")]
[Authorize(Policy = "SuperAdminOnly")]
public class ReportController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly IExportService _exportService;
    private readonly ILocalizationService _localizer;

    public ReportController(
        IReportService reportService,
        IExportService exportService,
        ILocalizationService localizer)
    {
        _reportService = reportService;
        _exportService = exportService;
        _localizer = localizer;
    }

    /// <summary>
    /// Gets all available reports.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAvailableReports()
    {
        try
        {
            var reports = await _reportService.GetAvailableReportsAsync();
            return Ok(new ApiResponse<object>(200, string.Empty, new { reports }));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Generates a report by report type.
    /// </summary>
    [HttpPost("{reportType}/generate")]
    public async Task<IActionResult> GenerateReport(
        string reportType,
        [FromBody] Dictionary<string, object>? parameters = null)
    {
        try
        {
            var result = await _reportService.GeneratePreBuiltReportAsync(reportType, parameters);
            return Ok(new ApiResponse<ReportResultDto>(200, _localizer["Report.Generated"], result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Downloads a report in CSV or Excel format.
    /// </summary>
    [HttpGet("{reportType}/download")]
    public async Task<IActionResult> DownloadReport(
        string reportType,
        [FromQuery] string format = "csv",
        [FromQuery] Dictionary<string, object>? parameters = null)
    {
        try
        {
            var result = await _reportService.GeneratePreBuiltReportAsync(reportType, parameters);

            byte[] fileData;
            string contentType;
            string fileExtension;

            if (format.ToLowerInvariant() == "excel")
            {
                // Convert report data to Excel using Open XML SDK
                using var stream = new MemoryStream();
                using var spreadsheet = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook);
                
                var workbookPart = spreadsheet.AddWorkbookPart();
                workbookPart.Workbook = new Workbook();
                
                var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                worksheetPart.Worksheet = new Worksheet(new SheetData());
                
                var sheets = spreadsheet.WorkbookPart!.Workbook.AppendChild(new Sheets());
                var sheet = new Sheet { Id = spreadsheet.WorkbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "Report" };
                sheets.Append(sheet);
                
                var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>()!;
                
                // Headers
                if (result.Data.Any())
                {
                    var headers = result.Data.First().Keys.ToList();
                    var headerRow = new Row { RowIndex = 1 };
                    for (int i = 0; i < headers.Count; i++)
                    {
                        var cell = new Cell
                        {
                            DataType = CellValues.String,
                            CellValue = new CellValue(headers[i])
                        };
                        headerRow.Append(cell);
                    }
                    sheetData.Append(headerRow);

                    // Data
                    uint rowIndex = 2;
                    foreach (var rowData in result.Data)
                    {
                        var row = new Row { RowIndex = rowIndex++ };
                        foreach (var header in headers)
                        {
                            var value = rowData.ContainsKey(header) ? rowData[header]?.ToString() ?? "" : "";
                            var cell = new Cell
                            {
                                DataType = CellValues.String,
                                CellValue = new CellValue(value)
                            };
                            row.Append(cell);
                        }
                        sheetData.Append(row);
                    }
                }
                
                workbookPart.Workbook.Save();
                
                fileData = stream.ToArray();
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                fileExtension = "xlsx";
            }
            else
            {
                // Convert to CSV
                var csv = new System.Text.StringBuilder();
                if (result.Data.Any())
                {
                    var headers = result.Data.First().Keys.ToList();
                    csv.AppendLine(string.Join(",", headers));

                    foreach (var rowData in result.Data)
                    {
                        var values = headers.Select(h => 
                            rowData.ContainsKey(h) 
                                ? $"\"{(rowData[h]?.ToString() ?? "").Replace("\"", "\"\"")}\"" 
                                : "\"\"");
                        csv.AppendLine(string.Join(",", values));
                    }
                }

                fileData = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
                contentType = "text/csv";
                fileExtension = "csv";
            }

            var fileName = $"{reportType}_{DateTime.UtcNow:yyyyMMddHHmmss}.{fileExtension}";
            return File(fileData, contentType, fileName);
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }
}
