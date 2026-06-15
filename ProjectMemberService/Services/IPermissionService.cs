using ProjectMemberService.Models;

namespace ProjectMemberService.Services
{
    public interface IPermissionService
    {
        // Kiểm tra user có trong danh sách roles cho phép của project đó không
        Task<bool> IsAuthorizedAsync(Guid projectId, string userId, params MemberRole[] allowedRoles);
        Task<ProjectMember?> GetMemberAsync(Guid projectId, string userId);
    }
}