using Microsoft.AspNetCore.Mvc;
using ProjectMemberService.DTOs;
using ProjectMemberService.Models;
using ProjectMemberService.Services;

namespace ProjectMemberService.Controllers
{
    [ApiController]
    [Route("api/projects/{projectId}/[controller]")]
    [Produces("application/json")]
    public class MembersController : ControllerBase
    {
        private readonly IMemberService _memberService;

        public MembersController(IMemberService memberService)
        {
            _memberService = memberService;
        }

        private string GetUserId()
        {
            return Request.Headers["X-User-Id"].FirstOrDefault() ?? "anonymous";
        }

        /// <summary>
        /// Thêm thành viên vào dự án
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<MemberResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<MemberResponseDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddMember(Guid projectId, [FromBody] AddMemberDto dto)
        {
            var operatorUserId = GetUserId();
            var result = await _memberService.AddMemberAsync(projectId, dto, operatorUserId);

            if (!result.Success)
                return BadRequest(result);

            return CreatedAtAction(nameof(GetMembers), new { projectId }, result);
        }

        /// <summary>
        /// Lấy danh sách thành viên của dự án
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<MemberResponseDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<List<MemberResponseDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMembers(Guid projectId)
        {
            var userId = GetUserId();
            var result = await _memberService.GetMembersAsync(projectId, userId);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }

        /// <summary>
        /// Cập nhật vai trò của thành viên
        /// </summary>
        [HttpPut("{memberId}")]
        [ProducesResponseType(typeof(ApiResponse<MemberResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<MemberResponseDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateRole(Guid projectId, Guid memberId, [FromBody] UpdateMemberRoleDto dto)
        {
            var operatorUserId = GetUserId();
            var result = await _memberService.UpdateRoleAsync(projectId, memberId, dto, operatorUserId);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Xóa thành viên khỏi dự án
        /// </summary>
        [HttpDelete("{memberId}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RemoveMember(Guid projectId, Guid memberId)
        {
            var operatorUserId = GetUserId();
            var result = await _memberService.RemoveMemberAsync(projectId, memberId, operatorUserId);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// N3 gọi API này để kiểm tra role của một user trong project
        /// Trả về 403 nếu không có trong dự án. Trả về Int Role (0=Owner, 1=Manager, 2=Member, 3=Viewer)
        /// </summary>
        [HttpGet("{userId}/role-check")]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CheckRole(Guid projectId, string userId)
        {
            // Inject _permissionService from DI directly or we can just use _memberService if it has a way.
            // Wait, we need IPermissionService to get the member. We don't have it in the constructor of MembersController.
            // I'll get it from HttpContext.RequestServices
            var permissionService = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            
            var isSystemAdmin = await permissionService.IsSystemAdminAsync(userId);
            if (isSystemAdmin)
            {
                return Ok((int)MemberRole.Owner); // Admin has Owner privileges implicitly
            }

            var member = await permissionService.GetMemberAsync(projectId, userId);
            if (member == null)
            {
                return StatusCode(403, "User không thuộc dự án này");
            }

            return Ok((int)member.Role);
        }
    }
}
