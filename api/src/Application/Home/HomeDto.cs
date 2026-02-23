using System.Text.Json.Serialization;
using MovieSite.Application.Common;

namespace MovieSite.Application.Home;

public sealed record BannerDto(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("content_type")] string ContentType,
    [property: JsonPropertyName("content_id")] long ContentId,
    [property: JsonPropertyName("title_cn")] string TitleCn,
    [property: JsonPropertyName("poster_cos_key")] string? PosterCosKey,
    [property: JsonPropertyName("backdrop_cos_key")] string? BackdropCosKey,
    [property: JsonPropertyName("display_order")] int DisplayOrder
);

public sealed record HomeDto(
    [property: JsonPropertyName("banners")] List<BannerDto> Banners,
    [property: JsonPropertyName("hotMovies")] List<MediaCardDto> HotMovies,
    [property: JsonPropertyName("hotTv")] List<MediaCardDto> HotTv,
    [property: JsonPropertyName("hotAnimeCn")] List<MediaCardDto> HotAnimeCn,
    [property: JsonPropertyName("hotAnimeJp")] List<MediaCardDto> HotAnimeJp
);
