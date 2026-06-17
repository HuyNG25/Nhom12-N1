using Microsoft.EntityFrameworkCore;
using ProjectMemberService.Data;
using ProjectMemberService.DTOs;
using ProjectMemberService.Models;

namespace ProjectMemberService.Services
{
    public class ProjectService : IProjectService
    {
        private readonly ProjectDbContext _context;
        private readonly ILogger<ProjectService> _logger;
        private readonly IPermissionService _permissionService;

        public ProjectService(ProjectDbContext context, ILogger<ProjectService> logger, IPermissionService permissionService)
        {
            _context = context;
            _logger = logger;
            _permissionService = permissionService;
        }


        public async Task<ApiResponse<ProjectResponseDto>> CreateAsync(CreateProjectDto dto, string userId)
        {
            // Validate EndDate >= StartDate
            if (dto.EndDate.HasValue && dto.EndDate.Value < dto.StartDate)
            {
                return ApiResponse<ProjectResponseDto>.Fail("Ngày kết thúc phải sau ngày bắt đầu");
            }

            var project = new Project
            {
                Name = dto.Name,
                Description = dto.Description,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Color = dto.Color,
                CreatedBy = userId,
                Status = ProjectStatus.Active
            };

            _context.Projects.Add(project);

            // Tự động thêm người tạo là Owner
            var ownerMember = new ProjectMember
            {
                ProjectId = project.Id,
                UserId = userId,
                DisplayName = userId,
                Role = MemberRole.Owner
            };
            _context.ProjectMembers.Add(ownerMember);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Project '{Name}' created by user '{UserId}'", project.Name, userId);

            return ApiResponse<ProjectResponseDto>.Ok(MapToResponse(project), "Tạo dự án thành công");
        }

        public async Task<ApiResponse<List<ProjectResponseDto>>> GetAllAsync(string? userId = null)
        {
            var query = _context.Projects
                .Include(p => p.Members)
                .Include(p => p.Sprints)
                .AsQueryable();

            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(p => p.Members.Any(m => m.UserId == userId));
            }

            var projects = await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var result = projects.Select(MapToResponse).ToList();
            return ApiResponse<List<ProjectResponseDto>>.Ok(result);
        }

        public async Task<ApiResponse<ProjectDetailResponseDto>> GetByIdAsync(Guid id, string userId)
        {
            var project = await _context.Projects
                .Include(p => p.Members)
                .Include(p => p.Sprints)
                .Include(p => p.Milestones)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
            {
                return ApiResponse<ProjectDetailResponseDto>.Fail("Không tìm thấy dự án");
            }

            var isAuthorized = await _permissionService.IsAuthorizedAsync(id, userId, MemberRole.Owner, MemberRole.Manager, MemberRole.Member, MemberRole.Viewer);
            if (!isAuthorized)
            {
                return ApiResponse<ProjectDetailResponseDto>.Fail("Bạn không có quyền xem thông tin dự án này");
            }

            var response = new ProjectDetailResponseDto
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                StartDate = project.StartDate,
                EndDate = project.EndDate,
                Color = project.Color,
                Status = project.Status.ToString(),
                CreatedBy = project.CreatedBy,
                CreatedAt = project.CreatedAt,
                UpdatedAt = project.UpdatedAt,
                MemberCount = project.Members.Count,
                SprintCount = project.Sprints.Count,
                Members = project.Members.Select(m => new MemberResponseDto
                {
                    Id = m.Id,
                    ProjectId = m.ProjectId,
                    UserId = m.UserId,
                    DisplayName = m.DisplayName,
                    Email = m.Email,
                    Role = m.Role.ToString(),
                    JoinedAt = m.JoinedAt
                }).ToList(),
                Sprints = project.Sprints.Select(s => new SprintResponseDto
                {
                    Id = s.Id,
                    ProjectId = s.ProjectId,
                    Name = s.Name,
                    Goal = s.Goal,
                    StartDate = s.StartDate,
                    EndDate = s.EndDate,
                    Status = s.Status.ToString(),
                    CreatedAt = s.CreatedAt
                }).ToList(),
                Milestones = project.Milestones.Select(m => new MilestoneResponseDto
                {
                    Id = m.Id,
                    ProjectId = m.ProjectId,
                    Title = m.Title,
                    Description = m.Description,
                    DueDate = m.DueDate,
                    IsCompleted = m.IsCompleted,
                    CreatedAt = m.CreatedAt
                }).ToList()
            };

            return ApiResponse<ProjectDetailResponseDto>.Ok(response);
        }

        public async Task<ApiResponse<ProjectResponseDto>> UpdateAsync(Guid id, UpdateProjectDto dto, string userId)
        {
            var project = await _context.Projects
                .Include(p => p.Members)
                .Include(p => p.Sprints)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
            {
                return ApiResponse<ProjectResponseDto>.Fail("Không tìm thấy dự án");
            }

            // Kiểm tra quyền: chỉ Owner hoặc Manager hoặc Admin hệ thống mới được sửa
            var isAuthorized = await _permissionService.IsAuthorizedAsync(id, userId, MemberRole.Owner, MemberRole.Manager);
            if (!isAuthorized)
            {
                return ApiResponse<ProjectResponseDto>.Fail("Bạn không có quyền chỉnh sửa dự án này");
            }


            if (dto.EndDate.HasValue && dto.EndDate.Value < dto.StartDate)
            {
                return ApiResponse<ProjectResponseDto>.Fail("Ngày kết thúc phải sau ngày bắt đầu");
            }

            project.Name = dto.Name;
            project.Description = dto.Description;
            project.StartDate = dto.StartDate;
            project.EndDate = dto.EndDate;
            project.Color = dto.Color;
            project.Status = dto.Status;
            project.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Project '{Name}' updated by user '{UserId}'", project.Name, userId);

            return ApiResponse<ProjectResponseDto>.Ok(MapToResponse(project), "Cập nhật dự án thành công");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(Guid id, string userId)
        {
            var project = await _context.Projects
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
            {
                return ApiResponse<bool>.Fail("Không tìm thấy dự án");
            }

            // Chỉ Owner hoặc Admin hệ thống mới được xóa
            var isAuthorized = await _permissionService.IsAuthorizedAsync(id, userId, MemberRole.Owner);
            if (!isAuthorized)
            {
                return ApiResponse<bool>.Fail("Chỉ Owner mới có quyền xóa dự án");
            }


            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Project '{Name}' deleted by user '{UserId}'", project.Name, userId);

            return ApiResponse<bool>.Ok(true, "Xóa dự án thành công");
        }

        private static ProjectResponseDto MapToResponse(Project project)
        {
            return new ProjectResponseDto
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                StartDate = project.StartDate,
                EndDate = project.EndDate,
                Color = project.Color,
                Status = project.Status.ToString(),
                CreatedBy = project.CreatedBy,
                CreatedAt = project.CreatedAt,
                UpdatedAt = project.UpdatedAt,
                MemberCount = project.Members?.Count ?? 0,
                SprintCount = project.Sprints?.Count ?? 0
            };
        }
    }
}
