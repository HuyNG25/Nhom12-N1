using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProjectMemberService.DTOs;
using ProjectMemberService.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectMemberService.Controllers
{
    [ApiController]
    [Route("api/projects/{projectId}/[controller]")]
    [Produces("application/json")]
    public class MilestonesController : ControllerBase
    {
        private readonly IMilestoneService _milestoneService;

        public MilestonesController(IMilestoneService milestoneService)
        {
            _milestoneService = milestoneService;
        }

        private string GetUserId()
        {
            return Request.Headers["X-User-Id"].FirstOrDefault() ?? "anonymous";
        }

        /// <summary>
        /// Tạo milestone mới cho dự án
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<MilestoneResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<MilestoneResponseDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create(Guid projectId, [FromBody] CreateMilestoneDto dto)
        {
            var operatorUserId = GetUserId();
            var result = await _milestoneService.CreateAsync(projectId, dto, operatorUserId);

            if (!result.Success)
                return BadRequest(result);

            return CreatedAtAction(nameof(GetById), new { projectId, milestoneId = result.Data!.Id }, result);
        }

        /// <summary>
        /// Lấy danh sách milestone của dự án
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<MilestoneResponseDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<List<MilestoneResponseDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAll(Guid projectId)
        {
            var result = await _milestoneService.GetAllAsync(projectId);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }

        /// <summary>
        /// Lấy chi tiết milestone
        /// </summary>
        [HttpGet("{milestoneId}")]
        [ProducesResponseType(typeof(ApiResponse<MilestoneResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<MilestoneResponseDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid projectId, Guid milestoneId)
        {
            var result = await _milestoneService.GetByIdAsync(projectId, milestoneId);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }

        /// <summary>
        /// Cập nhật milestone
        /// </summary>
        [HttpPut("{milestoneId}")]
        [ProducesResponseType(typeof(ApiResponse<MilestoneResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<MilestoneResponseDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(Guid projectId, Guid milestoneId, [FromBody] UpdateMilestoneDto dto)
        {
            var operatorUserId = GetUserId();
            var result = await _milestoneService.UpdateAsync(projectId, milestoneId, dto, operatorUserId);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Xóa milestone
        /// </summary>
        [HttpDelete("{milestoneId}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Delete(Guid projectId, Guid milestoneId)
        {
            var operatorUserId = GetUserId();
            var result = await _milestoneService.DeleteAsync(projectId, milestoneId, operatorUserId);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
    }
}
