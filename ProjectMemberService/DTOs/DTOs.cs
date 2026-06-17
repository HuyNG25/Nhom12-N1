using System.ComponentModel.DataAnnotations;
using ProjectMemberService.Models;

namespace ProjectMemberService.DTOs
{

    // ===== Project DTOs =====
    public class CreateProjectDto
    {
        [Required(ErrorMessage = "Tên dự án là bắt buộc")]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Ngày bắt đầu là bắt buộc")]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Mã màu phải có dạng #RRGGBB")]
        public string? Color { get; set; }
    }

    public class UpdateProjectDto
    {
        [Required(ErrorMessage = "Tên dự án là bắt buộc")]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Mã màu phải có dạng #RRGGBB")]
        public string? Color { get; set; }

        public ProjectStatus Status { get; set; }
    }

    public class ProjectResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Color { get; set; }
        public string Status { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int MemberCount { get; set; }
        public int SprintCount { get; set; }
    }

    public class ProjectDetailResponseDto : ProjectResponseDto
    {
        public List<MemberResponseDto> Members { get; set; } = new();
        public List<SprintResponseDto> Sprints { get; set; } = new();
        public List<MilestoneResponseDto> Milestones { get; set; } = new();
    }

    // ===== Member DTOs =====
    public class AddMemberDto
    {
        [Required(ErrorMessage = "UserId là bắt buộc")]
        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên hiển thị là bắt buộc")]
        [MaxLength(200)]
        public string DisplayName { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string? Email { get; set; }

        public MemberRole Role { get; set; } = MemberRole.Member;
    }

    public class UpdateMemberRoleDto
    {
        [Required]
        public MemberRole Role { get; set; }
    }

    public class MemberResponseDto
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string Role { get; set; } = string.Empty;
        public DateTime JoinedAt { get; set; }
    }

    // ===== Sprint DTOs =====
    public class CreateSprintDto
    {
        [Required(ErrorMessage = "Tên sprint là bắt buộc")]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Goal { get; set; }

        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Nếu không truyền, mặc định EndDate = StartDate + 14 ngày
        /// </summary>
        public DateTime? EndDate { get; set; }
    }

    public class UpdateSprintDto
    {
        [Required(ErrorMessage = "Tên sprint là bắt buộc")]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Goal { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class SprintResponseDto
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Goal { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    // ===== Milestone DTOs =====
    public class CreateMilestoneDto
    {
        [Required(ErrorMessage = "Tiêu đề milestone là bắt buộc")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Ngày đến hạn là bắt buộc")]
        public DateTime DueDate { get; set; }
    }

    public class UpdateMilestoneDto
    {
        [Required(ErrorMessage = "Tiêu đề milestone là bắt buộc")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Ngày đến hạn là bắt buộc")]
        public DateTime DueDate { get; set; }

        public bool IsCompleted { get; set; }
    }

    public class MilestoneResponseDto
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime DueDate { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ===== Common =====
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }

        public static ApiResponse<T> Ok(T data, string message = "Thành công")
            => new() { Success = true, Message = message, Data = data };

        public static ApiResponse<T> Fail(string message)
            => new() { Success = false, Message = message };
    }
}
