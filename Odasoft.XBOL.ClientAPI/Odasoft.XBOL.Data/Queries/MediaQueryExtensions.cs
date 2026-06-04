using Odasoft.XBOL.Commons.Enums;
using Odasoft.XBOL.DTO;
using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.Data.Queries;

public static class MediaQueryExtensions
{
    public static IQueryable<Media> AvailableBlobMedia(this IQueryable<Media> query)
    {
        return query.Where(media =>
            media.DeletedAt == null
            && media.BlobAsset.DeletedAt == null
            && media.BlobAsset.Status == BlobAssetStatus.Available);
    }

    public static IQueryable<MediaResponse> SelectMediaResponse(this IQueryable<Media> query)
    {
        return query.Select(media => new MediaResponse
        {
            Id = media.Id,
            Url = media.BlobAsset.Url,
            ContentType = media.BlobAsset.ContentType,
            FileName = media.BlobAsset.FileName,
            MediaType = media.MediaType,
            Order = media.Order
        });
    }
}
