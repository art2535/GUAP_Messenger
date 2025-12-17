using Messenger.API.Responses;
using Messenger.Core.DTOs.Reactions;
using Messenger.Core.Interfaces;
using Messenger.Core.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace Messenger.API.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Consumes("application/json")]
    [SwaggerTag("Контроллер для управления реакциями на сообщения")]
    public class ReactionsController : ControllerBase
    {
        private readonly IReactionService _reactionService;

        public ReactionsController(IReactionService reactionService)
        {
            _reactionService = reactionService;
        }

        [HttpGet("{messageId}")]
        [SwaggerOperation(
            Summary = "Получить все реакции на сообщение",
            Description = "Возвращает список всех реакций (эмодзи) на указанное сообщение, включая информацию о пользовнике и тип реакции.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Реакции успешно получены", typeof(GetReactionsSuccessResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Пользователь не авторизован")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Сообщение не найдено или реакций нет", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера", typeof(ErrorResponse))]
        public async Task<IActionResult> GetReactionsByMessageAsync(
            [SwaggerParameter(Description = "Идентификатор сообщения (GUID)")] Guid messageId, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var reactions = await _reactionService.GetReactionsByMessageIdAsync(messageId, cancellationToken);

                return Ok(new GetReactionsSuccessResponse
                {
                    IsSuccess = true,
                    Data = reactions
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
            Summary = "Добавить реакцию на сообщение",
            Description = "Добавляет реакцию (эмодзи) от имени текущего авторизованного пользователя к указанному сообщению.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Реакция успешно добавлена", typeof(AddReactionSuccessResponse))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Некорректный тип реакции", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Пользователь не авторизован")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Сообщение не найдено", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера", typeof(ErrorResponse))]
        public async Task<IActionResult> AddReactionAsync(
            [SwaggerParameter(Description = "Идентификатор сообщения (GUID)")] Guid messageId, 
            [FromBody] [SwaggerParameter(Description = "Данные реакции", Required = true)] CreateReactionRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                var reaction = new Reaction
                {
                    ReactionId = Guid.NewGuid(),
                    MessageId = messageId,
                    UserId = userId,
                    ReactionType = request.ReactionType
                };

                await _reactionService.AddReactionAsync(reaction, cancellationToken);

                return Ok(new AddReactionSuccessResponse
                {
                    IsSuccess = true,
                    Message = "Статус пользователя обновлен"
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ErrorResponse
                {
                    IsSuccess = false,
                    Error = ex.Message
                });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new ErrorResponse
                {
                    IsSuccess = false,
                    Error = "Сообщение не найдено"
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

        [HttpDelete("{messageId}")]
        [SwaggerOperation(
            Summary = "Удалить свою реакцию с сообщения",
            Description = "Удаляет реакцию текущего авторизованного пользователя с указанного сообщения. " +
                          "Если у пользователя несколько реакций — удаляется только одна (обычно последняя).")]
        [SwaggerResponse(StatusCodes.Status200OK, "Реакция успешно удалена", typeof(DeleteReactionSuccessResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Пользователь не авторизован")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Реакция пользователя не найдена", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера", typeof(ErrorResponse))]
        public async Task<IActionResult> DeleteReactionAsync(Guid messageId, CancellationToken cancellationToken = default)
        {
            try
            {
                var reaction = await _reactionService.GetReactionsByMessageIdAsync(messageId, cancellationToken);

                if (reaction == null)
                {
                    return NotFound(new ErrorResponse
                    {
                        IsSuccess = false,
                        Error = "Реакция на сообщение не найдена"
                    });
                }

                await _reactionService.DeleteReactionAsync(messageId, cancellationToken);

                return Ok(new DeleteReactionSuccessResponse
                {
                    IsSuccess = true,
                    Message = "Реакция успешно удалена"
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
