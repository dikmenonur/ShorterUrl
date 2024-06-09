using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ShorterUrl.Core.Domain;
using ShorterUrl.Core.Helpers;

namespace ShorterUrl.Tools.Functions
{
    public class UrlArchive
    {

        private readonly ILogger _logger;
        private readonly ShortenerSettings _settings;

        public UrlArchive(ILoggerFactory loggerFactory, ShortenerSettings settings)
        {
            _logger = loggerFactory.CreateLogger<UrlList>();
            _settings = settings;
        }

        [Function("UrlArchive")]
        public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "api/UrlArchive")] HttpRequestData req,
        ExecutionContext context)
        {
            _logger.LogInformation($"HTTP trigger - UrlArchive");

            string userId = string.Empty;
            ShortUrlEntity input;
            ShortUrlEntity result;
            try
            {
                // Validation of the inputs
                if (req == null)
                {
                    return req.CreateResponse(HttpStatusCode.NotFound);
                }

                using (var reader = new StreamReader(req.Body))
                {
                    var body = await reader.ReadToEndAsync();
                    input = JsonSerializer.Deserialize<ShortUrlEntity>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (input == null)
                    {
                        return req.CreateResponse(HttpStatusCode.NotFound);
                    }
                }

                StorageTableHelper stgHelper = new StorageTableHelper(_settings.DataStorage);

                result = await stgHelper.ArchiveShortUrlEntity(input);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error was encountered.");
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteAsJsonAsync(new { ex.Message });
                return badRequest;
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result);
            return response;
        }
    }
}
