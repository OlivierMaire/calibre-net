using Calibre_net.Services;
using Calibre_net.Shared.Contracts;
using FastEndpoints;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.StaticFiles;
using ATL.AudioData;
using ATL;
using static ATL.PictureInfo;
using Calibre_net.Data;
using Microsoft.AspNetCore.Authorization;
using FastEndpoints.Security;
using System.Security.Claims;

namespace Calibre_net.Api.Endpoints;

public class Book : Group
{
    public Book()
    {
        Configure("book", ep => ep.Description(x => x.WithGroupName("book")));
    }
}

public sealed class GetBookEndpoint(BookService service) : Endpoint<GetBookRequest, BookDto>
{
    // [JsonSchema(JsonObjectType.Int32, Format = "guid")]
    // private int Id { get; set; }
    private readonly BookService service = service;

    public override void Configure()
    {
        Get("/{Id:int}");
        Version(1);
        Group<Book>();
        ResponseCache(60); //cache for 60 seconds
        Policies(PermissionType.BOOK_VIEW);
        Description(x => x.Produces(404));
    }

    public override async Task HandleAsync(GetBookRequest req, CancellationToken ct)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        var book = service.GetBook(req.Id,userId);
        if (book == null)
            await SendNotFoundAsync(ct);
        else
            await SendOkAsync(book);
    }
}

public sealed class GetBookCoverEndpoint(BookService bookService, ConfigurationService configService) : Endpoint<GetBookRequest, byte[]>
{
    private readonly BookService bookService = bookService;
    private readonly ConfigurationService configService = configService;
    private const int _7DaysInSeconds = 86400;

    public override void Configure()
    {
        Get("/cover/{Id:int}");
        Version(1);
        Group<Book>();
        ResponseCache(_7DaysInSeconds); //cache for 7 days
        Policies(PermissionType.BOOK_VIEW);

    }

    public override async Task HandleAsync(GetBookRequest req, CancellationToken ct)
    {
        var conf = configService.GetCalibreConfiguration();

        // get book
        var bookPath = bookService.GetBookCover(req.Id);
        if (!String.IsNullOrEmpty(bookPath))
        {
            var path = conf.Database?.Location ?? string.Empty;
            path = path.EndsWith(Path.DirectorySeparatorChar) ? path : path + Path.DirectorySeparatorChar;
            path += bookPath;

            if (System.IO.File.Exists(path))
            {
                new FileExtensionContentTypeProvider()
                    .TryGetContentType(path, out var contentType);
                var stream = System.IO.File.OpenRead(path);
                await SendStreamAsync(stream, contentType: contentType ?? "image/jpg",
                cancellation: ct, fileName: $"{req.Id}.jpg");
                return;
            }

        }
        await SendErrorsAsync();
    }
}


public sealed class DownloadBookEndpoint(BookService bookService, ConfigurationService configService) : Endpoint<DownloadBookRequest, byte[]>
{
    private readonly BookService bookService = bookService;
    private readonly ConfigurationService configService = configService;
    private const int _7DaysInSeconds = 86400;

    public override void Configure()
    {
        Get("/download/{Id:int}/{format}");
        Version(1);
        Group<Book>();
        ResponseCache(_7DaysInSeconds); //cache for 7 days
        Policies(PermissionType.BOOK_VIEW);

    }

    public override async Task HandleAsync(DownloadBookRequest req, CancellationToken ct)
    {
        var conf = configService.GetCalibreConfiguration();

        // get book
        var bookPath = bookService.GetBookFile(req.Id, req.Format);
        if (!String.IsNullOrEmpty(bookPath))
        {
            var path = conf.Database?.Location ?? string.Empty;
            path = path.EndsWith(Path.DirectorySeparatorChar) ? path : path + Path.DirectorySeparatorChar;
            path += bookPath;
            if (System.IO.File.Exists(path))
            {
                new FileExtensionContentTypeProvider()
                .TryGetContentType(path, out var contentType);
                // FileInfo fi = new FileInfo(path);
                // await SendFileAsync(fi, contentType:  contentType ?? "application/octet-stream", enableRangeProcessing: true);

                this.HttpContext.Response.Headers.ContentDisposition = $"inline;"; // filename={Path.GetFileName(path)}";
                var stream = System.IO.File.OpenRead(path);
                await SendStreamAsync(stream, contentType: contentType ?? "application/octet-stream",
                cancellation: ct
                //, fileName: Path.GetFileName(path) // do not set filename else the Content-Disposition Header becomes attachment;
                , enableRangeProcessing: true);
                return;
            }

        }
        await SendErrorsAsync();
    }
}

