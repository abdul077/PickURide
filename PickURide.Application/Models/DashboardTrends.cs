using System;
using System.Collections.Generic;

namespace PickURide.Application.Models
{
    public class DashboardTrends
    {
        public List<DailyMetric> DailyMetrics { get; set; } = new List<DailyMetric>();
        public List<MonthlyMetric> MonthlyMetrics { get; set; } = new List<MonthlyMetric>();
        public RideStatusBreakdown StatusBreakdown { get; set; } = new RideStatusBreakdown();
        public TransactionStatusBreakdown TransactionStatusBreakdown { get; set; } = new TransactionStatusBreakdown();
    }

    public class DailyMetric
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public int RideCount { get; set; }
        public decimal Commission { get; set; }
    }

    public class MonthlyMetric
    {
        public string Month { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int CompletedRides { get; set; }
    }

    public class RideStatusBreakdown
    {
        public int Completed { get; set; }
        public int InProgress { get; set; }
        public int Waiting { get; set; }
        public int Cancelled { get; set; }
    }

    public class TransactionStatusBreakdown
    {
        public List<TransactionStatusMetric> CompletedTransactions { get; set; } = new List<TransactionStatusMetric>();
        public List<TransactionStatusMetric> HeldTransactions { get; set; } = new List<TransactionStatusMetric>();
        public List<TransactionStatusMetric> CancelledTransactions { get; set; } = new List<TransactionStatusMetric>();
    }

    public class TransactionStatusMetric
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public int TransactionCount { get; set; }
    }

    public class TrendRequest
    {
        public int? Days { get; set; }
    }
}

