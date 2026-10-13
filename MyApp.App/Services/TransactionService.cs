using Microsoft.EntityFrameworkCore;
using MyApp.App.Data;
using MyApp.App.Models;

namespace MyApp.App.Services
{
    public class TransactionService
    {
        private readonly AppDbContext _context;

        public TransactionService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Transaction>> GetTransactionsAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int? accountId = null,
            int? categoryId = null,
            string? transactionType = null,
            string? searchDescription = null,
            int? limit = null)
        {
            var query = _context.Transactions
                .Include(t => t.Account)
                .Include(t => t.Category)
                .Include(t => t.TransferToAccount)
                .AsQueryable();

            if (fromDate.HasValue)
            {
                var from = fromDate.Value.Date;
                query = query.Where(t => t.TransactionDate >= from);
            }

            if (toDate.HasValue)
            {
                var to = toDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(t => t.TransactionDate <= to);
            }

            if (accountId.HasValue)
            {
                // For transfers, include transactions where this account is source OR destination
                query = query.Where(t => t.AccountId == accountId.Value || t.TransferToAccountId == accountId.Value);
            }

            if (categoryId.HasValue)
            {
                query = query.Where(t => t.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(transactionType) && transactionType != "All")
            {
                query = query.Where(t => t.TransactionType == transactionType);
            }

            if (!string.IsNullOrWhiteSpace(searchDescription))
            {
                var search = searchDescription.Trim();
                query = query.Where(t => t.Description != null && t.Description.Contains(search));
            }

            IQueryable<Transaction> orderedQuery = query
                .OrderByDescending(t => t.TransactionDate)
                .ThenByDescending(t => t.Id);

            if (limit.HasValue)
            {
                orderedQuery = orderedQuery.Take(limit.Value);
            }

            return await orderedQuery.ToListAsync();
        }

        public async Task<Transaction?> GetTransactionByIdAsync(int id)
        {
            return await _context.Transactions
                .Include(t => t.Account)
                .Include(t => t.Category)
                .Include(t => t.TransferToAccount)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<Transaction> CreateTransactionAsync(Transaction transaction)
        {
            transaction.CreatedAt = DateTime.UtcNow;
            transaction.UpdatedAt = DateTime.UtcNow;

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();
            return transaction;
        }

        public async Task<Transaction> UpdateTransactionAsync(Transaction transaction)
        {
            transaction.UpdatedAt = DateTime.UtcNow;

            _context.Transactions.Update(transaction);
            await _context.SaveChangesAsync();
            return transaction;
        }

        public async Task<bool> DeleteTransactionAsync(int id)
        {
            var transaction = await _context.Transactions.FindAsync(id);
            if (transaction == null)
                return false;

            _context.Transactions.Remove(transaction);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetTransactionCountAsync()
        {
            return await _context.Transactions.CountAsync();
        }
    }
}