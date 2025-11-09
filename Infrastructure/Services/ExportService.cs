using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs.Licensing;
using Application.Services;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Infrastructure.Services;

public class ExportService : IExportService
{
    public Task<byte[]> ExportCompaniesToCsvAsync(IEnumerable<CompanyDto> companies)
    {
        var csv = new StringBuilder();
        
        // Header row
        csv.AppendLine("Id,Name,IsActive,ExpiryDate,ContactEmail,ContactPhone,Address,IsTrial,TrialEndDate,SubscriptionPlanId,CreatedTimestamp,UpdatedTimestamp");

        // Data rows
        foreach (var company in companies)
        {
            csv.AppendLine($"{company.Id}," +
                          $"\"{company.Name.Replace("\"", "\"\"")}\"," +
                          $"{company.IsActive}," +
                          $"{(company.ExpiryDate?.ToString("yyyy-MM-dd") ?? "")}," +
                          $"\"{company.ContactEmail?.Replace("\"", "\"\"") ?? ""}\"," +
                          $"\"{company.ContactPhone?.Replace("\"", "\"\"") ?? ""}\"," +
                          $"\"{company.Address?.Replace("\"", "\"\"") ?? ""}\"," +
                          $"{company.IsTrial}," +
                          $"{(company.TrialEndDate?.ToString("yyyy-MM-dd") ?? "")}," +
                          $"{(company.SubscriptionPlanId?.ToString() ?? "")}," +
                          $"{company.CreatedTimestamp:yyyy-MM-dd HH:mm:ss}," +
                          $"{(company.UpdatedTimestamp?.ToString("yyyy-MM-dd HH:mm:ss") ?? "")}");
        }

        // Add UTF-8 BOM for Excel compatibility
        var csvBytes = Encoding.UTF8.GetBytes(csv.ToString());
        var bomBytes = Encoding.UTF8.GetPreamble();
        var result = new byte[bomBytes.Length + csvBytes.Length];
        Buffer.BlockCopy(bomBytes, 0, result, 0, bomBytes.Length);
        Buffer.BlockCopy(csvBytes, 0, result, bomBytes.Length, csvBytes.Length);

        return Task.FromResult(result);
    }

    public Task<byte[]> ExportCompaniesToExcelAsync(IEnumerable<CompanyDto> companies)
    {
        using var stream = new MemoryStream();
        using (var spreadsheet = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook, true))
        {
            var workbookPart = spreadsheet.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();
            
            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            worksheetPart.Worksheet = new Worksheet(new SheetData());
            
            var sheets = spreadsheet.WorkbookPart!.Workbook.AppendChild(new Sheets());
            var sheet = new Sheet { Id = spreadsheet.WorkbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "Companies" };
            sheets.Append(sheet);
            
            var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>()!;
            
            // Header row
            var headerRow = new Row { RowIndex = 1 };
            var headers = new[] { "Id", "Name", "IsActive", "ExpiryDate", "ContactEmail", "ContactPhone", "Address", "IsTrial", "TrialEndDate", "SubscriptionPlanId", "CreatedTimestamp", "UpdatedTimestamp" };
            foreach (var header in headers)
            {
                var cell = new Cell { DataType = CellValues.String, CellValue = new CellValue(header) };
                headerRow.Append(cell);
            }
            sheetData.Append(headerRow);
            
            // Data rows
            uint rowIndex = 2;
            foreach (var company in companies)
            {
                var row = new Row { RowIndex = rowIndex++ };
                row.Append(CreateCell(company.Id.ToString()));
                row.Append(CreateCell(company.Name ?? ""));
                row.Append(CreateCell(company.IsActive.ToString()));
                row.Append(CreateCell(company.ExpiryDate?.ToString("yyyy-MM-dd") ?? ""));
                row.Append(CreateCell(company.ContactEmail ?? ""));
                row.Append(CreateCell(company.ContactPhone ?? ""));
                row.Append(CreateCell(company.Address ?? ""));
                row.Append(CreateCell(company.IsTrial.ToString()));
                row.Append(CreateCell(company.TrialEndDate?.ToString("yyyy-MM-dd") ?? ""));
                row.Append(CreateCell(company.SubscriptionPlanId?.ToString() ?? ""));
                row.Append(CreateCell(company.CreatedTimestamp.ToString("yyyy-MM-dd HH:mm:ss")));
                row.Append(CreateCell(company.UpdatedTimestamp?.ToString("yyyy-MM-dd HH:mm:ss") ?? ""));
                sheetData.Append(row);
            }
            
            workbookPart.Workbook.Save();
        }
        
        return Task.FromResult(stream.ToArray());
    }

    public Task<byte[]> ExportSubscriptionHistoryToCsvAsync(IEnumerable<SubscriptionHistoryDto> history)
    {
        var csv = new StringBuilder();
        
        // Header row
        csv.AppendLine("Id,CompanyId,ActionType,ActionTypeName,OldValue,NewValue,PerformedBy,Timestamp,Notes");

        // Data rows
        foreach (var item in history)
        {
            csv.AppendLine($"{item.Id}," +
                          $"{item.CompanyId}," +
                          $"{(int)item.ActionType}," +
                          $"\"{item.ActionTypeName.Replace("\"", "\"\"")}\"," +
                          $"\"{(item.OldValue?.Replace("\"", "\"\"") ?? "")}\"," +
                          $"\"{(item.NewValue?.Replace("\"", "\"\"") ?? "")}\"," +
                          $"{(item.PerformedBy?.ToString() ?? "")}," +
                          $"{item.Timestamp:yyyy-MM-dd HH:mm:ss}," +
                          $"\"{(item.Notes?.Replace("\"", "\"\"") ?? "")}\"");
        }

        return Task.FromResult(Encoding.UTF8.GetBytes(csv.ToString()));
    }

    public Task<byte[]> ExportSubscriptionHistoryToExcelAsync(IEnumerable<SubscriptionHistoryDto> history)
    {
        using var stream = new MemoryStream();
        using (var spreadsheet = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook, true))
        {
            var workbookPart = spreadsheet.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();
            
            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            worksheetPart.Worksheet = new Worksheet(new SheetData());
            
            var sheets = spreadsheet.WorkbookPart!.Workbook.AppendChild(new Sheets());
            var sheet = new Sheet { Id = spreadsheet.WorkbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "Subscription History" };
            sheets.Append(sheet);
            
            var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>()!;
            
            // Header row
            var headerRow = new Row { RowIndex = 1 };
            var headers = new[] { "Id", "CompanyId", "ActionType", "ActionTypeName", "OldValue", "NewValue", "PerformedBy", "Timestamp", "Notes" };
            foreach (var header in headers)
            {
                var cell = new Cell { DataType = CellValues.String, CellValue = new CellValue(header) };
                headerRow.Append(cell);
            }
            sheetData.Append(headerRow);
            
            // Data rows
            uint rowIndex = 2;
            foreach (var item in history)
            {
                var row = new Row { RowIndex = rowIndex++ };
                row.Append(CreateCell(item.Id.ToString()));
                row.Append(CreateCell(item.CompanyId.ToString()));
                row.Append(CreateCell(((int)item.ActionType).ToString()));
                row.Append(CreateCell(item.ActionTypeName ?? ""));
                row.Append(CreateCell(item.OldValue ?? ""));
                row.Append(CreateCell(item.NewValue ?? ""));
                row.Append(CreateCell(item.PerformedBy?.ToString() ?? ""));
                row.Append(CreateCell(item.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")));
                row.Append(CreateCell(item.Notes ?? ""));
                sheetData.Append(row);
            }
            
            workbookPart.Workbook.Save();
        }
        
        return Task.FromResult(stream.ToArray());
    }

    private static Cell CreateCell(string value)
    {
        return new Cell
        {
            DataType = CellValues.String,
            CellValue = new CellValue(value)
        };
    }
}
