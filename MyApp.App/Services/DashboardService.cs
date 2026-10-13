using Microsoft.EntityFrameworkCore;
using MyApp.App.Data;
using MyApp.App.Models;

namespace MyApp.App.Services
{
    /// <summary>
    /// Builds the dashboard data set. All dashboard database access and calculations
    /// live in this service; the view and the view model never query the database.
    /// Data is read through the existing AccountService/TransactionService where possible.
    /// </summary>
    public class DashboardService
    {
        /// <summary>Number of transactions listed in the "Recent Transactions" panel.</summary>
        public const int RecentTransactionCount = 5;

        /// <summary>Number of reminders listed in the "Upcoming Reminders" panel.</summary>
        public const int UpcomingReminderCount = 5;

        /// <summary>Number of calendar months (including the current month) shown in the expense chart.</summary>
        public const int ExpenseChartMonthCount = 6;

        private const string IncomeTransactionType = "Income";
        private const string ExpenseTransactionType = "Expense";

        private readonly AppDbContext _context;
        private readonly AccountService _accountService;
        private readonly TransactionService _transactionService;

        public DashboardService(
            AppDbContext context,
            AccountService accountService,
            TransactionService transactionService)
        {
            _context = context;
            _accountService = accountService;
            _transactionService = transactionService;
        }

        /// <summary>
        /// Calculates every value displayed on the dashboard for the given date (defaults to today).
        /// </summary>
        public async Task<DashboardSnapshot> GetDashboardSnapshotAsync(DateTime? asOfDate = null)
        {
            var asOf = (asOfDate ?? DateTime.Today).Date;
            var currentMonthStart = new DateTime(asOf.Year, asOf.Month, 1);
            var currentMonthEnd = currentMonthStart.AddMonths(1).AddDays(-1);
            var chartStart = currentMonthStart.AddMonths(-(ExpenseChartMonthCount - 1));

            // One read of the current month is enough for both income and expense totals.
            // Transfers are excluded because only "Income"/"Expense" rows are summed.
            var currentMonthTransactions = await _transactionService.GetTransactionsAsync(currentMonthStart, currentMonthEnd);

            return new DashboardSnapshot
            {
                AsOfDate = asOf,
                TotalAccountBalance = await GetTotalAccountBalanceAsync(),
                CurrentMonthIncome = SumAmounts(currentMonthTransactions, IncomeTransactionType),
                CurrentMonthExpenses = SumAmounts(currentMonthTransactions, ExpenseTransactionType),
                RecentTransactions = await GetRecentTransactionsAsync(),
                UpcomingReminders = await GetUpcomingRemindersAsync(asOf),
                OverdueReminders = await GetOverdueRemindersAsync(asOf),
                Vehicle = await GetVehicleSummaryAsync(),
                MonthlyExpenses = await GetMonthlyExpensesAsync(chartStart, currentMonthEnd)
            };
        }

        /// <summary>Sum of the calculated current balances of all active accounts.</summary>
        private async Task<decimal> GetTotalAccountBalanceAsync()
        {
            var accounts = await _accountService.GetAllAccountsAsync();

            decimal total = 0m;
            foreach (var account in accounts.Where(a => a.IsActive))
            {
                // Reuses the same balance rules as the Accounts module
                // (opening balance, income, expenses and transfer in/out).
                total += await _accountService.CalculateCurrentBalanceAsync(account.Id);
            }

            return total;
        }

        private async Task<List<Transaction>> GetRecentTransactionsAsync()
        {
            return await _transactionService.GetTransactionsAsync(limit: RecentTransactionCount);
        }

        /// <summary>The five nearest outstanding reminders (today or later), soonest first.</summary>
        private async Task<List<Reminder>> GetUpcomingRemindersAsync(DateTime asOf)
        {
            return await _context.Reminders
                .Where(r => !r.IsCompleted && r.ReminderDate >= asOf)
                .OrderBy(r => r.ReminderDate)
                .ThenBy(r => r.Id)
                .Take(UpcomingReminderCount)
                .ToListAsync();
        }

        /// <summary>Outstanding reminders whose date has passed, most overdue first.</summary>
        private async Task<List<Reminder>> GetOverdueRemindersAsync(DateTime asOf)
        {
            return await _context.Reminders
                .Where(r => !r.IsCompleted && r.ReminderDate < asOf)
                .OrderBy(r => r.ReminderDate)
                .ThenBy(r => r.Id)
                .ToListAsync();
        }

        /// <summary>Summary of the active vehicle, or null when no vehicle is registered.</summary>
        private async Task<VehicleSummary?> GetVehicleSummaryAsync()
        {
            var vehicle = await _context.Vehicles
                .Where(v => v.IsActive)
                .OrderBy(v => v.Id)
                .FirstOrDefaultAsync();

            if (vehicle == null)
                return null;

            var lastMaintenanceDate = await _context.MaintenanceRecords
                .Where(m => m.VehicleId == vehicle.Id)
                .OrderByDescending(m => m.ServiceDate)
                .Select(m => (DateTime?)m.ServiceDate)
                .FirstOrDefaultAsync();

            return new VehicleSummary
            {
                VehicleName = BuildVehicleName(vehicle),
                RegistrationNo = vehicle.RegistrationNo,
                CurrentMileage = vehicle.CurrentMileage,
                LastMaintenanceDate = lastMaintenanceDate
            };
        }

        /// <summary>Expense total per calendar month for the last six months, oldest first.</summary>
        private async Task<List<MonthlyExpenseSummary>> GetMonthlyExpensesAsync(DateTime chartStart, DateTime chartEnd)
        {
            var expenses = await _transactionService.GetTransactionsAsync(
                chartStart,
                chartEnd,
                transactionType: ExpenseTransactionType);

            var totalsPerMonth = expenses
                .GroupBy(t => new { t.TransactionDate.Year, t.TransactionDate.Month })
                .ToDictionary(g => (g.Key.Year, g.Key.Month), g => g.Sum(t => t.Amount));

            var monthlyExpenses = new List<MonthlyExpenseSummary>(ExpenseChartMonthCount);
            for (var monthOffset = 0; monthOffset < ExpenseChartMonthCount; monthOffset++)
            {
                var month = chartStart.AddMonths(monthOffset);
                totalsPerMonth.TryGetValue((month.Year, month.Month), out var totalExpense);

                monthlyExpenses.Add(new MonthlyExpenseSummary
                {
                    Month = month,
                    TotalExpense = totalExpense
                });
            }

            return monthlyExpenses;
        }

        private static decimal SumAmounts(IEnumerable<Transaction> transactions, string transactionType)
        {
            return transactions
                .Where(t => t.TransactionType == transactionType)
                .Sum(t => t.Amount);
        }

        private static string BuildVehicleName(Vehicle vehicle)
        {
            var name = $"{vehicle.Make} {vehicle.Model}".Trim();
            return string.IsNullOrWhiteSpace(name) ? "Vehicle" : name;
        }
    }
}
