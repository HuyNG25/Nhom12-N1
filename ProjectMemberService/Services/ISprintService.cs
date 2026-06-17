using ProjectMemberService.DTOs;

namespace ProjectMemberService.Services
{
    public interface ISprintService
    {
        Task<ApiResponse<SprintResponseDto>> CreateAsync(Guid projectId, CreateSprintDto dto, string operatorUserId);
        Task<ApiResponse<List<SprintResponseDto>>> GetAllAsync(Guid projectId, string userId);
        Task<ApiResponse<SprintResponseDto>> GetByIdAsync(Guid projectId, Guid sprintId, string userId);
        Task<ApiResponse<SprintResponseDto>> UpdateAsync(Guid projectId, Guid sprintId, UpdateSprintDto dto, string operatorUserId);
        Task<ApiResponse<SprintResponseDto>> StartSprintAsync(Guid projectId, Guid sprintId, string operatorUserId);
        Task<ApiResponse<SprintResponseDto>> CompleteSprintAsync(Guid projectId, Guid sprintId, string operatorUserId);
    }
}
