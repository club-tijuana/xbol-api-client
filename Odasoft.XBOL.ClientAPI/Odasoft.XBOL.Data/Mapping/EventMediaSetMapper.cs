using Microsoft.EntityFrameworkCore;
using Odasoft.XBOL.Commons.Enums;
using Odasoft.XBOL.Data.Queries;
using Odasoft.XBOL.DTO;
using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.Data.Mapping
{
    public static class EventMediaSetMapper
    {
        public static async Task<Dictionary<long, EventMediaSetResponse>> GetEventMediaSetsAsync(
            DbContext dbContext,
            IReadOnlyCollection<long> eventIds)
        {
            if (eventIds.Count == 0)
            {
                return [];
            }

            var media = await dbContext.Set<Media>()
                .AvailableBlobMedia()
                .Where(x =>
                    x.ReferenceType == ClientSaleType.Event &&
                    eventIds.Contains(x.ReferenceId))
                .OrderBy(x => x.ReferenceId)
                .ThenBy(x => x.MediaType)
                .ThenBy(x => x.Order)
                .ThenBy(x => x.Id)
                .Select(x => new
                {
                    x.ReferenceId,
                    Media = new MediaResponse
                    {
                        Id = x.Id,
                        Url = x.BlobAsset.Url,
                        ContentType = x.BlobAsset.ContentType,
                        FileName = x.BlobAsset.FileName,
                        MediaType = x.MediaType,
                        Order = x.Order
                    }
                })
                .ToListAsync();

            return media
                .GroupBy(x => x.ReferenceId)
                .ToDictionary(
                    x => x.Key,
                    x => CreateMediaSet(x.Select(item => item.Media)));
        }

        public static EventMediaSetResponse CreateMediaSet(IEnumerable<MediaResponse> media)
        {
            return new EventMediaSetResponse
            {
                Banner = media
                    .Where(x => x.MediaType == ClientMediaType.Banner)
                    .OrderBy(x => x.Order)
                    .ThenBy(x => x.Id)
                    .FirstOrDefault(),
                Logo = media
                    .Where(x => x.MediaType == ClientMediaType.Logo)
                    .OrderBy(x => x.Order)
                    .ThenBy(x => x.Id)
                    .FirstOrDefault(),
                Sponsors = media
                    .Where(x => x.MediaType == ClientMediaType.Sponsor)
                    .OrderBy(x => x.Order)
                    .ThenBy(x => x.Id)
                    .ToList(),
                Gallery = media
                    .Where(x => x.MediaType == ClientMediaType.Gallery)
                    .OrderBy(x => x.Order)
                    .ThenBy(x => x.Id)
                    .ToList()
            };
        }

        public static MediaResponse CreateMediaResponse(Media media)
        {
            return new MediaResponse
            {
                Id = media.Id,
                Url = media.BlobAsset.Url,
                ContentType = media.BlobAsset.ContentType,
                FileName = media.BlobAsset.FileName,
                MediaType = media.MediaType,
                Order = media.Order
            };
        }
    }
}
