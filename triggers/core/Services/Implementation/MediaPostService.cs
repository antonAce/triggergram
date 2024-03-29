using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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

        public async Task<IEnumerable<Guid>> GetMediaPostIdsByAccount(Guid accountId, CancellationToken token)
        {
            return await _context.MediaPosts.Where(m => m.AccountId == accountId)
                .Select(m => m.Id).ToArrayAsync(token);
        }
        
        public async Task<Guid> CreateMediaPostAsync(MediaPostRecord mediaPostRecord, CancellationToken token)
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

        public async Task<Stream> GetMediaAsync(Guid postId, CancellationToken token)
        {
            var media = await _context.MediaPosts
                .FirstOrDefaultAsync(m => m.Id == postId, token);

            return await _container.DownloadMediaAsync($"{media.AccountId}/{media.Id}.png", token);
        }

        public async Task<MediaPostView> GetMediaPostContentAsync(Guid postId, CancellationToken token)
        {
            var mediaPost = await _context.MediaPosts.FirstOrDefaultAsync(m => m.Id == postId, token);

            return new MediaPostView
            {
                Id = mediaPost.Id.ToString(),
                Title = mediaPost.Title,
                Description = mediaPost.Description,
                CreatedAt = mediaPost.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                Views = mediaPost.Views,
                Account = mediaPost.AccountId.ToString()
            };
        }
    }
}
