using ProjectMemberService.DTOs;

namespace ProjectMemberService.Services
{
    public interface IProjectService
    {
        Task<ApiResponse<ProjectResponseDto>> CreateAsync(CreateProjectDto dto, string userId);
        Task<ApiResponse<List<ProjectResponseDto>>> GetAllAsync(string? userId = null);
        Task<ApiResponse<ProjectDetailResponseDto>> GetByIdAsync(Guid id, string userId);
        Task<ApiResponse<ProjectResponseDto>> UpdateAsync(Guid id, UpdateProjectDto dto, string userId);
        Task<ApiResponse<bool>> DeleteAsync(Guid id, string userId);
    }
}