public sealed class SetBookmarkEndpoint(ApplicationDbContext dbContext) : Endpoint<SetBookmarkRequest, Ok>
{
    private readonly ApplicationDbContext dbContext = dbContext;

    public override void Configure()
    {
        Get("/setBookmark");
        Version(1);
        Group<Book>();
        Policies(PermissionType.BOOK_VIEW);
    }

    public override async Task HandleAsync(SetBookmarkRequest req, CancellationToken ct)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        if (string.IsNullOrEmpty(userId))
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        // save bookmark
        var bookmark = await dbContext.Bookmarks.FindAsync([userId, (uint)req.BookId, req.BookFormat], ct);
        if (bookmark != null)
            bookmark.Position = req.Position;
        else
        {
            dbContext.Bookmarks.Add(new Bookmark
            {
                BookId = (uint)req.BookId,
                Format = req.BookFormat,
                Position = req.Position,
                UserId = userId
            });
        }
        await dbContext.SaveChangesAsync(ct);
        await SendOkAsync(ct);
    }
}

public sealed class GetBookmarkEndpoint(ApplicationDbContext dbContext) : Endpoint<GetBookmarkRequest, GetBookmarkResponse>
{
    private readonly ApplicationDbContext dbContext = dbContext;

    public override void Configure()
    {
        Get("/getBookmark");
        Version(1);
        Group<Book>();
        Policies(PermissionType.BOOK_VIEW);
    }

    public override async Task HandleAsync(GetBookmarkRequest req, CancellationToken ct)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        if (string.IsNullOrEmpty(userId))
        {
            await SendUnauthorizedAsync(ct);
            return;
        }
        // get bookmark
        var bookmark = await dbContext.Bookmarks.FindAsync([userId, (uint)req.BookId, req.BookFormat], ct);

        if (bookmark != null)
            await SendOkAsync(new GetBookmarkResponse((int)bookmark.BookId, bookmark.Format, bookmark.Position), ct);
        else
            await SendOkAsync(new GetBookmarkResponse((int)req.BookId, req.BookFormat, string.Empty), ct);
    }
}


public sealed class SearchBooksEndpoint(BookService service) : Endpoint<GetSearchValuesRequest, List<BookDto>>
{
    private readonly BookService service = service;

    public override void Configure()
    {
        Post("/search");
        Version(1);
        Group<Book>();
        Policies(PermissionType.BOOK_VIEW);
        // ResponseCache((int)TimeSpan.FromDays(1).TotalSeconds);//, varyByHeader: "x-request-hash");
        Options(x => x.CacheOutput(p => p.AddPolicy(typeof(MyCustomPolicy))
        .SetVaryByHeader("x-request-hash")
        .Expire(TimeSpan.FromDays(1))
        .Tag("book_search")));
    }

    public override async Task HandleAsync(GetSearchValuesRequest req, CancellationToken ct)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        this.HttpContext.Response.Headers.Append("cache", "force-cache");
        await SendOkAsync(service.GetBooks(req, userId), ct);
    }
}


public sealed class GetCustomColumns(BookService bookService) : EndpointWithoutRequest<GetCustomColumnListResponse>
{
    private readonly BookService _bookService = bookService;

    public override void Configure()
    {
        Get("custom_columns");
        Version(1);
        Group<Book>();
        ResponseCache((int)TimeSpan.FromDays(1).TotalSeconds);
        Policies(PermissionType.BOOK_VIEW);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var cc = _bookService.GetCustomColumns();
        await SendOkAsync(new GetCustomColumnListResponse(cc.ProjectToDto().ToList()), ct);
    }
}