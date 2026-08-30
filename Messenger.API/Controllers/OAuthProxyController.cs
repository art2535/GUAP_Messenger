using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Messenger.API.Controllers
{
    /// <summary>
    /// Контроллер прокси OAuth 2.0 token endpoint для Scalar UI.
    /// Обходит CORS при обмене authorization_code/refresh_token с SSO ГУАП.
    /// </summary>
    [ApiController]
    [Route("oauth")]
    [AllowAnonymous]
    [ApiVersionNeutral]
    public class OAuthProxyController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public OAuthProxyController(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost("token")]
        [EndpointName("OAuthProxyToken")]
        [EndpointSummary("OAuth token proxy")]
        [EndpointDescription("Проксирует token request на Keycloak SSO ГУАП. " +
            "Нужен для Scalar UI: обход CORS и поддержка authorization_code + refresh_token.")]
        [Consumes("application/x-www-form-urlencoded")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        public async Task<IActionResult> Token(CancellationToken cancellationToken)
        {
            var authority = $"{_configuration["AzureAd:Instance"]?.TrimEnd('/')}/{_configuration["AzureAd:TenantId"]}";
            var tokenUrl = $"{authority}/protocol/openid-connect/token";

            var form = await Request.ReadFormAsync(cancellationToken);
            var pairs = form
                .SelectMany(kv => kv.Value.Select(v => new KeyValuePair<string, string>(kv.Key, v ?? string.Empty)))
                .ToList();

            using var content = new FormUrlEncodedContent(pairs);
            var client = _httpClientFactory.CreateClient();

            using var response = await client.PostAsync(tokenUrl, content, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(responseBody))
            {
                return StatusCode((int)response.StatusCode, new
                {
                    error = "empty_response",
                    error_description = "Keycloak returned an empty body"
                });
            }

            return new ContentResult
            {
                StatusCode = (int)response.StatusCode,
                Content = responseBody,
                ContentType = "application/json; charset=utf-8"
            };
        }
    }
}
