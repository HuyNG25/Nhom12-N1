using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectMemberService.DTOs;
using ProjectMemberService.Services;

namespace ProjectMemberService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ProjectsController : ControllerBase
    {
        private readonly IProjectService _projectService;

        public ProjectsController(IProjectService projectService)
        {
            _projectService = projectService;
        }

        /// <summary>
        /// Lấy UserId từ JWT token hoặc header
        /// </summary>
        private string GetUserId()
        {
            return Request.Headers["X-User-Id"].FirstOrDefault() ?? "anonymous";
        }

        /// <summary>
        /// Tạo dự án mới
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<ProjectResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<ProjectResponseDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateProjectDto dto)
        {
            var userId = GetUserId();
            var result = await _projectService.CreateAsync(dto, userId);

            if (!result.Success)
                return BadRequest(result);

            return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result);
        }

        /// <summary>
        /// Lấy danh sách dự án
        /// </summary>
        /// <param name="myProjects">Nếu true, chỉ lấy dự án mà user là thành viên</param>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<ProjectResponseDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] bool myProjects = false)
        {
            var userId = myProjects ? GetUserId() : null;
            var result = await _projectService.GetAllAsync(userId);
            return Ok(result);
        }

        /// <summary>
        /// Lấy chi tiết dự án theo ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<ProjectDetailResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<ProjectDetailResponseDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var userId = GetUserId();
            var result = await _projectService.GetByIdAsync(id, userId);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }

        /// <summary>
        /// Cập nhật dự án
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<ProjectResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<ProjectResponseDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<ProjectResponseDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProjectDto dto)
        {
            var userId = GetUserId();
            var result = await _projectService.UpdateAsync(id, dto, userId);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Xóa dự án
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = GetUserId();
            var result = await _projectService.DeleteAsync(id, userId);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
    }
}
