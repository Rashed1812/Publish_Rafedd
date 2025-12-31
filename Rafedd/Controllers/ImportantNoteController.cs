using BLL.ServiceAbstraction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOS.Common;
using Shared.DTOS.Notes;
using Shared.Exceptions;
using System.Security.Claims;

namespace Rafedd.Controllers
{
    [ApiController]
    [Route("api/v1")]
    public class ImportantNoteController : ControllerBase
    {
        private readonly IImportantNoteService _noteService;
        private readonly ILogger<ImportantNoteController> _logger;

        public ImportantNoteController(IImportantNoteService noteService, ILogger<ImportantNoteController> logger)
        {
            _noteService = noteService;
            _logger = logger;
        }

        private string? GetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        // Employee endpoints - GET own important notes
        [HttpGet("employee/important-notes")]
        [Authorize(Roles = "Employee")]
        [ProducesResponseType(typeof(ApiResponse<List<ImportantNoteDto>>), 200)]
        public async Task<ActionResult<ApiResponse<List<ImportantNoteDto>>>> GetEmployeeNotes()
        {
            try
            {
                var employeeId = GetUserId();
                if (string.IsNullOrEmpty(employeeId))
                {
                    throw new UnauthorizedException("User ID not found");
                }

                var notes = await _noteService.GetEmployeeNotesAsync(employeeId);
                return Ok(ApiResponse<List<ImportantNoteDto>>.SuccessResponse(notes, "تم الحصول على الملاحظات بنجاح"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting employee notes");
                throw new BadRequestException("حدث خطأ أثناء الحصول على الملاحظات");
            }
        }

        // Employee endpoints - CREATE important note
        [HttpPost("employee/important-notes")]
        [Authorize(Roles = "Employee")]
        [ProducesResponseType(typeof(ApiResponse<ImportantNoteDto>), 200)]
        public async Task<ActionResult<ApiResponse<ImportantNoteDto>>> CreateNote([FromBody] CreateImportantNoteDto dto)
        {
            try
            {
                var employeeId = GetUserId();
                if (string.IsNullOrEmpty(employeeId))
                {
                    throw new UnauthorizedException("User ID not found");
                }

                var note = await _noteService.CreateNoteAsync(dto, employeeId);
                return Ok(ApiResponse<ImportantNoteDto>.SuccessResponse(note, "تم إنشاء الملاحظة بنجاح"));
            }
            catch (InvalidOperationException ex)
            {
                throw new BadRequestException(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating note");
                throw new BadRequestException("حدث خطأ أثناء إنشاء الملاحظة");
            }
        }

        // Employee endpoints - UPDATE important note
        [HttpPut("employee/important-notes/{id}")]
        [Authorize(Roles = "Employee")]
        [ProducesResponseType(typeof(ApiResponse<ImportantNoteDto>), 200)]
        public async Task<ActionResult<ApiResponse<ImportantNoteDto>>> UpdateNote(int id, [FromBody] UpdateImportantNoteDto dto)
        {
            try
            {
                var employeeId = GetUserId();
                if (string.IsNullOrEmpty(employeeId))
                {
                    throw new UnauthorizedException("User ID not found");
                }

                var note = await _noteService.UpdateNoteAsync(id, dto, employeeId);
                return Ok(ApiResponse<ImportantNoteDto>.SuccessResponse(note, "تم تحديث الملاحظة بنجاح"));
            }
            catch (InvalidOperationException ex)
            {
                throw new NotFoundException(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new UnauthorizedException(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating note {NoteId}", id);
                throw new BadRequestException("حدث خطأ أثناء تحديث الملاحظة");
            }
        }

        // Employee endpoints - DELETE important note
        [HttpDelete("employee/important-notes/{id}")]
        [Authorize(Roles = "Employee")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        public async Task<ActionResult<ApiResponse>> DeleteNote(int id)
        {
            try
            {
                var employeeId = GetUserId();
                if (string.IsNullOrEmpty(employeeId))
                {
                    throw new UnauthorizedException("User ID not found");
                }

                await _noteService.DeleteNoteAsync(id, employeeId);
                return Ok(ApiResponse.SuccessResponse("تم حذف الملاحظة بنجاح"));
            }
            catch (InvalidOperationException ex)
            {
                throw new NotFoundException(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new UnauthorizedException(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting note {NoteId}", id);
                throw new BadRequestException("حدث خطأ أثناء حذف الملاحظة");
            }
        }

        // Manager endpoints - GET important notes from employees
        [HttpGet("manager/important-notes")]
        [Authorize(Roles = "Manager")]
        [ProducesResponseType(typeof(ApiResponse<List<ImportantNoteDto>>), 200)]
        public async Task<ActionResult<ApiResponse<List<ImportantNoteDto>>>> GetManagerNotes()
        {
            try
            {
                var managerUserId = GetUserId();
                if (string.IsNullOrEmpty(managerUserId))
                {
                    throw new UnauthorizedException("User ID not found");
                }

                var notes = await _noteService.GetManagerNotesAsync(managerUserId);
                return Ok(ApiResponse<List<ImportantNoteDto>>.SuccessResponse(notes, "تم الحصول على ملاحظات الموظفين بنجاح"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting manager notes");
                throw new BadRequestException("حدث خطأ أثناء الحصول على الملاحظات");
            }
        }

        // GET specific note by ID (accessible by employee or their manager)
        [HttpGet("important-notes/{id}")]
        [Authorize(Roles = "Employee,Manager")]
        [ProducesResponseType(typeof(ApiResponse<ImportantNoteDto>), 200)]
        public async Task<ActionResult<ApiResponse<ImportantNoteDto>>> GetNoteById(int id)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    throw new UnauthorizedException("User ID not found");
                }

                var note = await _noteService.GetNoteByIdAsync(id, userId);
                return Ok(ApiResponse<ImportantNoteDto>.SuccessResponse(note, "تم الحصول على الملاحظة بنجاح"));
            }
            catch (InvalidOperationException ex)
            {
                throw new NotFoundException(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new UnauthorizedException(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting note {NoteId}", id);
                throw new BadRequestException("حدث خطأ أثناء الحصول على الملاحظة");
            }
        }
    }
}
