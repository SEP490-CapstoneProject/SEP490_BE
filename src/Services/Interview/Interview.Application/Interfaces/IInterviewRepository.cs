using InterviewEntity = Interview.Domain.Entities.Interview;

namespace Interview.Application.Interfaces;

public interface IInterviewRepository
{
    Task<InterviewEntity> CreateAsync(InterviewEntity interview);
    Task<InterviewEntity?> GetByIdAsync(int id);
    Task<IEnumerable<InterviewEntity>> GetAllAsync();
    Task<IEnumerable<InterviewEntity>> GetAllByUserIdAsync(int userId);
    Task<IEnumerable<InterviewEntity>> GetByDateAsync(int userId, DateTime date);
    Task<IEnumerable<InterviewEntity>> GetByStatusAsync(int userId, string status);
    Task UpdateAsync(InterviewEntity interview);
    Task DeleteAsync(int id);
}

