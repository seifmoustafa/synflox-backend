using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Licensing;

namespace Application.Services;

/// <summary>
/// Service interface for importing data from various formats.
/// </summary>
public interface IImportService
{
    /// <summary>
    /// Imports companies from CSV format.
    /// </summary>
    Task<ImportResult<CreateCompanyDto>> ImportCompaniesFromCsvAsync(byte[] fileData);

    /// <summary>
    /// Imports companies from Excel format.
    /// </summary>
    Task<ImportResult<CreateCompanyDto>> ImportCompaniesFromExcelAsync(byte[] fileData);
}

/// <summary>
/// Result of an import operation.
/// </summary>
public class ImportResult<T>
{
    public List<T> ValidItems { get; set; } = new();
    public List<ImportError> Errors { get; set; } = new();
    public int TotalRows { get; set; }
    public int SuccessCount => ValidItems.Count;
    public int ErrorCount => Errors.Count;
}

/// <summary>
/// Represents an error during import.
/// </summary>
public class ImportError
{
    public int RowNumber { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}



