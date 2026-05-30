using Portfolio.Domain.Entities;

namespace Portfolio.Application.Interfaces;

public interface IRewardPointTransactionRepository
{
    Task<RewardPointTransaction> AddAsync(RewardPointTransaction transaction);
    Task<List<RewardPointTransaction>> GetByUserIdAsync(int userId);
    Task<List<RewardPointTransaction>> GetByUserIdAndDateRangeAsync(int userId, DateTime startDate, DateTime endDate);
    Task<decimal> GetUserTotalPointsAsync(int userId);
    Task<decimal> GetUserTodayEarnedAsync(int userId);
    Task<RewardPointTransaction?> GetByIdAsync(int id);
    Task<List<RewardPointTransaction>> GetRecentTransactionsAsync(int userId, int days = 30);
}
