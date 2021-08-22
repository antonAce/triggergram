using System;
using System.Threading;
using System.Threading.Tasks;
using MediaPostEntity = Triggergram.Core.Data.Models.MediaPost;
using Triggergram.Core.Data.Context;
using Triggergram.Core.Services.Contracts;
using Triggergram.Core.Services.DTO;

namespace Triggergram.Core.Services.Implementation
{
    public class MediaPostService : IMediaPostService
    {
        private readonly TriggergramContext _context;
        private readonly IMediaContainer _container;
        private readonly IMediaConverter _converter;
        
        public MediaPostService(TriggergramContext context,
            IMediaContainer container,
            IMediaConverter converter)
        {
            _context = context;
            _container = container;
            _converter = converter;
        }
        
        public async Task<Guid> CreateMediaPost(MediaPostRecord mediaPostRecord, CancellationToken token)
        {
            var postId = Guid.NewGuid();
            var mediaFileName = $"{mediaPostRecord.AccountId}/{postId}.png";

            await using var convertedFileStream = await _converter.ConvertMediaFormatAsync(
                mediaPostRecord.MediaStream, token);
            await _container.SaveMediaAsync(mediaFileName, convertedFileStream, token);
            
            await _context.MediaPosts.AddAsync(new MediaPostEntity
            {
                Id = postId,
                Title = mediaPostRecord.Title,
                Description = mediaPostRecord.Description,
                CreatedAt = DateTime.Now,
                Views = 0,
                AccountId = mediaPostRecord.AccountId
            }, token);
            await _context.SaveChangesAsync(token);

            return postId;
        }
    }
}
