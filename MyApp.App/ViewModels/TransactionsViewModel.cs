using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using MyApp.App.Models;
using MyApp.App.Services;

namespace MyApp.App.ViewModels
{
    public class TransactionsViewModel : ViewModelBase
    {
        private readonly TransactionService _transactionService;
        private readonly AccountService _accountService;
        private readonly CategoryService _categoryService;

        private ObservableCollection<Transaction> _transactions;
        private Transaction? _selectedTransaction;

        private ObservableCollection<Account> _accounts;
        private ObservableCollection<Category> _categories;

        private DateTime? _fromDate;
        private DateTime? _toDate;
        private int? _filterAccountId;
        private int? _filterCategoryId;
        private string? _filterTransactionType;
        private string? _searchDescription;

        public TransactionsViewModel(
            TransactionService transactionService,
            AccountService accountService,
            CategoryService categoryService)
        {
            _transactionService = transactionService;
            _accountService = accountService;
            _categoryService = categoryService;

            _transactions = new ObservableCollection<Transaction>();
            _accounts = new ObservableCollection<Account>();
            _categories = new ObservableCollection<Category>();
            _filterTransactionType = "All";

            LoadTransactionsCommand = new RelayCommand(async _ => await LoadTransactionsAsync());
            AddIncomeCommand = new RelayCommand(_ => AddTransaction("Income"));
            AddExpenseCommand = new RelayCommand(_ => AddTransaction("Expense"));
            AddTransferCommand = new RelayCommand(_ => AddTransaction("Transfer"));
            EditTransactionCommand = new RelayCommand(_ => EditTransaction(), _ => CanEditTransaction());
            DeleteTransactionCommand = new RelayCommand(_ => DeleteTransactionAsync(), _ => CanEditTransaction());
            RefreshCommand = new RelayCommand(async _ => await LoadTransactionsAsync());
            ApplyFilterCommand = new RelayCommand(async _ => await ApplyFilterAsync());
            ClearFiltersCommand = new RelayCommand(async _ => await ClearFiltersAsync());
        }

        public ObservableCollection<Transaction> Transactions
        {
            get => _transactions;
            set => SetProperty(ref _transactions, value);
        }

        public Transaction? SelectedTransaction
        {
            get => _selectedTransaction;
            set
            {
                SetProperty(ref _selectedTransaction, value);
                ((RelayCommand)EditTransactionCommand).RaiseCanExecuteChanged();
                ((RelayCommand)DeleteTransactionCommand).RaiseCanExecuteChanged();
            }
        }

        public ObservableCollection<Account> Accounts
        {
            get => _accounts;
            set => SetProperty(ref _accounts, value);
        }

        public ObservableCollection<Category> Categories
        {
            get => _categories;
            set => SetProperty(ref _categories, value);
        }

        public DateTime? FromDate
        {
            get => _fromDate;
            set => SetProperty(ref _fromDate, value);
        }

        public DateTime? ToDate
        {
            get => _toDate;
            set => SetProperty(ref _toDate, value);
        }

        public int? FilterAccountId
        {
            get => _filterAccountId;
            set => SetProperty(ref _filterAccountId, value);
        }

        public int? FilterCategoryId
        {
            get => _filterCategoryId;
            set => SetProperty(ref _filterCategoryId, value);
        }

        public string? FilterTransactionType
        {
            get => _filterTransactionType;
            set => SetProperty(ref _filterTransactionType, value);
        }

        public string? SearchDescription
        {
            get => _searchDescription;
            set => SetProperty(ref _searchDescription, value);
        }

        public string[] TransactionTypes { get; } = { "All", "Income", "Expense", "Transfer" };

        public ICommand LoadTransactionsCommand { get; }
        public ICommand AddIncomeCommand { get; }
        public ICommand AddExpenseCommand { get; }
        public ICommand AddTransferCommand { get; }
        public ICommand EditTransactionCommand { get; }
        public ICommand DeleteTransactionCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ApplyFilterCommand { get; }
        public ICommand ClearFiltersCommand { get; }

