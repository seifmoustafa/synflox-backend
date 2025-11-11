using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs.Company;
using Application.Services;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Infrastructure.Services;

public class ImportService : IImportService
{
    public Task<ImportResult<CreateCompanyDto>> ImportCompaniesFromCsvAsync(byte[] fileData)
    {
        var result = new ImportResult<CreateCompanyDto>();
        var lines = Encoding.UTF8.GetString(fileData).Split('\n');
        
        if (lines.Length < 2)
        {
            result.Errors.Add(new ImportError { RowNumber = 0, ErrorMessage = "File is empty or missing header row" });
            return Task.FromResult(result);
        }

        // Skip header row
        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            result.TotalRows++;
            var columns = ParseCsvLine(line);

            try
            {
                if (columns.Length < 2)
                {
                    result.Errors.Add(new ImportError { RowNumber = i + 1, ErrorMessage = "Insufficient columns" });
                    continue;
                }

                var dto = new CreateCompanyDto
                {
                    Name = columns.Length > 1 ? columns[1].Trim() : "",
                    ContactEmail = columns.Length > 4 ? columns[4].Trim() : null,
                    ContactPhone = columns.Length > 5 ? columns[5].Trim() : null,
                    Address = columns.Length > 6 ? columns[6].Trim() : null
                };

                // Validate required fields
                if (string.IsNullOrWhiteSpace(dto.Name))
                {
                    result.Errors.Add(new ImportError { RowNumber = i + 1, FieldName = "Name", ErrorMessage = "Name is required" });
                    continue;
                }

                result.ValidItems.Add(dto);
            }
            catch (Exception ex)
            {
                result.Errors.Add(new ImportError { RowNumber = i + 1, ErrorMessage = ex.Message });
            }
        }

        return Task.FromResult(result);
    }

    public Task<ImportResult<CreateCompanyDto>> ImportCompaniesFromExcelAsync(byte[] fileData)
    {
        var result = new ImportResult<CreateCompanyDto>();

        try
        {
            using var stream = new MemoryStream(fileData);
            using var spreadsheet = SpreadsheetDocument.Open(stream, false);
            
            var workbookPart = spreadsheet.WorkbookPart;
            if (workbookPart == null)
            {
                result.Errors.Add(new ImportError { RowNumber = 0, ErrorMessage = "Invalid Excel file" });
                return Task.FromResult(result);
            }

            var worksheetPart = workbookPart.WorksheetParts.FirstOrDefault();
            if (worksheetPart == null)
            {
                result.Errors.Add(new ImportError { RowNumber = 0, ErrorMessage = "No worksheets found in file" });
                return Task.FromResult(result);
            }

            var sheetData = worksheetPart.Worksheet.Elements<SheetData>().FirstOrDefault();
            if (sheetData == null)
            {
                result.Errors.Add(new ImportError { RowNumber = 0, ErrorMessage = "No data found in worksheet" });
                return Task.FromResult(result);
            }

            var rows = sheetData.Elements<Row>().ToList();
            if (rows.Count < 2)
            {
                result.Errors.Add(new ImportError { RowNumber = 0, ErrorMessage = "File is empty or missing header row" });
                return Task.FromResult(result);
            }

            // Skip header row (first row)
            for (int i = 1; i < rows.Count; i++)
            {
                result.TotalRows++;
                var row = rows[i];
                var cells = row.Elements<Cell>().ToList();

                try
                {
                    // Column 2 (B) is Name (0-indexed: cells[1])
                    var nameCell = cells.Count > 1 ? GetCellValue(cells[1], workbookPart) : null;
                    if (string.IsNullOrWhiteSpace(nameCell))
                    {
                        result.Errors.Add(new ImportError { RowNumber = i + 1, FieldName = "Name", ErrorMessage = "Name is required" });
                        continue;
                    }

                    var dto = new CreateCompanyDto
                    {
                        Name = nameCell.Trim(),
                        ContactEmail = cells.Count > 4 ? GetCellValue(cells[4], workbookPart)?.Trim() : null,
                        ContactPhone = cells.Count > 5 ? GetCellValue(cells[5], workbookPart)?.Trim() : null,
                        Address = cells.Count > 6 ? GetCellValue(cells[6], workbookPart)?.Trim() : null
                    };

                    result.ValidItems.Add(dto);
                }
                catch (Exception ex)
                {
                    result.Errors.Add(new ImportError { RowNumber = i + 1, ErrorMessage = ex.Message });
                }
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add(new ImportError { RowNumber = 0, ErrorMessage = $"Error reading Excel file: {ex.Message}" });
        }

        return Task.FromResult(result);
    }

    private string? GetCellValue(Cell cell, WorkbookPart workbookPart)
    {
        if (cell.CellValue == null)
            return null;

        var value = cell.CellValue.Text;
        
        // If cell has a data type and it's a shared string, get the actual string
        if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
        {
            var sharedStringTable = workbookPart.SharedStringTablePart?.SharedStringTable;
            if (sharedStringTable != null && int.TryParse(value, out var index))
            {
                var sharedStringItem = sharedStringTable.Elements<SharedStringItem>().ElementAt(index);
                if (sharedStringItem != null)
                {
                    value = sharedStringItem.Text?.Text ?? value;
                }
            }
        }

        return value;
    }

    private string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++; // Skip next quote
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        result.Add(current.ToString());
        return result.ToArray();
    }
}
