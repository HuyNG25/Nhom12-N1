using Microsoft.EntityFrameworkCore;
using ProjectMemberService.Data;
using ProjectMemberService.DTOs;
using ProjectMemberService.Models;

namespace ProjectMemberService.Services
{
    public class SprintService : ISprintService
    {
        private readonly ProjectDbContext _context;
        private readonly ILogger<SprintService> _logger;
        private readonly IEventPublisher _eventPublisher;
        private readonly IPermissionService _permissionService;

        public SprintService(ProjectDbContext context, ILogger<SprintService> logger, 
                             IEventPublisher eventPublisher, IPermissionService permissionService)
        {
            _context = context;
            _logger = logger;
            _eventPublisher = eventPublisher;
            _permissionService = permissionService;
        }


        public async Task<ApiResponse<SprintResponseDto>> CreateAsync(Guid projectId, CreateSprintDto dto, string operatorUserId)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null)
            {
                return ApiResponse<SprintResponseDto>.Fail("Không tìm thấy dự án");
            }

            // Kiểm tra quyền của người thực hiện
            var isAuthorized = await _permissionService.IsAuthorizedAsync(projectId, operatorUserId, MemberRole.Owner, MemberRole.Manager);
            if (!isAuthorized)
            {
                return ApiResponse<SprintResponseDto>.Fail("Bạn không có quyền quản trị sprint trong dự án này");
            }


            var startDate = dto.StartDate ?? DateTime.UtcNow;
            var endDate = dto.EndDate ?? startDate.AddDays(14); // Mặc định 2 tuần

            if (endDate < startDate)
            {
                return ApiResponse<SprintResponseDto>.Fail("Ngày kết thúc phải sau ngày bắt đầu");
            }

            // Kiểm tra ngày của sprint so với dự án
            if (startDate < project.StartDate)
            {
                return ApiResponse<SprintResponseDto>.Fail("Ngày bắt đầu sprint không được trước ngày bắt đầu dự án");
            }
            if (project.EndDate.HasValue && endDate > project.EndDate.Value)
            {
                return ApiResponse<SprintResponseDto>.Fail("Ngày kết thúc sprint không được sau ngày kết thúc dự án");
            }

            var sprint = new Sprint
            {
                ProjectId = projectId,
                Name = dto.Name,
                Goal = dto.Goal,
                StartDate = startDate,
                EndDate = endDate,
                Status = SprintStatus.Planning
            };

            _context.Sprints.Add(sprint);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Sprint '{Name}' created for project {ProjectId}", sprint.Name, projectId);