        public async Task LoadTransactionsAsync()
        {
            await LoadReferenceDataAsync();

            var transactions = await _transactionService.GetTransactionsAsync(
                FromDate,
                ToDate,
                FilterAccountId,
                FilterCategoryId,
                string.IsNullOrWhiteSpace(FilterTransactionType) || FilterTransactionType == "All" ? null : FilterTransactionType,
                SearchDescription);

            Transactions = new ObservableCollection<Transaction>(transactions);
        }

        private async Task LoadReferenceDataAsync()
        {
            var accounts = await _accountService.GetAllAccountsAsync();
            var categories = await _categoryService.GetAllCategoriesAsync();

            var selectedAccountId = FilterAccountId;
            var selectedCategoryId = FilterCategoryId;

            Accounts = new ObservableCollection<Account>(accounts);
            Categories = new ObservableCollection<Category>(categories);

            // Restore filter selections after refreshing the list
            if (selectedAccountId.HasValue && accounts.Any(a => a.Id == selectedAccountId.Value))
            {
                FilterAccountId = selectedAccountId.Value;
            }
            if (selectedCategoryId.HasValue && categories.Any(c => c.Id == selectedCategoryId.Value))
            {
                FilterCategoryId = selectedCategoryId.Value;
            }
        }

        private bool CanEditTransaction()
        {
            return SelectedTransaction != null;
        }

        private async void AddTransaction(string type)
        {
            var accounts = await _accountService.GetAllAccountsAsync();
            var categories = await _categoryService.GetAllCategoriesAsync();

            var dialog = new TransactionDialog(null, type, accounts, categories);
            if (dialog.ShowDialog() == true && dialog.IsSaved)
            {
                var transaction = dialog.Transaction;

                // Re-validate transfer rules
                if (transaction.TransactionType == "Transfer")
                {
                    if (transaction.AccountId == transaction.TransferToAccountId)
                    {
                        System.Windows.MessageBox.Show("Source and destination accounts must be different.", "Validation Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                        return;
                    }
                }

                await _transactionService.CreateTransactionAsync(transaction);
                await LoadTransactionsAsync();
            }
        }

        private async void EditTransaction()
        {
            if (SelectedTransaction == null) return;

            var accounts = await _accountService.GetAllAccountsAsync();
            var categories = await _categoryService.GetAllCategoriesAsync();

            var dialog = new TransactionDialog(SelectedTransaction, null, accounts, categories);
            if (dialog.ShowDialog() == true && dialog.IsSaved)
            {
                var edited = dialog.Transaction;

                // Copy edited values back to the selected transaction
                SelectedTransaction.TransactionDate = edited.TransactionDate;
                SelectedTransaction.TransactionType = edited.TransactionType;
                SelectedTransaction.AccountId = edited.AccountId;
                SelectedTransaction.CategoryId = edited.CategoryId;
                SelectedTransaction.TransferToAccountId = edited.TransferToAccountId;
                SelectedTransaction.Amount = edited.Amount;
                SelectedTransaction.Description = edited.Description;
                SelectedTransaction.ReferenceNo = edited.ReferenceNo;
                SelectedTransaction.Notes = edited.Notes;

                await _transactionService.UpdateTransactionAsync(SelectedTransaction);
                await LoadTransactionsAsync();
            }
        }

        private async void DeleteTransactionAsync()
        {
            var transaction = SelectedTransaction;
            if (transaction == null) return;

            var result = System.Windows.MessageBox.Show(
                $"Are you sure you want to delete this {transaction.TransactionType} transaction dated {transaction.TransactionDate:d} for {transaction.Amount:C}?",
                "Confirm Deletion",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                var success = await _transactionService.DeleteTransactionAsync(transaction.Id);
                if (success)
                {
                    await LoadTransactionsAsync();
                }
                else
                {
                    System.Windows.MessageBox.Show("Failed to delete the transaction.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        private async Task ApplyFilterAsync()
        {
            await LoadTransactionsAsync();
        }

        private async Task ClearFiltersAsync()
        {
            FromDate = null;
            ToDate = null;
            FilterAccountId = null;
            FilterCategoryId = null;
            FilterTransactionType = "All";
            SearchDescription = null;

            await LoadTransactionsAsync();
        }
    }
}