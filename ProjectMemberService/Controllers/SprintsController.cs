using Microsoft.AspNetCore.Mvc;
using ProjectMemberService.DTOs;
using ProjectMemberService.Services;

namespace ProjectMemberService.Controllers
{
    [ApiController]
    [Route("api/projects/{projectId}/[controller]")]
    [Produces("application/json")]
    public class SprintsController : ControllerBase
    {
        private readonly ISprintService _sprintService;

        public SprintsController(ISprintService sprintService)
        {
            _sprintService = sprintService;
        }

        private string GetUserId()
        {
            return Request.Headers["X-User-Id"].FirstOrDefault() ?? "anonymous";
        }

        /// <summary>
        /// Tạo sprint mới cho dự án
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<SprintResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<SprintResponseDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create(Guid projectId, [FromBody] CreateSprintDto dto)
        {
            var operatorUserId = GetUserId();
            var result = await _sprintService.CreateAsync(projectId, dto, operatorUserId);

            if (!result.Success)
                return BadRequest(result);

            return CreatedAtAction(nameof(GetById), new { projectId, sprintId = result.Data!.Id }, result);
        }

        /// <summary>
        /// Lấy danh sách sprint của dự án
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<SprintResponseDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<List<SprintResponseDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAll(Guid projectId)
        {
            var userId = GetUserId();
            var result = await _sprintService.GetAllAsync(projectId, userId);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }

        /// <summary>
        /// Lấy chi tiết sprint
        /// </summary>
        [HttpGet("{sprintId}")]
        [ProducesResponseType(typeof(ApiResponse<SprintResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<SprintResponseDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid projectId, Guid sprintId)
        {
            var userId = GetUserId();
            var result = await _sprintService.GetByIdAsync(projectId, sprintId, userId);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }

        /// <summary>
        /// Cập nhật sprint
        /// </summary>
        [HttpPut("{sprintId}")]
        [ProducesResponseType(typeof(ApiResponse<SprintResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<SprintResponseDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(Guid projectId, Guid sprintId, [FromBody] UpdateSprintDto dto)
        {
            var operatorUserId = GetUserId();
            var result = await _sprintService.UpdateAsync(projectId, sprintId, dto, operatorUserId);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Bắt đầu sprint (chuyển từ Planning sang Active)
        /// </summary>
        [HttpPut("{sprintId}/start")]
        [ProducesResponseType(typeof(ApiResponse<SprintResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<SprintResponseDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> StartSprint(Guid projectId, Guid sprintId)
        {
            var operatorUserId = GetUserId();
            var result = await _sprintService.StartSprintAsync(projectId, sprintId, operatorUserId);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Hoàn thành sprint (chuyển từ Active sang Completed)
        /// </summary>
        [HttpPut("{sprintId}/complete")]
        [ProducesResponseType(typeof(ApiResponse<SprintResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<SprintResponseDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CompleteSprint(Guid projectId, Guid sprintId)
        {
            var operatorUserId = GetUserId();
            var result = await _sprintService.CompleteSprintAsync(projectId, sprintId, operatorUserId);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
    }
}
