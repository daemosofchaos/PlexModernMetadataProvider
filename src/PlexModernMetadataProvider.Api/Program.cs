using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using PlexModernMetadataProvider.Api.Models;
using PlexModernMetadataProvider.Api.Options;
using PlexModernMetadataProvider.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = null;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.WriteIndented = false;
});

builder.Services
    .AddOptions<ProviderOptions>()
    .Bind(builder.Configuration.GetSection(ProviderOptions.SectionName));

builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<ITmdbClient, TmdbClient>();
builder.Services.AddHttpClient<OmdbMovieSource>();
builder.Services.AddHttpClient<TvMazeTvSource>();
builder.Services.AddSingleton<FilenameParser>();
builder.Services.AddSingleton<RankingService>();
builder.Services.AddSingleton<PlexMapper>();
builder.Services.AddScoped<TmdbMovieSource>();
builder.Services.AddScoped<IMovieMetadataSource>(serviceProvider => serviceProvider.GetRequiredService<TmdbMovieSource>());
builder.Services.AddScoped<IMovieMetadataSource>(serviceProvider => serviceProvider.GetRequiredService<OmdbMovieSource>());
builder.Services.AddScoped<TmdbTvSource>();
builder.Services.AddScoped<ITvMetadataSource>(serviceProvider => serviceProvider.GetRequiredService<TmdbTvSource>());
builder.Services.AddScoped<ITvMetadataSource>(serviceProvider => serviceProvider.GetRequiredService<TvMazeTvSource>());
builder.Services.AddScoped<MetadataSourceRegistry>();
builder.Services.AddScoped<MatchService>();
builder.Services.AddScoped<MetadataService>();

var app = builder.Build();

app.Use(async (context, next) =>
{
    app.Logger.LogInformation("{Method} {Path}", context.Request.Method, context.Request.Path);
    await next();
});

app.MapGet("/health", (IOptions<ProviderOptions> options) => Results.Ok(new
{
    ok = true,
    service = "plex-modern-metadata-provider-dotnet",
    movieSources = options.Value.MovieSourceOrder,
    tvSources = options.Value.TvSourceOrder
}));

app.MapGet(ProviderDefinitions.MovieBasePath, () => Results.Ok(ProviderDefinitions.CreateMovieProvider()));
app.MapPost($"{ProviderDefinitions.MovieBasePath}{ProviderDefinitions.MatchPath}",
    async Task<IResult> (MatchRequest request, MatchService service, HttpRequest httpRequest, IOptions<ProviderOptions> options, CancellationToken cancellationToken) =>
    {
        try
        {
            var result = await service.MatchAsync(request, BuildContext(httpRequest, options.Value), cancellationToken);
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = "Movie match failed", message = ex.Message });
        }
    });

app.MapGet($"{ProviderDefinitions.MovieBasePath}{ProviderDefinitions.MetadataPath}/{{ratingKey}}",
    async Task<IResult> (string ratingKey, MetadataService service, HttpRequest httpRequest, IOptions<ProviderOptions> options, CancellationToken cancellationToken) =>
    {
        try
        {
            var result = await service.GetMovieMetadataAsync(ratingKey, BuildContext(httpRequest, options.Value), cancellationToken);
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            return Results.NotFound(new { error = "Movie metadata not found", message = ex.Message });
        }
    });

app.MapGet($"{ProviderDefinitions.MovieBasePath}{ProviderDefinitions.MetadataPath}/{{ratingKey}}/images",
    async Task<IResult> (string ratingKey, MetadataService service, HttpRequest httpRequest, IOptions<ProviderOptions> options, CancellationToken cancellationToken) =>
    {
        try
        {
            var result = await service.GetMovieImagesAsync(ratingKey, BuildContext(httpRequest, options.Value), cancellationToken);
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            return Results.NotFound(new { error = "Movie images not found", message = ex.Message });
        }
    });

app.MapGet($"{ProviderDefinitions.MovieBasePath}{ProviderDefinitions.MetadataPath}/{{ratingKey}}{ProviderDefinitions.ExtrasPath}",
    async Task<IResult> (string ratingKey, MetadataService service, HttpRequest httpRequest, IOptions<ProviderOptions> options, CancellationToken cancellationToken) =>
    {
        var result = await service.GetMovieExtrasAsync(ratingKey, BuildContext(httpRequest, options.Value), BuildExtrasPaging(httpRequest), cancellationToken);
        return Results.Ok(result);
    });

