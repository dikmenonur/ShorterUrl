using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ShorterUrl.Core.Domain;
using ShorterUrl.Core.Helpers;
using ShorterUrl.Core.Messages;

namespace ShorterUrl.Tools.Functions
{
    public class UrlList
    {

        private readonly ILogger _logger;
        private readonly ShortenerSettings _settings;

        public UrlList(ILoggerFactory loggerFactory, ShortenerSettings settings)
        {
            _logger = loggerFactory.CreateLogger<UrlList>();
            _settings = settings;
        }

        [Function("UrlList")]
        public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/UrlList")] HttpRequestData req, ExecutionContext context)
        {
            _logger.LogInformation($"Starting UrlList...");

            var result = new ListResponse();
            string userId = string.Empty;

            StorageTableHelper stgHelper = new StorageTableHelper(_settings.DataStorage);

            try
            {
                result.UrlList = await stgHelper.GetAllShortUrlEntities();
                result.UrlList = result.UrlList.Where(p => !(p.IsArchived ?? false)).ToList();
                var host = string.IsNullOrEmpty(_settings.CustomDomain) ? req.Url.Host : _settings.CustomDomain;
                foreach (ShortUrlEntity url in result.UrlList)
                {
                    url.ShortUrl = Utility.GetShortUrl(host, url.RowKey);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error was encountered.");
                var badres = req.CreateResponse(HttpStatusCode.BadRequest);
                await badres.WriteAsJsonAsync(new { ex.Message });
                return badres;
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result);
            return response;
        }
    }
}
