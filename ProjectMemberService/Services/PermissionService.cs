using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ProjectMemberService.Data;
using ProjectMemberService.Models;

namespace ProjectMemberService.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly ProjectDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PermissionService(ProjectDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<bool> IsAuthorizedAsync(Guid projectId, string userId, params MemberRole[] allowedRoles)
        {
            if (await IsSystemAdminAsync(userId))
            {
                return true;
            }

            var member = await GetMemberAsync(projectId, userId);
            
            return member != null && allowedRoles.Contains(member.Role);
        }

        public async Task<ProjectMember?> GetMemberAsync(Guid projectId, string userId)
        {
            return await _context.ProjectMembers
                .FirstOrDefaultAsync(m => m.ProjectId == projectId && m.UserId == userId);
        }

        public async Task<bool> IsSystemAdminAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return false;
            }

            // 1. Check HttpContext for JWT Claims or Custom Headers
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                // Check X-User-Role header
                var roleHeader = httpContext.Request.Headers["X-User-Role"].FirstOrDefault();
                if (string.Equals(roleHeader, "Admin", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                // Check Claims (JWT)
                var user = httpContext.User;
                if (user != null)
                {
                    if (user.IsInRole("Admin") || 
                        user.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value == "Admin" ||
                        user.FindFirst("role")?.Value == "Admin")
                    {
                        return true;
                    }
                }
            }

            // 2. Check Database (SystemAdmins table)
            return await _context.SystemAdmins.AnyAsync(a => a.UserId == userId);
        }
    }
}