app.MapGet(ProviderDefinitions.TvBasePath, () => Results.Ok(ProviderDefinitions.CreateTvProvider()));
app.MapPost($"{ProviderDefinitions.TvBasePath}{ProviderDefinitions.MatchPath}",
    async Task<IResult> (MatchRequest request, MatchService service, HttpRequest httpRequest, IOptions<ProviderOptions> options, CancellationToken cancellationToken) =>
    {
        try
        {
            var result = await service.MatchAsync(request, BuildContext(httpRequest, options.Value), cancellationToken);
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = "TV match failed", message = ex.Message });
        }
    });

app.MapGet($"{ProviderDefinitions.TvBasePath}{ProviderDefinitions.MetadataPath}/{{ratingKey}}",
    async Task<IResult> (string ratingKey, bool? includeChildren, MetadataService service, HttpRequest httpRequest, IOptions<ProviderOptions> options, CancellationToken cancellationToken) =>
    {
        try
        {
            var result = await service.GetTvMetadataAsync(
                ratingKey,
                BuildContext(httpRequest, options.Value),
                includeChildren == true || httpRequest.Query["includeChildren"] == "1",
                cancellationToken);

            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            return Results.NotFound(new { error = "TV metadata not found", message = ex.Message });
        }
    });

app.MapGet($"{ProviderDefinitions.TvBasePath}{ProviderDefinitions.MetadataPath}/{{ratingKey}}/images",
    async Task<IResult> (string ratingKey, MetadataService service, HttpRequest httpRequest, IOptions<ProviderOptions> options, CancellationToken cancellationToken) =>
    {
        try
        {
            var result = await service.GetTvImagesAsync(ratingKey, BuildContext(httpRequest, options.Value), cancellationToken);
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            return Results.NotFound(new { error = "TV images not found", message = ex.Message });
        }
    });

app.MapGet($"{ProviderDefinitions.TvBasePath}{ProviderDefinitions.MetadataPath}/{{ratingKey}}{ProviderDefinitions.ExtrasPath}",
    async Task<IResult> (string ratingKey, MetadataService service, HttpRequest httpRequest, IOptions<ProviderOptions> options, CancellationToken cancellationToken) =>
    {
        var result = await service.GetTvExtrasAsync(ratingKey, BuildContext(httpRequest, options.Value), BuildExtrasPaging(httpRequest), cancellationToken);
        return Results.Ok(result);
    });

app.MapGet($"{ProviderDefinitions.TvBasePath}{ProviderDefinitions.MetadataPath}/{{ratingKey}}/children",
    async Task<IResult> (string ratingKey, MetadataService service, HttpRequest httpRequest, IOptions<ProviderOptions> options, CancellationToken cancellationToken) =>
    {
        try
        {
            var result = await service.GetTvChildrenAsync(
                ratingKey,
                BuildContext(httpRequest, options.Value),
                BuildPaging(httpRequest),
                cancellationToken);

            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            return Results.NotFound(new { error = "TV children not found", message = ex.Message });
        }
    });

app.Run();

static PlexRequestContext BuildContext(HttpRequest request, ProviderOptions options)
{
    var language = request.Headers["X-Plex-Language"].FirstOrDefault()
        ?? request.Query["X-Plex-Language"].FirstOrDefault()
        ?? options.DefaultLanguage;

    var country = request.Headers["X-Plex-Country"].FirstOrDefault()
        ?? request.Query["X-Plex-Country"].FirstOrDefault()
        ?? options.DefaultCountry;

    return new PlexRequestContext(language, country);
}

static PagingOptions BuildPaging(HttpRequest request)
{
    static int ParseValue(string? value, int fallback)
        => int.TryParse(value, out var parsed) && parsed > 0 ? parsed : fallback;

    var size = ParseValue(request.Headers["X-Plex-Container-Size"].FirstOrDefault() ?? request.Query["X-Plex-Container-Size"].FirstOrDefault(), 20);
    var start = ParseValue(request.Headers["X-Plex-Container-Start"].FirstOrDefault() ?? request.Query["X-Plex-Container-Start"].FirstOrDefault(), 1);
    return new PagingOptions(size, start);
}

static PagingOptions BuildExtrasPaging(HttpRequest request)
{
    static int ParseValue(string? value, int fallback)
        => int.TryParse(value, out var parsed) && parsed >= 0 ? parsed : fallback;

    var size = ParseValue(request.Headers["X-Plex-Container-Size"].FirstOrDefault() ?? request.Query["X-Plex-Container-Size"].FirstOrDefault(), 50);
    var start = ParseValue(request.Headers["X-Plex-Container-Start"].FirstOrDefault() ?? request.Query["X-Plex-Container-Start"].FirstOrDefault(), 0);
    return new PagingOptions(size, start);
}

public partial class Program;
