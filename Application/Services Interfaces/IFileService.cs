using Microsoft.AspNetCore.Http;

namespace Application.Services
{
    /// <summary>
    /// Saves uploaded files using a named scheme (e.g. "Image", "Pdf") configured in
    /// <c>appsettings.json</c> and returns the public request path for accessing the file.
    /// </summary>
    public interface IFileService
    {
        /// <summary>
        /// Saves the file under the configured scheme using the optional base file name
        /// (without extension) and returns the public path. If no name is provided,
        /// the uploaded file's name is used.
        /// </summary>
        Task<string> SaveFileAsync(IFormFile file, string schemeName, string? fileNameWithoutExtension = null);

        /// <summary>
        /// Renames an already saved file and returns the new public path.
        /// </summary>
        Task<string> RenameFileAsync(string fileRequestPath, string schemeName, string newFileNameWithoutExtension);

        /// <summary>
        /// Deletes a previously saved file if it exists.
        /// </summary>
        Task DeleteFileAsync(string fileRequestPath, string schemeName);

        /// <summary>
        /// Saves a byte array as a file under the configured scheme and returns the public path.
        /// </summary>
        Task<string> SaveFileFromBytesAsync(byte[] fileData, string fileName, string schemeName);
    }
}
