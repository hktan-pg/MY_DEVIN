using System.Globalization;
using MyApp.App.Models;

namespace MyApp.App.Services
{
    /// <summary>
    /// Read-only, aggregated data required to render the dashboard.
    /// Produced by <see cref="DashboardService"/> and consumed by DashboardViewModel.
    /// </summary>
    public class DashboardSnapshot
    {
        /// <summary>Date the snapshot was calculated for (start of day).</summary>
        public DateTime AsOfDate { get; set; } = DateTime.Today;

        /// <summary>Sum of the calculated current balances of all active accounts.</summary>
        public decimal TotalAccountBalance { get; set; }

        /// <summary>Sum of Income transactions for the current calendar month.</summary>
        public decimal CurrentMonthIncome { get; set; }

        /// <summary>Sum of Expense transactions for the current calendar month.</summary>
        public decimal CurrentMonthExpenses { get; set; }

        /// <summary>Income minus expenses for the current month (transfers never affect this).</summary>
        public decimal NetCashFlow => CurrentMonthIncome - CurrentMonthExpenses;

        /// <summary>The most recent transactions (newest first).</summary>
        public List<Transaction> RecentTransactions { get; set; } = new List<Transaction>();

        /// <summary>The five nearest outstanding reminders (soonest first).</summary>
        public List<Reminder> UpcomingReminders { get; set; } = new List<Reminder>();

        /// <summary>Outstanding reminders whose date has passed, most overdue first.</summary>
        public List<Reminder> OverdueReminders { get; set; } = new List<Reminder>();

        /// <summary>Summary of the active vehicle, or null when none is registered.</summary>
        public VehicleSummary? Vehicle { get; set; }

        /// <summary>Expense totals for the last six calendar months (oldest first).</summary>
        public List<MonthlyExpenseSummary> MonthlyExpenses { get; set; } = new List<MonthlyExpenseSummary>();
    }

    /// <summary>Compact vehicle information shown on the dashboard.</summary>
    public class VehicleSummary
    {
        public string VehicleName { get; set; } = string.Empty;

        public string? RegistrationNo { get; set; }

        public int CurrentMileage { get; set; }

        public DateTime? LastMaintenanceDate { get; set; }
    }

    /// <summary>One bar of the monthly expense chart.</summary>
    public class MonthlyExpenseSummary
    {
        /// <summary>First day of the month this value belongs to.</summary>
        public DateTime Month { get; set; }

        public decimal TotalExpense { get; set; }

        /// <summary>Short axis label, e.g. "Sep 2026".</summary>
        public string MonthLabel => Month.ToString("MMM yyyy", CultureInfo.InvariantCulture);
    }
}
