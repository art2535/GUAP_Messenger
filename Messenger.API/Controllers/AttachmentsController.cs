using Messenger.API.Responses;
using Messenger.Core.DTOs.Attachments;
using Messenger.Core.Interfaces;
using Messenger.Core.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Messenger.API.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Consumes("application/json")]
    [SwaggerTag("Контроллер для управления вложениями")]
    public class AttachmentsController : ControllerBase
    {
        private readonly IAttachmentService _attachmentService;

        public AttachmentsController(IAttachmentService attachmentService)
        {
            _attachmentService = attachmentService;
        }

        [HttpGet("{messageId}")]
        [SwaggerOperation(
            Summary = "Получение списка вложений по идентификатору сообщения",
            Description = "Возвращает все файлы (вложения), прикреплённые к указанному сообщению. Требуется действительный JWT-токен.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Список вложений успешно получен", typeof(GetAttachmentsSuccessResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Пользователь не авторизован")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Сообщение с указанным ID не найдено", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера", typeof(ErrorResponse))]
        public async Task<IActionResult> GetAttachmentsByMessageAsync(
            [SwaggerParameter(Description = "Уникальный идентификатор сообщения (GUID)")] Guid messageId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var attachents = await _attachmentService.GetAttachmentsByMessageIdAsync(messageId, cancellationToken);

                return Ok(new GetAttachmentsSuccessResponse
                {
                    IsSuccess = true,
                    Data = attachents
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    IsSuccess = false,
                    Error = ex.Message
                });
            }
        }

        [HttpPost("{messageId}")]
        [SwaggerOperation(
            Summary = "Добавление нового вложения к сообщению",
            Description = "Создаёт запись о новом файле (вложении), прикреплённом к сообщению. " +
                          "Требуется передать данные файла в теле запроса. " +
                          "Идентификатор вложения генерируется автоматически. Требуется действительный JWT-токен.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Вложение успешно добавлено", typeof(CreateAttachmentSuccessResponse))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Некорректные данные запроса", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Пользователь не авторизован")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Сообщение с указанным ID не найдено", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера", typeof(ErrorResponse))]
        public async Task<IActionResult> CreateAttachmentAsync(
            [SwaggerParameter(Description = "Уникальный идентификатор сообщения (GUID), к которому добавляется вложение")] 
            Guid messageId, 
            [FromBody] [SwaggerParameter(Description = "Данные нового вложения", Required = true)] 
            CreateAttachmentRequest request, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var attachment = new Attachment
                {
                    AttachmentId = Guid.NewGuid(),
                    MessageId = messageId,
                    FileName = request.FileName,
                    FileType = request.FileType,
                    SizeInBytes = request.SizeInBytes,
                    Url = request.Url
                };

                await _attachmentService.AddAttachmentAsync(attachment, cancellationToken);

                return Ok(new CreateAttachmentSuccessResponse
                {
                    IsSuccess = true,
                    Message = "Вложение успешно добавлено"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    IsSuccess = false,
                    Error = ex.Message
                });
            }
        }
    }
}
