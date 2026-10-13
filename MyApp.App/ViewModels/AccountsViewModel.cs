using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using MyApp.App.Models;
using MyApp.App.Services;

namespace MyApp.App.ViewModels
{
    public class AccountsViewModel : ViewModelBase
    {
        private readonly AccountService _accountService;
        private ObservableCollection<Account> _accounts;
        private Account? _selectedAccount;

        public AccountsViewModel(AccountService accountService)
        {
            _accountService = accountService;
            _accounts = new ObservableCollection<Account>();
            
            LoadAccountsCommand = new RelayCommand(async _ => await LoadAccountsAsync());
            AddAccountCommand = new RelayCommand(_ => AddAccount());
            EditAccountCommand = new RelayCommand(_ => EditAccount(), _ => CanEditAccount());
            DeactivateAccountCommand = new RelayCommand(_ => DeactivateAccount(), _ => CanEditAccount());
            DeleteAccountCommand = new RelayCommand(_ => DeleteAccount(), _ => CanEditAccount());
        }

        public ObservableCollection<Account> Accounts
        {
            get => _accounts;
            set => SetProperty(ref _accounts, value);
        }

        public Account? SelectedAccount
        {
            get => _selectedAccount;
            set
            {
                SetProperty(ref _selectedAccount, value);
                ((RelayCommand)EditAccountCommand).RaiseCanExecuteChanged();
                ((RelayCommand)DeactivateAccountCommand).RaiseCanExecuteChanged();
                ((RelayCommand)DeleteAccountCommand).RaiseCanExecuteChanged();
            }
        }

        public ICommand LoadAccountsCommand { get; }
        public ICommand AddAccountCommand { get; }
        public ICommand EditAccountCommand { get; }
        public ICommand DeactivateAccountCommand { get; }
        public ICommand DeleteAccountCommand { get; }

        public async Task LoadAccountsAsync()
        {
            var accounts = await _accountService.GetAllAccountsAsync();
            
            // Calculate current balance for each account
            foreach (var account in accounts)
            {
                account.CurrentBalance = await _accountService.CalculateCurrentBalanceAsync(account.Id);
            }
            
            Accounts = new ObservableCollection<Account>(accounts);
        }

        private async void AddAccount()
        {
            var dialog = new AccountDialog();
            if (dialog.ShowDialog() == true && dialog.IsSaved)
            {
                var account = dialog.Account;

                // Save to database
                await _accountService.CreateAccountAsync(account);
                await LoadAccountsAsync(); // Refresh to get the ID and calculate balance
            }
        }

        private bool CanEditAccount()
        {
            return SelectedAccount != null;
        }

        private async void EditAccount()
        {
            if (SelectedAccount == null) return;

            var dialog = new AccountDialog(SelectedAccount);
            if (dialog.ShowDialog() == true && dialog.IsSaved)
            {
                var edited = dialog.Account;

                // Update the selected account with the edited values
                SelectedAccount.Name = edited.Name;
                SelectedAccount.AccountType = edited.AccountType;
                SelectedAccount.AccountNumber = edited.AccountNumber;
                SelectedAccount.OpeningBalance = edited.OpeningBalance;
                SelectedAccount.Notes = edited.Notes;
                SelectedAccount.IsActive = edited.IsActive;

                // Update in database
                await _accountService.UpdateAccountAsync(SelectedAccount);
                await LoadAccountsAsync();
            }
        }

        private async void DeactivateAccount()
        {
            if (SelectedAccount == null) return;

            var result = System.Windows.MessageBox.Show(
                $"Are you sure you want to deactivate '{SelectedAccount.Name}'?",
                "Confirm Deactivation",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                await _accountService.DeactivateAccountAsync(SelectedAccount.Id);
                SelectedAccount.IsActive = false;
                
                // Refresh the list
                await LoadAccountsAsync();
            }
        }

        private async void DeleteAccount()
        {
            var account = SelectedAccount;
            if (account == null) return;

            // Check if account has transaction history
            var hasHistory = await _accountService.HasTransactionHistoryAsync(account.Id);
            
            if (hasHistory)
            {
                System.Windows.MessageBox.Show(
                    "This account has transaction history and cannot be deleted. Please deactivate it instead.",
                    "Cannot Delete",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            var result = System.Windows.MessageBox.Show(
                $"Are you sure you want to permanently delete '{account.Name}'?",
                "Confirm Deletion",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                var success = await _accountService.DeleteAccountAsync(account.Id);
                if (success)
                {
                    Accounts.Remove(account);
                    SelectedAccount = null;
                }
                else
                {
                    System.Windows.MessageBox.Show("Failed to delete the account.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }
    }
}