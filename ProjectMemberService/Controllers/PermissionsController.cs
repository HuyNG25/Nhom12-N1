using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectMemberService.Data;
using ProjectMemberService.DTOs;
using ProjectMemberService.Models;
using ProjectMemberService.Services;

namespace ProjectMemberService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class PermissionsController : ControllerBase
    {
        private readonly ProjectDbContext _context;
        private readonly IPermissionService _permissionService;

        public PermissionsController(ProjectDbContext context, IPermissionService permissionService)
        {
            _context = context;
            _permissionService = permissionService;
        }

        private string GetUserId()
        {
            return Request.Headers["X-User-Id"].FirstOrDefault() ?? "anonymous";
        }

        /// <summary>
        /// Lấy danh sách System Admins
        /// </summary>
        [HttpGet("Admins")]
        [ProducesResponseType(typeof(ApiResponse<List<string>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAdmins()
        {
            var operatorId = GetUserId();
            if (!await _permissionService.IsSystemAdminAsync(operatorId))
            {
                return StatusCode(403, ApiResponse<List<string>>.Fail("Chỉ System Admin mới xem được danh sách này"));
            }

            var admins = await _context.SystemAdmins.Select(a => a.UserId).ToListAsync();
            return Ok(ApiResponse<List<string>>.Ok(admins));
        }

        /// <summary>
        /// Cấp quyền System Admin cho một user
        /// </summary>
        [HttpPost("Admins/{userId}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> AddAdmin(string userId)
        {
            var operatorId = GetUserId();
            if (!await _permissionService.IsSystemAdminAsync(operatorId))
            {
                return StatusCode(403, ApiResponse<bool>.Fail("Chỉ System Admin mới có quyền cấp phép"));
            }

            if (await _context.SystemAdmins.AnyAsync(a => a.UserId == userId))
            {
                return Ok(ApiResponse<bool>.Ok(true, "User đã là Admin từ trước"));
            }

            _context.SystemAdmins.Add(new SystemAdmin
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<bool>.Ok(true, "Đã cấp quyền Admin thành công"));
        }

        /// <summary>
        /// Thu hồi quyền System Admin
        /// </summary>
        [HttpDelete("Admins/{userId}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> RemoveAdmin(string userId)
        {
            var operatorId = GetUserId();
            if (!await _permissionService.IsSystemAdminAsync(operatorId))
            {
                return StatusCode(403, ApiResponse<bool>.Fail("Chỉ System Admin mới có quyền thu hồi"));
            }

            var admin = await _context.SystemAdmins.FirstOrDefaultAsync(a => a.UserId == userId);
            if (admin == null)
            {
                return Ok(ApiResponse<bool>.Ok(true, "User không phải là Admin"));
            }

            // Ngăn chặn xóa chính mình nếu là admin cuối cùng? (Có thể bỏ qua, nhưng an toàn hơn thì nên có)
            if (operatorId == userId)
            {
                var adminCount = await _context.SystemAdmins.CountAsync();
                if (adminCount <= 1)
                {
                    return BadRequest(ApiResponse<bool>.Fail("Không thể thu hồi quyền của Admin duy nhất"));
                }
            }

            _context.SystemAdmins.Remove(admin);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<bool>.Ok(true, "Đã thu hồi quyền Admin thành công"));
        }
    }
}
