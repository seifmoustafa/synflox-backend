using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Responses
{
    public class PaginationDto(int totalItemCount, int pageSize, int currentPage)
    {
        public int ItemsCount { get; set; } = totalItemCount;
        public int PagesCount => (int)Math.Ceiling((double)ItemsCount / PageSize);
        public int PageSize { get; set; } = pageSize;
        public int CurrentPage { get; set; } = currentPage;

        //TODO: Implement the Uri Structrue or let it to frontend
        public Uri? FirstPage { get; set; }
        public Uri? LastPage { get; set; }
        public Uri? NextPage { get; set; }
        public Uri? PreviousPage { get; set; }
        public bool HasPrevious => CurrentPage > 1;
        public bool HasNext => CurrentPage < PagesCount;
    }
}
