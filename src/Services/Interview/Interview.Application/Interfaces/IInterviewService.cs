using Interview.Application.DTOs;
using InterviewEntity = Interview.Domain.Entities.Interview;

namespace Interview.Application.Interfaces;

public interface IInterviewService
{
    Task<InterviewEntity> CreateInterviewAsync(InterviewEntity interview);
    Task<InterviewItemDto?> GetInterviewByIdAsync(int id);
    Task<IEnumerable<InterviewItemDto>> GetAllByUserIdAsync(int userId);
    Task<IEnumerable<InterviewItemDto>> GetByDateAsync(int userId, DateTime date);
    Task<IEnumerable<InterviewItemDto>> GetByStatusAsync(int userId, string status);
    Task UpdateInterviewAsync(InterviewEntity interview);
    Task DeleteInterviewAsync(int id);
}

