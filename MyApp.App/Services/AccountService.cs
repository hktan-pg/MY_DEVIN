using Microsoft.EntityFrameworkCore;
using MyApp.App.Data;
using MyApp.App.Models;

namespace MyApp.App.Services
{
    public class AccountService
    {
        private readonly AppDbContext _context;

        public AccountService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Account>> GetAllAccountsAsync()
        {
            return await _context.Accounts
                .OrderBy(a => a.Name)
                .ToListAsync();
        }

        public async Task<Account?> GetAccountByIdAsync(int id)
        {
            return await _context.Accounts.FindAsync(id);
        }

        public async Task<Account> CreateAccountAsync(Account account)
        {
            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();
            return account;
        }

        public async Task<Account> UpdateAccountAsync(Account account)
        {
            _context.Accounts.Update(account);
            await _context.SaveChangesAsync();
            return account;
        }

        public async Task<bool> DeactivateAccountAsync(int id)
        {
            var account = await _context.Accounts.FindAsync(id);
            if (account == null)
                return false;

            account.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAccountAsync(int id)
        {
            var account = await _context.Accounts.FindAsync(id);
            if (account == null)
                return false;

            // Check if account has transactions (as source OR transfer destination)
            var hasTransactions = await _context.Transactions.AnyAsync(t => t.AccountId == id || t.TransferToAccountId == id);
            if (hasTransactions)
                return false;

            _context.Accounts.Remove(account);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<decimal> CalculateCurrentBalanceAsync(int accountId)
        {
            var account = await _context.Accounts.FindAsync(accountId);
            if (account == null)
                return 0;

            // Include transactions where this account is the source OR the destination of a transfer
            var transactions = await _context.Transactions
                .Where(t => t.AccountId == accountId || t.TransferToAccountId == accountId)
                .ToListAsync();

            decimal currentBalance = account.OpeningBalance;

            foreach (var transaction in transactions)
            {
                switch (transaction.TransactionType)
                {
                    case "Income":
                        // Only add income that was posted to this account
                        if (transaction.AccountId == accountId)
                        {
                            currentBalance += transaction.Amount;
                        }
                        break;
                    case "Expense":
                        // Only subtract expenses that were posted from this account
                        if (transaction.AccountId == accountId)
                        {
                            currentBalance -= transaction.Amount;
                        }
                        break;
                    case "Transfer":
                        // If this account is the source of the transfer, subtract
                        if (transaction.AccountId == accountId)
                        {
                            currentBalance -= transaction.Amount;
                        }
                        // If this account is the destination of the transfer, add
                        else if (transaction.TransferToAccountId == accountId)
                        {
                            currentBalance += transaction.Amount;
                        }
                        break;
                }
            }

            return currentBalance;
        }

        public async Task<bool> HasTransactionHistoryAsync(int accountId)
        {
            return await _context.Transactions.AnyAsync(t => t.AccountId == accountId || t.TransferToAccountId == accountId);
        }
    }
}