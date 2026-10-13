using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using MyApp.App.Models;
using MyApp.App.Services;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace MyApp.App.ViewModels
{
    /// <summary>
    /// View model of the dashboard. Every value shown comes from <see cref="DashboardService"/>;
    /// this class only exposes that data and builds the chart model for the view.
    /// </summary>
    public class DashboardViewModel : ViewModelBase
    {
        private static readonly OxyColor ChartTextColor = OxyColor.Parse("#B0B0B0");
        private static readonly OxyColor ChartLineColor = OxyColor.Parse("#3E3E42");
        private static readonly OxyColor ChartBarColor = OxyColor.Parse("#E57373");

        /// <summary>Axis keys required to render BarSeries as vertical columns.</summary>
        private const string CategoryAxisKey = "categoryAxisKey";
        private const string ValueAxisKey = "valueAxisKey";

        /// <summary>How many overdue reminders the alert panel lists before it just counts them.</summary>
        private const int OverdueReminderDisplayCount = 5;

        private readonly DashboardService _dashboardService;
        private readonly IServiceProvider _serviceProvider;

        private string _todayDisplay = string.Empty;
        private decimal _totalAccountBalance;
        private decimal _currentMonthIncome;
        private decimal _currentMonthExpenses;
        private ObservableCollection<Transaction> _recentTransactions;
        private ObservableCollection<Reminder> _upcomingReminders;
        private ObservableCollection<Reminder> _overdueReminders;
        private int _overdueReminderCount;
        private VehicleSummary? _vehicle;
        private PlotModel? _expenseChart;

        public DashboardViewModel(DashboardService dashboardService, IServiceProvider serviceProvider)
        {
            _dashboardService = dashboardService;
            _serviceProvider = serviceProvider;

            _recentTransactions = new ObservableCollection<Transaction>();
            _upcomingReminders = new ObservableCollection<Reminder>();
            _overdueReminders = new ObservableCollection<Reminder>();

            RefreshCommand = new RelayCommand(async _ => await RefreshAsync());
            ViewAllTransactionsCommand = new RelayCommand(_ => NavigateToTransactions());
            OpenRemindersCommand = new RelayCommand(_ => NavigateToReminders());
        }

        /// <summary>Today's date shown under the dashboard title.</summary>
        public string TodayDisplay
        {
            get => _todayDisplay;
            private set => SetProperty(ref _todayDisplay, value);
        }

        /// <summary>Total calculated balance of all active accounts.</summary>
        public decimal TotalAccountBalance
        {
            get => _totalAccountBalance;
            private set => SetProperty(ref _totalAccountBalance, value);
        }

        /// <summary>Income for the current calendar month.</summary>
        public decimal CurrentMonthIncome
        {
            get => _currentMonthIncome;
            private set
            {
                if (SetProperty(ref _currentMonthIncome, value))
                {
                    OnPropertyChanged(nameof(NetCashFlow));
                    OnPropertyChanged(nameof(IsNetCashFlowNegative));
                }
            }
        }

        /// <summary>Expenses for the current calendar month.</summary>
        public decimal CurrentMonthExpenses
        {
            get => _currentMonthExpenses;
            private set
            {
                if (SetProperty(ref _currentMonthExpenses, value))
                {
                    OnPropertyChanged(nameof(NetCashFlow));
                    OnPropertyChanged(nameof(IsNetCashFlowNegative));
                }
            }
        }

        /// <summary>Income minus expenses for the current month (transfers excluded).</summary>
        public decimal NetCashFlow => CurrentMonthIncome - CurrentMonthExpenses;

        /// <summary>True when the current month spends more than it earns.</summary>
        public bool IsNetCashFlowNegative => NetCashFlow < 0;

        /// <summary>The five most recent transactions, newest first.</summary>
        public ObservableCollection<Transaction> RecentTransactions
        {
            get => _recentTransactions;
            private set
            {
                if (SetProperty(ref _recentTransactions, value))
                {
                    OnPropertyChanged(nameof(HasRecentTransactions));
                }
            }
        }

        /// <summary>The next five outstanding reminders, soonest first.</summary>
        public ObservableCollection<Reminder> UpcomingReminders
        {
            get => _upcomingReminders;
            private set
            {
                if (SetProperty(ref _upcomingReminders, value))
                {
                    OnPropertyChanged(nameof(HasUpcomingReminders));
                }
            }
        }

        public bool HasRecentTransactions => RecentTransactions.Count > 0;

        public bool HasUpcomingReminders => UpcomingReminders.Count > 0;

        /// <summary>Outstanding reminders whose date has passed, shown most overdue first.</summary>
        public ObservableCollection<Reminder> OverdueReminders
        {
            get => _overdueReminders;
            private set
            {
                if (SetProperty(ref _overdueReminders, value))
                {
                    OnPropertyChanged(nameof(HasOverdueReminders));
                }
            }
        }

        /// <summary>Total number of overdue reminders (not only the ones listed).</summary>
        public int OverdueReminderCount
        {
            get => _overdueReminderCount;
            private set
            {
                if (SetProperty(ref _overdueReminderCount, value))
                {
                    OnPropertyChanged(nameof(OverdueSummaryText));
                }
            }
        }

        public bool HasOverdueReminders => OverdueReminders.Count > 0;

        /// <summary>Alert text for the overdue panel, e.g. "3 reminders are overdue".</summary>
        public string OverdueSummaryText
        {
            get
            {
                if (OverdueReminderCount == 1)
                    return "1 reminder is overdue";

                if (OverdueReminderCount > OverdueReminders.Count)
                    return $"{OverdueReminderCount} reminders are overdue - showing the {OverdueReminders.Count} most overdue";

                return $"{OverdueReminderCount} reminders are overdue";
            }
        }

        /// <summary>Summary of the registered vehicle, or null when there is none.</summary>
        public VehicleSummary? Vehicle
        {
            get => _vehicle;
            private set
            {
                if (SetProperty(ref _vehicle, value))
                {
                    OnPropertyChanged(nameof(HasVehicle));
                }
            }
        }

        public bool HasVehicle => Vehicle != null;

        /// <summary>Chart model showing expenses of the last six calendar months.</summary>
        public PlotModel? ExpenseChart
        {
            get => _expenseChart;
            private set => SetProperty(ref _expenseChart, value);
        }

        public ICommand RefreshCommand { get; }

        public ICommand ViewAllTransactionsCommand { get; }

        /// <summary>Opens the Reminders screen; used by the reminder lists on the dashboard.</summary>
        public ICommand OpenRemindersCommand { get; }

        /// <summary>
        /// Reloads the dashboard from the database. Called when the dashboard becomes
        /// active (view Loaded) and by the Refresh command.
        /// </summary>
        public async Task RefreshAsync()
        {
            var snapshot = await _dashboardService.GetDashboardSnapshotAsync();

            TodayDisplay = snapshot.AsOfDate.ToString("dddd, d MMMM yyyy", CultureInfo.InvariantCulture);
            TotalAccountBalance = snapshot.TotalAccountBalance;
            CurrentMonthIncome = snapshot.CurrentMonthIncome;
            CurrentMonthExpenses = snapshot.CurrentMonthExpenses;

            RecentTransactions = new ObservableCollection<Transaction>(snapshot.RecentTransactions);
            UpcomingReminders = new ObservableCollection<Reminder>(snapshot.UpcomingReminders);

            // The panel lists the most overdue reminders; the count covers them all.
            OverdueReminderCount = snapshot.OverdueReminders.Count;
            OverdueReminders = new ObservableCollection<Reminder>(
                snapshot.OverdueReminders.Take(OverdueReminderDisplayCount));

            Vehicle = snapshot.Vehicle;

            ExpenseChart = BuildExpenseChart(snapshot.MonthlyExpenses);
        }

        private void NavigateToTransactions()
        {
            _serviceProvider.GetRequiredService<MainViewModel>().NavigateTo("Transactions");
        }

        private void NavigateToReminders()
        {
            _serviceProvider.GetRequiredService<MainViewModel>().NavigateTo("Reminders");
        }

        /// <summary>
        /// Builds a simple column chart (month on the X axis, total expense on the Y axis)
        /// for the supplied months, oldest first.
        /// </summary>
        private static PlotModel BuildExpenseChart(IReadOnlyList<MonthlyExpenseSummary> monthlyExpenses)
        {
            var model = new PlotModel
            {
                Background = OxyColors.Transparent,
                PlotAreaBorderColor = ChartLineColor,
                TextColor = ChartTextColor,
                // Left margin stays automatic (wide RM labels), enough room at the bottom for month labels.
                PlotMargins = new OxyThickness(double.NaN, 12, 12, 28)
            };

            // OxyPlot draws BarSeries as "transposed" bars when the axis keys are set
            // explicitly (value axis -> XAxisKey, category axis -> YAxisKey), which gives
            // normal vertical columns: months along the bottom, RM amounts up the side.
            var monthAxis = new CategoryAxis
            {
                Position = AxisPosition.Bottom,
                Key = CategoryAxisKey,
                ItemsSource = monthlyExpenses.Select(m => m.MonthLabel).ToList(),
                TextColor = ChartTextColor,
                TicklineColor = ChartLineColor,
                AxislineColor = ChartLineColor,
                MajorGridlineStyle = LineStyle.None,
                MinorGridlineStyle = LineStyle.None,
                GapWidth = 0.6
            };

            var amountAxis = new LinearAxis
            {
                Position = AxisPosition.Left,
                Key = ValueAxisKey,
                Minimum = 0,
                AbsoluteMinimum = 0,
                MaximumPadding = 0.15,
                TextColor = ChartTextColor,
                TicklineColor = ChartLineColor,
                AxislineColor = ChartLineColor,
                AxislineStyle = LineStyle.Solid,
                MajorGridlineStyle = LineStyle.Solid,
                MajorGridlineColor = ChartLineColor,
                MinorGridlineStyle = LineStyle.None,
                LabelFormatter = value => CurrencyFormatter.Format(value)
            };

            var series = new BarSeries
            {
                XAxisKey = ValueAxisKey,
                YAxisKey = CategoryAxisKey,
                FillColor = ChartBarColor,
                StrokeThickness = 0,
                TrackerFormatString = "{1}: {2}"
            };

            foreach (var month in monthlyExpenses)
            {
                series.Items.Add(new BarItem { Value = (double)month.TotalExpense });
            }

            model.Axes.Add(amountAxis);
            model.Axes.Add(monthAxis);
            model.Series.Add(series);

            return model;
        }
    }
}