using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities.Common;


namespace Application.DTOs.Responses
{
    public class ApiResponse<T>(int statusCode, string message, T? data = default,
        IEnumerable<string>? errors = null, PaginationMetadata? pagination = null)
    {
        public int StatusCode { get; set; } = statusCode;
        public string Message { get; set; } = message;
        public T Data { get; set; } = data;
        public IEnumerable<string> Errors { get; set; } = errors ?? [];
        public PaginationDto? Pagination { get; set; } = null;

        /// <summary>
        /// Creates a successful API response
        /// </summary>
        public static ApiResponse<T> Success(T data, string message = "Success")
        {
            return new ApiResponse<T>(200, message, data);
        }

        /// <summary>
        /// Creates an error API response
        /// </summary>
        public static ApiResponse<T> Error(string message, int statusCode = 400)
        {
            return new ApiResponse<T>(statusCode, message, default(T), new[] { message });
        }

        /// <summary>
        /// Creates an error API response with data (for detailed error info)
        /// </summary>
        public static ApiResponse<T> Error(string message, int statusCode, T data)
        {
            return new ApiResponse<T>(statusCode, message, data, new[] { message });
        }
    }
}
