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
            Summary = "Получение вложений по идентификатору сообщения",
            Description = "Возвращает список всех файлов, прикреплённых к указанному сообщению. " +
                          "Требуется авторизация по JWT-токену.")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 401)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> GetAttachmentsByMessageAsync(Guid messageId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var attachents = await _attachmentService.GetAttachmentsByMessageIdAsync(messageId, cancellationToken);

                return Ok(new
                {
                    IsSuccess = true,
                    Data = attachents
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    IsSuccess = false,
                    Error = ex.Message
                });
            }
        }

        [HttpPost("{messageId}")]
        [SwaggerOperation(
            Summary = "Создание нового вложения",
            Description = "Добавляет новое вложение (файл) к сообщению. " +
                          "Необходимо передать идентификатор сообщения, имя файла, тип, размер и URL. " +
                          "Вложение создаётся с новым уникальным идентификатором.")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 401)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> CreateAttachmentAsync(Guid messageId, [FromBody] CreateAttachmentRequest request, 
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

                return Ok(new
                {
                    IsSuccess = true,
                    Message = "Вложение успешно добавлено"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    IsSuccess = false,
                    Error = ex.Message
                });
            }
        }
    }
}
