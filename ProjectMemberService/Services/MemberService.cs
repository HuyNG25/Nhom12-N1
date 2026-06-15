using Microsoft.EntityFrameworkCore;
using ProjectMemberService.Data;
using ProjectMemberService.DTOs;
using ProjectMemberService.Models;
using ProjectMemberService.Services;

namespace ProjectMemberService.Services
{
    public class MemberService : IMemberService
    {
        private readonly ProjectDbContext _context;
        private readonly ILogger<MemberService> _logger;
        private readonly IEventPublisher _eventPublisher;
        private readonly IPermissionService _permissionService;

        public MemberService(ProjectDbContext context, ILogger<MemberService> logger, 
                             IEventPublisher eventPublisher, IPermissionService permissionService)
        {
            _context = context;
            _logger = logger;
            _eventPublisher = eventPublisher;
            _permissionService = permissionService;
        }

        public async Task<ApiResponse<MemberResponseDto>> AddMemberAsync(Guid projectId, AddMemberDto dto, string operatorUserId)
        {
            var operatorMember = await _permissionService.GetMemberAsync(projectId, operatorUserId);
            if (operatorMember == null || (operatorMember.Role != MemberRole.Owner && operatorMember.Role != MemberRole.Manager))
                return ApiResponse<MemberResponseDto>.Fail("Bạn không có quyền quản lý thành viên");
            
            if (operatorMember.Role == MemberRole.Manager && (dto.Role == MemberRole.Owner || dto.Role == MemberRole.Manager))
                return ApiResponse<MemberResponseDto>.Fail("Manager không có quyền thêm Manager hoặc Owner");

            if (await _context.ProjectMembers.AnyAsync(m => m.ProjectId == projectId && m.UserId == dto.UserId))
                return ApiResponse<MemberResponseDto>.Fail("Người dùng đã là thành viên");

            var member = new ProjectMember
            {
                ProjectId = projectId,
                UserId = dto.UserId,
                DisplayName = dto.DisplayName,
                Email = dto.Email,
                Role = dto.Role
            };

            _context.ProjectMembers.Add(member);
            await _context.SaveChangesAsync();
            
            // Event Publisher
            await _eventPublisher.PublishAsync("project.member.added", member);

            return ApiResponse<MemberResponseDto>.Ok(MapToResponse(member), "Thêm thành viên thành công");
        }

        public async Task<ApiResponse<List<MemberResponseDto>>> GetMembersAsync(Guid projectId)
        {
            var projectExists = await _context.Projects.AnyAsync(p => p.Id == projectId);
            if (!projectExists)
                return ApiResponse<List<MemberResponseDto>>.Fail("Không tìm thấy dự án");

            var members = await _context.ProjectMembers
                .Where(m => m.ProjectId == projectId)
                .ToListAsync();

            var response = members.Select(MapToResponse).ToList();
            return ApiResponse<List<MemberResponseDto>>.Ok(response, "Lấy danh sách thành viên thành công");
        }

        public async Task<ApiResponse<MemberResponseDto>> UpdateRoleAsync(Guid projectId, Guid memberId, UpdateMemberRoleDto dto, string operatorUserId)
        {
            var operatorMember = await _permissionService.GetMemberAsync(projectId, operatorUserId);
            if (operatorMember == null || (operatorMember.Role != MemberRole.Owner && operatorMember.Role != MemberRole.Manager))
                return ApiResponse<MemberResponseDto>.Fail("Bạn không có quyền");
            var targetMember = await _context.ProjectMembers.FirstOrDefaultAsync(m => m.Id == memberId && m.ProjectId == projectId);

            if (targetMember == null) return ApiResponse<MemberResponseDto>.Fail("Không tìm thấy thành viên");
            if (targetMember.Role == MemberRole.Owner) return ApiResponse<MemberResponseDto>.Fail("Không thể đổi quyền Owner");

            if (operatorMember.Role == MemberRole.Manager)
            {
                if (targetMember.Role == MemberRole.Owner || targetMember.Role == MemberRole.Manager || 
                    dto.Role == MemberRole.Owner || dto.Role == MemberRole.Manager)
                {
                    return ApiResponse<MemberResponseDto>.Fail("Manager không có quyền can thiệp vào Manager/Owner");
                }
            }

            targetMember.Role = dto.Role;
            await _context.SaveChangesAsync();
            
            return ApiResponse<MemberResponseDto>.Ok(MapToResponse(targetMember), "Cập nhật vai trò thành công");
        }

        public async Task<ApiResponse<bool>> RemoveMemberAsync(Guid projectId, Guid memberId, string operatorUserId)
        {
            var operatorMember = await _permissionService.GetMemberAsync(projectId, operatorUserId);
            if (operatorMember == null || (operatorMember.Role != MemberRole.Owner && operatorMember.Role != MemberRole.Manager))
                return ApiResponse<bool>.Fail("Bạn không có quyền quản lý thành viên");
            var member = await _context.ProjectMembers.FirstOrDefaultAsync(m => m.Id == memberId && m.ProjectId == projectId);

            if (member == null) return ApiResponse<bool>.Fail("Không tìm thấy thành viên");
            if (member.Role == MemberRole.Owner) return ApiResponse<bool>.Fail("Không thể xóa Owner");

            if (operatorMember.Role == MemberRole.Manager && (member.Role == MemberRole.Manager || member.Role == MemberRole.Owner))
                return ApiResponse<bool>.Fail("Manager không có quyền xóa Manager khác hoặc Owner");

            _context.ProjectMembers.Remove(member);
            await _context.SaveChangesAsync();

            return ApiResponse<bool>.Ok(true, "Xóa thành viên thành công");
        }

        private static MemberResponseDto MapToResponse(ProjectMember member)
        {
            return new MemberResponseDto
            {
                Id = member.Id,
                ProjectId = member.ProjectId,
                UserId = member.UserId,
                DisplayName = member.DisplayName,
                Email = member.Email,
                Role = member.Role.ToString(),
                JoinedAt = member.JoinedAt
            };
        }
    }
}