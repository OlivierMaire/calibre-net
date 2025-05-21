using Calibre_net.Services;
using Calibre_net.Shared.Contracts;
using FastEndpoints;
using FluentResults;

namespace Calibre_net.Api.Endpoints;
public class Category : Group
{
    public Category()
    {
        Configure("category", ep => ep.Description(x => {
            x.WithGroupName("category");
            x.ProducesProblemDetails(401,  "application/problem+json");
            x.ProducesProblemDetails(403, "application/problem+json");
            }));
    }
}

public sealed class GetTagsEndpoint(BookService bookService) : EndpointWithoutRequest<GetTagsResponse>
{
    private readonly BookService _bookService = bookService;

    public override void Configure()
    {
        Get("tags");
        Version(1);
        Group<Category>();
        ResponseCache((int)TimeSpan.FromDays(1).TotalSeconds);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var tags = _bookService.GetAllTags();
        await SendOkAsync(new GetTagsResponse(tags), ct);
    }
}

public sealed class GetSeriesEndpoint(BookService bookService) : EndpointWithoutRequest<GetSeriesResponse>
{
    private readonly BookService _bookService = bookService;

    public override void Configure()
    {
        Get("series");
        Version(1);
        Group<Category>();
        ResponseCache((int)TimeSpan.FromDays(1).TotalSeconds);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var series = _bookService.GetAllSeries();
        await SendOkAsync(new GetSeriesResponse(series), ct);
    }
}


public sealed class GetAuthorsEndpoint(BookService bookService) : EndpointWithoutRequest<GetAuthorsResponse>
{
    private readonly BookService _bookService = bookService;

    public override void Configure()
    {
        Get("authors");
        Version(1);
        Group<Category>();
        ResponseCache((int)TimeSpan.FromDays(1).TotalSeconds);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var authors = _bookService.GetAllAuthors();
        await SendOkAsync(new GetAuthorsResponse(authors), ct);
    }
}

public sealed class GetPublishersEndpoint(BookService bookService) : EndpointWithoutRequest<GetPublishersResponse>
{
    private readonly BookService _bookService = bookService;

    public override void Configure()
    {
        Get("publishers");
        Version(1);
        Group<Category>();
        ResponseCache((int)TimeSpan.FromDays(1).TotalSeconds);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var publishers = _bookService.GetAllPublishers();
        await SendOkAsync(new GetPublishersResponse(publishers), ct);
    }
}


public sealed class GetLanguagesEndpoint(BookService bookService) : EndpointWithoutRequest<GetLanguagesResponse>
{
    private readonly BookService _bookService = bookService;

    public override void Configure()
    {
        Get("languages");
        Version(1);
        Group<Category>();
        ResponseCache((int)TimeSpan.FromDays(1).TotalSeconds);
        Policies([PermissionType.BOOK_VIEW]);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var languages = _bookService.GetAllLanguages();
        await SendOkAsync(new GetLanguagesResponse(languages), ct);
    }
}


public sealed class GetCustomColumnEndpoint(BookService bookService) : Endpoint<GetCustomColumnsRequest, GetCustomColumnsResponse>
{
    private readonly BookService _bookService = bookService;

    public override void Configure()
    {
        Get("custom_column");
        Version(1);
        Group<Category>();
        ResponseCache((int)TimeSpan.FromDays(1).TotalSeconds, varyByQueryKeys: ["columnId"]);
        // Options(x => x.CacheOutput(p => p.AddPolicy(typeof(MyCustomPolicy))
        // .SetVaryByHeader("x-request-hash")
        // .Expire(TimeSpan.FromDays(1))));
        Policies(PermissionType.BOOK_VIEW);
    }

    public override async Task HandleAsync(GetCustomColumnsRequest req, CancellationToken ct)
    {
        var customColumns = _bookService.GetAllCustomColumns(req.ColumnId);
        await SendOkAsync(new GetCustomColumnsResponse(customColumns), ct);
    }
}


public sealed class GetRatingsEndpoint(BookService bookService) : EndpointWithoutRequest<GetRatingsResponse>
{
    private readonly BookService _bookService = bookService;

    public override void Configure()
    {
        Get("ratings");
        Version(1);
        Group<Category>();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var ratings = _bookService.GetAllRatings();
        await SendOkAsync(new GetRatingsResponse(ratings), ct);
    }
}


public sealed class GetFormatsEndpoint(BookService bookService) : EndpointWithoutRequest<GetFormatsResponse>
{
    private readonly BookService _bookService = bookService;

    public override void Configure()
    {
        Get("formats");
        Version(1);
        Group<Category>();
        ResponseCache((int)TimeSpan.FromDays(1).TotalSeconds);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var ratings = _bookService.GetAllFormats();
        await SendOkAsync(new GetFormatsResponse(ratings), ct);
    }
}