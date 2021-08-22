using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using Triggergram.Core.Services.Contracts;
using Triggergram.Core.Services.DTO;

namespace Triggergram.Core
{
    public class MediaPost
    {
        private readonly IMediaPostService _mediaPostService;
        private readonly string[] _allowedExtensions = { "image/jpeg", "image/png" };
        
        public MediaPost(IMediaPostService mediaPostService)
        {
            _mediaPostService = mediaPostService;
        }
        
        [FunctionName("MediaPost")]
        public async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log,
            CancellationToken token)
        {
            log.LogInformation($"Requested {req.Method} /api/MediaPost");

            return req.Method switch
            {
                "GET" => new NotFoundResult(),
                "POST" => await SaveMediaToStorage(req, token),
                _ => new NotFoundResult()
            };
        }

        private async Task<IActionResult> SaveMediaToStorage(HttpRequest req, CancellationToken token)
        {
            if (!req.HasFormContentType)
                return new BadRequestObjectResult(new { Message = "Incorrect input data." });

            if (req.Form.Files.GetFile("media") is null)
                return new BadRequestObjectResult(new { Message = "Media content is missing." });

            if (!_allowedExtensions.Contains(req.Form.Files["media"].ContentType))
                return new BadRequestObjectResult(new { Message = "Only *.jpg and *.png media types allowed." });

            await using var fileStream = new MemoryStream();
            await req.Form.Files["media"].CopyToAsync(fileStream, token);
            var postId = await _mediaPostService.CreateMediaPost(new MediaPostRecord
            {
                Title = req.Form["title"],
                Description = req.Form["description"],
                MediaStream = fileStream,
                AccountId = Guid.Parse(req.Form["accountId"])
            }, token);

            return new OkObjectResult(new { Message = postId });
        }
    }
}