            return ApiResponse<SprintResponseDto>.Ok(MapToResponse(sprint), "Tạo sprint thành công");
        }

        public async Task<ApiResponse<List<SprintResponseDto>>> GetAllAsync(Guid projectId, string userId)
        {
            var projectExists = await _context.Projects.AnyAsync(p => p.Id == projectId);
            if (!projectExists)
            {
                return ApiResponse<List<SprintResponseDto>>.Fail("Không tìm thấy dự án");
            }

            var isAuthorized = await _permissionService.IsAuthorizedAsync(projectId, userId, MemberRole.Owner, MemberRole.Manager, MemberRole.Member, MemberRole.Viewer);
            if (!isAuthorized)
            {
                return ApiResponse<List<SprintResponseDto>>.Fail("Bạn không có quyền xem danh sách sprint của dự án này");
            }

            var sprints = await _context.Sprints
                .Where(s => s.ProjectId == projectId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            var result = sprints.Select(MapToResponse).ToList();
            return ApiResponse<List<SprintResponseDto>>.Ok(result);
        }

        public async Task<ApiResponse<SprintResponseDto>> GetByIdAsync(Guid projectId, Guid sprintId, string userId)
        {
            var sprint = await _context.Sprints
                .FirstOrDefaultAsync(s => s.Id == sprintId && s.ProjectId == projectId);

            if (sprint == null)
            {
                return ApiResponse<SprintResponseDto>.Fail("Không tìm thấy sprint");
            }

            var isAuthorized = await _permissionService.IsAuthorizedAsync(projectId, userId, MemberRole.Owner, MemberRole.Manager, MemberRole.Member, MemberRole.Viewer);
            if (!isAuthorized)
            {
                return ApiResponse<SprintResponseDto>.Fail("Bạn không có quyền xem thông tin sprint này");
            }

            return ApiResponse<SprintResponseDto>.Ok(MapToResponse(sprint));
        }

        public async Task<ApiResponse<SprintResponseDto>> UpdateAsync(Guid projectId, Guid sprintId, UpdateSprintDto dto, string operatorUserId)
        {
            var sprint = await _context.Sprints
                .Include(s => s.Project)
                .FirstOrDefaultAsync(s => s.Id == sprintId && s.ProjectId == projectId);

            if (sprint == null)
            {
                return ApiResponse<SprintResponseDto>.Fail("Không tìm thấy sprint");
            }

            // Kiểm tra quyền của người thực hiện
            var isAuthorized = await _permissionService.IsAuthorizedAsync(projectId, operatorUserId, MemberRole.Owner, MemberRole.Manager);
            if (!isAuthorized)
            {
                return ApiResponse<SprintResponseDto>.Fail("Bạn không có quyền quản trị sprint trong dự án này");
            }


            if (sprint.Status == SprintStatus.Completed)
            {
                return ApiResponse<SprintResponseDto>.Fail("Không thể sửa sprint đã hoàn thành");
            }

            var proposedStartDate = dto.StartDate ?? sprint.StartDate;
            var proposedEndDate = dto.EndDate ?? sprint.EndDate;

            if (proposedEndDate < proposedStartDate)
            {
                return ApiResponse<SprintResponseDto>.Fail("Ngày kết thúc phải sau ngày bắt đầu");
            }

            // Kiểm tra ngày của sprint so với dự án
            if (proposedStartDate < sprint.Project.StartDate)
            {
                return ApiResponse<SprintResponseDto>.Fail("Ngày bắt đầu sprint không được trước ngày bắt đầu dự án");
            }
            if (sprint.Project.EndDate.HasValue && proposedEndDate > sprint.Project.EndDate.Value)
            {
                return ApiResponse<SprintResponseDto>.Fail("Ngày kết thúc sprint không được sau ngày kết thúc dự án");
            }

            sprint.Name = dto.Name;
            sprint.Goal = dto.Goal;
            sprint.StartDate = proposedStartDate;
            sprint.EndDate = proposedEndDate;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Sprint '{Name}' updated in project {ProjectId}", sprint.Name, projectId);

            return ApiResponse<SprintResponseDto>.Ok(MapToResponse(sprint), "Cập nhật sprint thành công");
        }

        public async Task<ApiResponse<SprintResponseDto>> StartSprintAsync(Guid projectId, Guid sprintId, string operatorUserId)
        {
            var sprint = await _context.Sprints
                .FirstOrDefaultAsync(s => s.Id == sprintId && s.ProjectId == projectId);

            if (sprint == null)
            {
                return ApiResponse<SprintResponseDto>.Fail("Không tìm thấy sprint");
            }

            // Kiểm tra quyền của người thực hiện
            var isAuthorized = await _permissionService.IsAuthorizedAsync(projectId, operatorUserId, MemberRole.Owner, MemberRole.Manager);
            if (!isAuthorized)
            {
                return ApiResponse<SprintResponseDto>.Fail("Bạn không có quyền quản trị sprint trong dự án này");
            }


            if (sprint.Status != SprintStatus.Planning)
            {
                return ApiResponse<SprintResponseDto>.Fail("Chỉ có thể bắt đầu sprint đang ở trạng thái Planning");
            }

            // Kiểm tra không có sprint Active khác trong project
            var hasActiveSprint = await _context.Sprints
                .AnyAsync(s => s.ProjectId == projectId && s.Status == SprintStatus.Active);

            if (hasActiveSprint)
            {
                return ApiResponse<SprintResponseDto>.Fail("Dự án đã có sprint đang chạy. Hãy hoàn thành sprint hiện tại trước");
            }

            sprint.Status = SprintStatus.Active;
            sprint.StartDate = DateTime.UtcNow;
            sprint.EndDate = sprint.StartDate.AddDays(14);

            await _context.SaveChangesAsync();

            var eventData = new
            {
                Id = sprint.Id,
                ProjectId = sprint.ProjectId,
                Name = sprint.Name,
                Goal = sprint.Goal,
                StartDate = sprint.StartDate,
                EndDate = sprint.EndDate,
                Status = sprint.Status.ToString()
            };
            await _eventPublisher.PublishAsync("sprint.started", eventData);

            return ApiResponse<SprintResponseDto>.Ok(MapToResponse(sprint), "Sprint đã bắt đầu");
        }

        public async Task<ApiResponse<SprintResponseDto>> CompleteSprintAsync(Guid projectId, Guid sprintId, string operatorUserId)
        {
            var sprint = await _context.Sprints
                .FirstOrDefaultAsync(s => s.Id == sprintId && s.ProjectId == projectId);

            if (sprint == null)
            {
                return ApiResponse<SprintResponseDto>.Fail("Không tìm thấy sprint");
            }

            // Kiểm tra quyền của người thực hiện
            var isAuthorized = await _permissionService.IsAuthorizedAsync(projectId, operatorUserId, MemberRole.Owner, MemberRole.Manager);
            if (!isAuthorized)
            {
                return ApiResponse<SprintResponseDto>.Fail("Bạn không có quyền quản trị sprint trong dự án này");
            }


            if (sprint.Status != SprintStatus.Active)
            {
                return ApiResponse<SprintResponseDto>.Fail("Chỉ có thể hoàn thành sprint đang Active");
            }

            sprint.Status = SprintStatus.Completed;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Sprint '{Name}' completed in project {ProjectId}", sprint.Name, projectId);

            return ApiResponse<SprintResponseDto>.Ok(MapToResponse(sprint), "Sprint đã hoàn thành");
        }

        private static SprintResponseDto MapToResponse(Sprint sprint)
        {
            return new SprintResponseDto
            {
                Id = sprint.Id,
                ProjectId = sprint.ProjectId,
                Name = sprint.Name,
                Goal = sprint.Goal,
                StartDate = sprint.StartDate,
                EndDate = sprint.EndDate,
                Status = sprint.Status.ToString(),
                CreatedAt = sprint.CreatedAt
            };
        }
    }
}
