using Microsoft.AspNetCore.Mvc;
using ProjectMemberService.DTOs;
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
            var result = await _memberService.GetMembersAsync(projectId);

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
    }
}
