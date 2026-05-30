using Microsoft.EntityFrameworkCore;
using Portfolio.Application.Interfaces;
using Portfolio.Domain.Entities;
using Portfolio.Infrastructure.Data;

namespace Portfolio.Infrastructure.Repositories;

public class RewardPointTransactionRepository : IRewardPointTransactionRepository
{
    private readonly PortfolioDbContext _context;

    public RewardPointTransactionRepository(PortfolioDbContext context)
    {
        _context = context;
    }

    public async Task<RewardPointTransaction> AddAsync(RewardPointTransaction transaction)
    {
        _context.RewardPointTransactions.Add(transaction);
        await _context.SaveChangesAsync();
        return transaction;
    }

    public async Task<List<RewardPointTransaction>> GetByUserIdAsync(int userId)
    {
        return await _context.RewardPointTransactions
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<RewardPointTransaction>> GetByUserIdAndDateRangeAsync(int userId, DateTime startDate, DateTime endDate)
    {
        return await _context.RewardPointTransactions
            .Where(t => t.UserId == userId && t.CreatedAt >= startDate && t.CreatedAt <= endDate)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<decimal> GetUserTotalPointsAsync(int userId)
    {
        var earned = await _context.RewardPointTransactions
            .Where(t => t.UserId == userId && t.Type == RewardPointType.Earn)
            .SumAsync(t => t.Points);

        var spent = await _context.RewardPointTransactions
            .Where(t => t.UserId == userId && t.Type == RewardPointType.Spend)
            .SumAsync(t => t.Points);

        return earned - spent;
    }

    public async Task<decimal> GetUserTodayEarnedAsync(int userId)
    {
        var today = DateTime.UtcNow.Date;
        return await _context.RewardPointTransactions
            .Where(t => t.UserId == userId 
                && t.Type == RewardPointType.Earn 
                && t.CreatedAt >= today
                && t.CreatedAt < today.AddDays(1))
            .SumAsync(t => t.Points);
    }

    public async Task<RewardPointTransaction?> GetByIdAsync(int id)
    {
        return await _context.RewardPointTransactions.FindAsync(id);
    }

    public async Task<List<RewardPointTransaction>> GetRecentTransactionsAsync(int userId, int days = 30)
    {
        var startDate = DateTime.UtcNow.AddDays(-days);
        return await GetByUserIdAndDateRangeAsync(userId, startDate, DateTime.UtcNow);
    }
}
