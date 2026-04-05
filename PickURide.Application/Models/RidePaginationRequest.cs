using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickURide.Application.Models
{
    public class RidePaginationRequest
    {
        public int PageNumber { get; set; } = 1; 
        public int PageSize { get; set; } = 10;
        public string? Search { get; set; }
        public string? SortColumn { get; set; }
        public string? SortDirection { get; set; }
        public string? FilterPeriod { get; set; } // "daily", "weekly", "monthly", "all"
        public string? StatusFilter { get; set; } // "Waiting", "In-Progress", "Completed", "All", etc.
        public bool? IsScheduledFilter { get; set; } // null = all, true = scheduled, false = instant
    }

}
