using System;
using System.Collections.Generic;

namespace PickURide.Application.Models
{
    public class PaymentPagedResultDto
    {
        public IEnumerable<PaymentDetailDto> Payments { get; set; } = new List<PaymentDetailDto>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage { get; set; }
        public bool HasNextPage { get; set; }

        // Add summary information for the current page/filter
        public int UniqueRidesCount { get; set; }
        public decimal TotalEarnings { get; set; }
        public decimal DailyEarnings { get; set; }
        public decimal WeeklyEarnings { get; set; }
        public decimal MonthlyEarnings { get; set; }
    }

    public class PaymentFilterDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SearchTerm { get; set; }
        public string? FilterPeriod { get; set; } // "all", "daily", "weekly", "monthly"
        public string? SortColumn { get; set; }
        public string? SortDirection { get; set; } // "asc" or "desc"
    }
}
