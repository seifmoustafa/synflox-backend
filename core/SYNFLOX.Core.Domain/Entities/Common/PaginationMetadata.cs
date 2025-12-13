using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Common
{
    public class PaginationMetadata(int totalItemCount, int pageSize, int currentPage)
    {
        public int ItemsCount { get; set; } = totalItemCount;
        // public int PagesCount => (int)Math.Ceiling((double)ItemsCount / PageSize);
        public int PageSize { get; set; } = pageSize > 10 ? 10 : pageSize;
        public int CurrentPage { get; set; } = currentPage < 1 ? 1 : currentPage;

    }
}
