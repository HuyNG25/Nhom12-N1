using Microsoft.EntityFrameworkCore;
using ProjectMemberService.Data;
using ProjectMemberService.Models;

namespace ProjectMemberService.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly ProjectDbContext _context;
        public PermissionService(ProjectDbContext context) => _context = context;

        public async Task<bool> IsAuthorizedAsync(Guid projectId, string userId, params MemberRole[] allowedRoles)
        {
            var member = await GetMemberAsync(projectId, userId);
            
            return member != null && allowedRoles.Contains(member.Role);
        }

        public async Task<ProjectMember?> GetMemberAsync(Guid projectId, string userId)
        {
            return await _context.ProjectMembers
                .FirstOrDefaultAsync(m => m.ProjectId == projectId && m.UserId == userId);
        }
    }
}