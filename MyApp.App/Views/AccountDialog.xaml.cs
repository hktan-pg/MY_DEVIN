using System.Windows;
using System.Windows.Input;
using MyApp.App.Models;

namespace MyApp.App
{
    public partial class AccountDialog : Window
    {
        public Account Account { get; private set; }
        public bool IsSaved { get; private set; } = false;

        public AccountDialog(Account? account = null)
        {
            InitializeComponent();

            // Unlock dark title bar to match the black theme
            WindowThemeHelper.EnableDarkTitleBar(this);

            if (account != null)
            {
                // Work with a copy so cancel doesn't mutate the original
                Account = new Account
                {
                    Id = account.Id,
                    Name = account.Name,
                    AccountType = account.AccountType,
                    AccountNumber = account.AccountNumber,
                    OpeningBalance = account.OpeningBalance,
                    IsActive = account.IsActive,
                    Notes = account.Notes,
                    CreatedAt = account.CreatedAt
                };
                Title = "Edit Account";
            }
            else
            {
                Account = new Account
                {
                    IsActive = true,
                    AccountType = "Bank",
                    OpeningBalance = 0
                };
                Title = "Add Account";
            }

            DataContext = Account;
            OpeningBalanceTextBox.Text = Account.OpeningBalance.ToString("0.##");
        }

        private void OpeningBalanceTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Only allow digits, one decimal point, and a leading minus sign
            foreach (char c in e.Text)
            {
                if (!char.IsDigit(c) && c != '.' && c != '-')
                {
                    e.Handled = true;
                    return;
                }
            }

            // Prevent multiple decimal points or minus signs
            var textBox = sender as System.Windows.Controls.TextBox;
            if (textBox != null)
            {
                var newText = textBox.Text.Insert(textBox.SelectionStart, e.Text);

                // Only one decimal point allowed
                if (newText.Count(ch => ch == '.') > 1)
                {
                    e.Handled = true;
                    return;
                }

                // Minus sign only allowed at the beginning and only once
                if (newText.Count(ch => ch == '-') > 1 ||
                    (newText.Contains('-') && !newText.StartsWith("-")))
                {
                    e.Handled = true;
                    return;
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Account.Name))
            {
                System.Windows.MessageBox.Show("Name is required.", "Validation Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(Account.AccountType))
            {
                System.Windows.MessageBox.Show("Account Type is required.", "Validation Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(OpeningBalanceTextBox.Text, out decimal openingBalance))
            {
                System.Windows.MessageBox.Show("Opening Balance must be a valid monetary value.", "Validation Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            Account.OpeningBalance = openingBalance;
            IsSaved = true;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}