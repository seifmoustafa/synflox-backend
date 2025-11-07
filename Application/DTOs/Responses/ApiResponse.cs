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
            //public PaginationDto Pagination { get; set; } = new PaginationDto(pagination.ItemsCount, pagination.PageSize, pagination.CurrentPage);
            public PaginationDto? Pagination { get; set; } = null;
        }
}
