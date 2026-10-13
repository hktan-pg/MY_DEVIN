using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MyApp.App.Models;

namespace MyApp.App
{
    public partial class TransactionDialog : Window
    {
        public Transaction Transaction { get; private set; }
        public bool IsSaved { get; private set; } = false;

        private readonly List<Account> _accounts;
        private readonly List<Category> _categories;
        private readonly string _defaultType;

        public TransactionDialog(
            Transaction? transaction = null,
            string? defaultType = null,
            List<Account>? accounts = null,
            List<Category>? categories = null)
        {
            InitializeComponent();

            WindowThemeHelper.EnableDarkTitleBar(this);

            _accounts = accounts ?? new List<Account>();
            _categories = categories ?? new List<Category>();
            _defaultType = defaultType ?? "Expense";

            if (transaction != null)
            {
                // Work with a copy so cancel doesn't mutate the original
                Transaction = new Transaction
                {
                    Id = transaction.Id,
                    TransactionDate = transaction.TransactionDate,
                    AccountId = transaction.AccountId,
                    CategoryId = transaction.CategoryId,
                    TransactionType = transaction.TransactionType,
                    Amount = transaction.Amount,
                    Description = transaction.Description,
                    ReferenceNo = transaction.ReferenceNo,
                    Notes = transaction.Notes,
                    TransferToAccountId = transaction.TransferToAccountId
                };
                Title = "Edit Transaction";
            }
            else
            {
                Transaction = new Transaction
                {
                    TransactionType = _defaultType,
                    TransactionDate = DateTime.Now
                };
                Title = $"Add {_defaultType} Transaction";
            }

            DataContext = Transaction;

            LoadAccounts();
            LoadCategories();
            LoadType();

            TransactionDatePicker.SelectedDate = Transaction.TransactionDate;
            if (Transaction.Amount > 0)
            {
                AmountTextBox.Text = Transaction.Amount.ToString("0.##");
            }
        }

        private void LoadAccounts()
        {
            var activeAccounts = _accounts.Where(a => a.IsActive).ToList();

            AccountComboBox.ItemsSource = activeAccounts;
            SourceAccountComboBox.ItemsSource = activeAccounts;
            DestinationAccountComboBox.ItemsSource = activeAccounts;

            // For edit mode, pre-set selections
            if (Transaction.AccountId > 0)
            {
                AccountComboBox.SelectedValue = Transaction.AccountId;
                SourceAccountComboBox.SelectedValue = Transaction.AccountId;
            }
            if (Transaction.TransferToAccountId.HasValue)
            {
                DestinationAccountComboBox.SelectedValue = Transaction.TransferToAccountId.Value;
            }
        }

        private void LoadCategories()
        {
            var type = Transaction.TransactionType;
            var filtered = _categories.Where(c => c.IsActive && c.CategoryType == type).OrderBy(c => c.Name).ToList();
            CategoryComboBox.ItemsSource = filtered;

            if (Transaction.CategoryId.HasValue)
            {
                CategoryComboBox.SelectedValue = Transaction.CategoryId.Value;
            }
        }

        private void LoadType()
        {
            // Select the correct item in the type combo box
            foreach (var item in TransactionTypeComboBox.Items)
            {
                if (item is ComboBoxItem comboItem && comboItem.Content?.ToString() == Transaction.TransactionType)
                {
                    TransactionTypeComboBox.SelectedItem = comboItem;
                    break;
                }
            }

            UpdatePanelVisibility();
        }

        private void TransactionTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Transaction == null) return;

            var selected = TransactionTypeComboBox.SelectedItem as ComboBoxItem;
            if (selected?.Content is string type && type != Transaction.TransactionType)
            {
                Transaction.TransactionType = type;
                Transaction.CategoryId = null;
                Transaction.TransferToAccountId = null;
                Title = type == "Transfer" ? "Add Transfer Transaction" : $"Add {type} Transaction";
                LoadCategories();
                UpdatePanelVisibility();
            }
        }

        private void UpdatePanelVisibility()
        {
            bool isTransfer = Transaction.TransactionType == "Transfer";

            AccountPanel.Visibility = isTransfer ? Visibility.Collapsed : Visibility.Visible;
            TransferPanel.Visibility = isTransfer ? Visibility.Visible : Visibility.Collapsed;

            // For transfer, category is not required - hide it
            var categoryLabel = CategoryLabel;
            if (categoryLabel != null)
            {
                categoryLabel.Visibility = isTransfer ? Visibility.Collapsed : Visibility.Visible;
            }
            CategoryComboBox.Visibility = isTransfer ? Visibility.Collapsed : Visibility.Visible;

            if (isTransfer)
            {
                Transaction.CategoryId = null;
            }
        }

        private void AmountTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            foreach (char c in e.Text)
            {
                if (!char.IsDigit(c) && c != '.')
                {
                    e.Handled = true;
                    return;
                }
            }

            var textBox = sender as System.Windows.Controls.TextBox;
            if (textBox != null)
            {
                var newText = textBox.Text.Insert(textBox.SelectionStart, e.Text);
                if (newText.Count(ch => ch == '.') > 1)
                {
                    e.Handled = true;
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Validate date
            if (TransactionDatePicker.SelectedDate == null)
            {
                System.Windows.MessageBox.Show("Please select a valid date.", "Validation Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
            Transaction.TransactionDate = TransactionDatePicker.SelectedDate.Value;

            // Validate amount
            if (!decimal.TryParse(AmountTextBox.Text, out decimal amount) || amount <= 0)
            {
                System.Windows.MessageBox.Show("Amount must be a number greater than zero.", "Validation Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
            Transaction.Amount = amount;

            if (Transaction.TransactionType == "Transfer")
            {
                var sourceId = SourceAccountComboBox.SelectedValue as int?;
                var destId = DestinationAccountComboBox.SelectedValue as int?;

                if (sourceId == null || sourceId <= 0)
                {
                    System.Windows.MessageBox.Show("Source account is required.", "Validation Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }

                if (destId == null || destId <= 0)
                {
                    System.Windows.MessageBox.Show("Destination account is required.", "Validation Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }

                if (sourceId.Value == destId.Value)
                {
                    System.Windows.MessageBox.Show("Source and destination accounts must be different.", "Validation Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }

                Transaction.AccountId = sourceId.Value;
                Transaction.TransferToAccountId = destId.Value;
                Transaction.CategoryId = null;
            }
            else
            {
                var accountId = AccountComboBox.SelectedValue as int?;
                if (accountId == null || accountId <= 0)
                {
                    System.Windows.MessageBox.Show("Account is required.", "Validation Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }
                Transaction.AccountId = accountId.Value;
                Transaction.TransferToAccountId = null;

                var categoryId = CategoryComboBox.SelectedValue as int?;
                if (categoryId == null || categoryId <= 0)
                {
                    System.Windows.MessageBox.Show(
                        Transaction.TransactionType == "Income"
                            ? "Income category is required."
                            : "Expense category is required.",
                        "Validation Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }
                Transaction.CategoryId = categoryId.Value;
            }

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