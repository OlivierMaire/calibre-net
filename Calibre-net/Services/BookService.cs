using Calibre_net.Client.Services;
using Calibre_net.Data.Calibre;
using Calibre_net.Shared.Contracts;
using Calibre_net.Shared;
using Dapper;
using System.Globalization;
using Calibre_net.Data;
using System.Security.Claims;

namespace Calibre_net.Services;

[ScopedRegistration]
public class BookService(CalibreDbDapperContext dbContext, ApplicationDbContext appDbContext)

{
    private readonly CalibreDbDapperContext dbContext = dbContext;
    private readonly ApplicationDbContext _appDbContext = appDbContext;
    private static readonly Dictionary<string, string> OrderTables = new(){
        {$"{SearchTermsConstants.BOOK_TAG}_LastModified", "b.timestamp"},
        {$"{SearchTermsConstants.BOOK_TAG}_Title", "b.title"},
        {$"{SearchTermsConstants.BOOK_TAG}_Pubdate", "b.pubdate"},
        {$"{SearchTermsConstants.AUTHOR_TAG}_AuthorSort", "a.sort"},
        {$"{SearchTermsConstants.SERIES_TAG}_Sort", "s.sort"},
        {$"{SearchTermsConstants.RATING_TAG}_Rating", "r.rating"},
        {$"{SearchTermsConstants.PUBLISHER_TAG}_Sort", "p.name"},
        {$"{SearchTermsConstants.LANGUAGE_TAG}_LangCode", "l.lang_code"},
        {$"{SearchTermsConstants.FORMAT_TAG}_Format", "d.format"},
    };

    public List<BookDto> GetBooks(GetSearchValuesRequest req, string userId)
    {
        // var books = calibreDb.Books
        // .Include(b => b.Authors)
        // .Include(b => b.Series)
        // .Include(b => b.Rating)
        // .AsSplitQuery()
        // .FromCache("books");

        using var ctx = dbContext.ConnectionCreate();
        var dynamicParams = new DynamicParameters();
        var sql = $"""
            SELECT b.id, b.title, b.sort, b.timestamp, b.pubdate, b.series_index, b.author_sort, b.path, b.uuid, b.has_cover, b.last_modified, 
            a.id, a.name, a.sort, a.link, 
            s.id, s.name, s.sort, s.link, 
            r.id, r.rating as ratingValue, r.link
            FROM books as b
            LEFT JOIN books_authors_link bal on bal.book = b.id
            LEFT JOIN authors a on a.Id = bal.author
            LEFT JOIN books_series_link bsl on bsl.book = b.id
            LEFT JOIN series s on s.Id = bsl.series
            LEFT JOIN books_ratings_link brl on brl.book = b.id
            LEFT JOIN ratings r on r.Id = brl.rating 
            """;

        string sqlJoin = string.Empty;
        string sqlWhere = string.Empty;

        if (req.Terms.HasKey(SearchTermsConstants.AUTHOR_TAG))
        {
            var term = req.Terms.Get(SearchTermsConstants.AUTHOR_TAG);
            if (term is StringSearchTerm stringTerm)
            {
                var operators = stringTerm.StringSearchOperator.ToEnumOperatorString().Split(",");
                var termValue = System.Net.WebUtility.UrlDecode(term?.Value);
                sqlWhere += $" AND (a.name {operators[0]} @authorName) ";
                dynamicParams.Add("authorName", operators[1].Replace("VALUE", termValue));
            }

            if (term is IdSearchTerm)
            {
                sqlWhere += " AND (a.Id = @authorId) ";
                dynamicParams.Add("authorId", term?.Value);
            }
        }
        if (req.Terms.HasKey(SearchTermsConstants.SERIES_TAG))
        {
            var term = req.Terms.Get(SearchTermsConstants.SERIES_TAG);
            if (term is StringSearchTerm stringTerm)
            {
                var operators = stringTerm.StringSearchOperator.ToEnumOperatorString().Split(",");
                var termValue = System.Net.WebUtility.UrlDecode(term?.Value);
                sqlWhere += $" AND (s.name {operators[0]} @seriesName) ";
                dynamicParams.Add("seriesName", operators[1].Replace("VALUE", termValue));
            }

            if (term is IdSearchTerm)
            {
                sqlWhere += " AND (s.Id = @seriesId) ";
                dynamicParams.Add("seriesId", term?.Value);
            }
        }
        if (req.Terms.HasKey(SearchTermsConstants.RATING_TAG))
        {
            var term = req.Terms.Get(SearchTermsConstants.RATING_TAG);
            if (term is RatingSearchTerm ratingTerm)
            {
                if (decimal.TryParse(term?.Value, CultureInfo.InvariantCulture, out var numValue))
                {
                    numValue *= 2;
                    sqlWhere += $" AND (r.rating {ratingTerm.NumericSearchOperator.ToEnumString()} @ratingName) ";
                    dynamicParams.Add("ratingName", (int)numValue);
                }
            }
            if (term is IdSearchTerm)
            {
                sqlWhere += " AND (r.Id = @ratingId) ";
                dynamicParams.Add("ratingId", term?.Value);
            }
        }
        if (req.Terms.HasKey(SearchTermsConstants.TAG_TAG))
        {
            var term = req.Terms.Get(SearchTermsConstants.TAG_TAG);

            sqlJoin += """ 
            JOIN books_tags_link btl on btl.book = b.id
            JOIN tags t on t.Id = btl.tag
            """;

            if (term is StringSearchTerm stringTerm)
            {
                var operators = stringTerm.StringSearchOperator.ToEnumOperatorString().Split(",");
                var termValue = System.Net.WebUtility.UrlDecode(term?.Value);
                sqlWhere += $" AND (t.name {operators[0]} @tagName) ";
                dynamicParams.Add("tagName", operators[1].Replace("VALUE", termValue));
            }
            if (term is IdSearchTerm)
            {
                sqlWhere += " AND (t.Id = @tagId) ";
                dynamicParams.Add("tagId", term?.Value);
            }
        }
        if (req.Terms.HasKey(SearchTermsConstants.PUBLISHER_TAG)
        || req.Orders.HasKey(SearchTermsConstants.PUBLISHER_TAG))
        {

            sqlJoin += """ 
             JOIN books_publishers_link bpl on bpl.book = b.id
             JOIN publishers p on p.Id = bpl.publisher
            """;
            if (req.Terms.HasKey(SearchTermsConstants.PUBLISHER_TAG))
            {
                var term = req.Terms.Get(SearchTermsConstants.PUBLISHER_TAG);

                if (term is StringSearchTerm stringTerm)
                {
                    var operators = stringTerm.StringSearchOperator.ToEnumOperatorString().Split(",");
                    var termValue = System.Net.WebUtility.UrlDecode(term?.Value);
                    sqlWhere += $" AND (p.name {operators[0]} @publisherName) ";
                    dynamicParams.Add("publisherName", operators[1].Replace("VALUE", termValue));
                }
                else if (term is IdSearchTerm)
                {
                    sqlWhere += " AND (p.Id = @publisherId) ";
                    dynamicParams.Add("publisherId", term?.Value);
                }
            }
        }
        if (req.Terms.HasKey(SearchTermsConstants.LANGUAGE_TAG)
        || req.Orders.HasKey(SearchTermsConstants.LANGUAGE_TAG))
        {

            sqlJoin += """ 
             JOIN books_languages_link bll on bll.book = b.id
             JOIN languages l on l.Id = bll.lang_code
            """;

            if (req.Terms.HasKey(SearchTermsConstants.PUBLISHER_TAG))
            {
                var term = req.Terms.Get(SearchTermsConstants.LANGUAGE_TAG);

                if (term is StringSearchTerm stringTerm)
                {
                    var operators = stringTerm.StringSearchOperator.ToEnumOperatorString().Split(",");
                    var termValue = System.Net.WebUtility.UrlDecode(term?.Value);
                    sqlWhere += $" AND (l.lang_code {operators[0]} @languageName) ";
                    dynamicParams.Add("languageName", operators[1].Replace("VALUE", termValue));
                }
                if (term is ListSearchTerm || term is IdSearchTerm)
                {
                    sqlWhere += " AND (l.Id = @languageId) ";
                    dynamicParams.Add("languageId", term?.Value);
                }
            }
        }
        if (req.Terms.HasKey(SearchTermsConstants.FORMAT_TAG)
        || req.Orders.HasKey(SearchTermsConstants.FORMAT_TAG))
        {
            sqlJoin += " JOIN data d on d.book = b.id ";
            if (req.Terms.HasKey(SearchTermsConstants.FORMAT_TAG))
            {
                var term = req.Terms.Get(SearchTermsConstants.FORMAT_TAG);
                if (term is ListSearchTerm || term is IdSearchTerm)
                {
                    var termValue = System.Net.WebUtility.UrlDecode(term?.Value);
                    if (termValue?.Contains(",") ?? false)
                    {
                        var formatList = termValue.Split(',').Select(f => f.Trim()).ToList();
                        var placeholders = string.Join(",", formatList.Select((_, i) => $"@format{i}"));
                        sqlWhere += $" AND (d.Format IN ({placeholders})) ";
                        for (int i = 0; i < formatList.Count; i++)
                        {
                            dynamicParams.Add($"format{i}", formatList[i]);
                        }
                    }
                    else
                    {
                        sqlWhere += " AND (d.Format = @formatValue) ";
                        dynamicParams.Add("formatValue", termValue);
                    }
                }
            }
        }
        if (req.Terms.HasKey(SearchTermsConstants.KEYWORD_TAG))
        {
            var term = req.Terms.Get(SearchTermsConstants.KEYWORD_TAG);

            sqlJoin += " JOIN comments c on c.book = b.id ";
            if (term is StringSearchTerm stringTerm)
            {
                var operators = stringTerm.StringSearchOperator.ToEnumOperatorString().Split(",");
                var termValue = System.Net.WebUtility.UrlDecode(term?.Value);
                sqlWhere += $""" 
                        AND (
                                (b.Title {operators[0]} @keyword) 
                            OR  (c.text {operators[0]} @keyword)
                            )
                        """;
                dynamicParams.Add("keyword", operators[1].Replace("VALUE", termValue));
            }
        }
        if (req.Terms.Any(t => t.Key.StartsWith("cc_")) ||
            req.Orders.Any(t => t.Key.StartsWith("cc_")))
        {
            var terms = req.Terms.Where(t => t.Key.StartsWith("cc_")).Select(t => t.Key);
            var orders = req.Orders.Where(t => t.Key.StartsWith("cc_")).Select(t => t.Key);
            IEnumerable<string> list = [.. terms, .. orders];
            foreach (var termKey in list.Distinct())
            {
                var ccKey = termKey[3..];

                sqlJoin += $""" 
                 JOIN books_custom_column_{ccKey}_link bccl_{ccKey} on bccl_{ccKey}.book = b.id
                 JOIN custom_column_{ccKey} cc_{ccKey} on cc_{ccKey}.Id = bccl_{ccKey}.value
                """;

                if (req.Terms.HasKey(termKey))
                {
                    var term = req.Terms.Single(t => t.Key == termKey);

                    if (term is StringSearchTerm stringTerm)
                    {
                        var operators = stringTerm.StringSearchOperator.ToEnumOperatorString().Split(",");
                        var termValue = System.Net.WebUtility.UrlDecode(term?.Value);

                        sqlWhere += $" AND (cc_{ccKey}.value {operators[0]} @cc_{ccKey}Name) ";
                        dynamicParams.Add($"cc_{ccKey}Name", operators[1].Replace("VALUE", termValue));
                    }
                    if (term is IdSearchTerm)
                    {
                        sqlWhere += $" AND (cc_{ccKey}.Id = @cc_{ccKey}Id) ";
                        dynamicParams.Add($"cc_{ccKey}Id", term?.Value);
                    }
                }
            }

        }

        sql += sqlJoin;
        if (!string.IsNullOrEmpty(sqlWhere))
        {
            sql += " WHERE 1 = 1 ";
            sql += sqlWhere;
        }

        if (req.Orders.Count > 0)
        {
            string sqlOrder = " ORDER BY ";
            bool first = true;
            foreach (var o in req.Orders)
            {
                if (!first)
                    sqlOrder += ", ";
                if (o.Key.StartsWith("cc_"))
                {
                    var ccKey = o.Key[3..];
                    sqlOrder += $" cc_{ccKey}.value {(o.Ascending ? "asc" : "desc")} ";

                }
                else
                {

                    var key = $"{o.Key}_{o.PropertyName}";
                    sqlOrder += $" {OrderTables[key]} {(o.Ascending ? "asc" : "desc")} ";
                }
                first = false;
            }
            sql += sqlOrder;
        }

        // Console.WriteLine(sql);

        var books =
            ctx.Query<Book, Author, Series, Rating, Book>(sql,
                        (book, author, serie, rating) =>
            {
                book.Authors.Add(author);
                book.Series.Add(serie);
                book.Rating.Add(rating);
                return book;
            },
            param: dynamicParams,
            splitOn: "Id,Id,Id,Id");

        books = books.GroupBy(b => b.Id)
            .Select(g =>
            {
                var groupedBook = g.First();
                groupedBook.Authors = g.Select(b => b.Authors.SingleOrDefault() ?? new Author()).ToList();
                groupedBook.Series = g.Select(b => b.Series.SingleOrDefault() ?? new Series()).ToList();
                groupedBook.Rating = g.Select(b => b.Rating.SingleOrDefault() ?? new Rating()).ToList();
                return groupedBook;
            }).ToList();

        if (!string.IsNullOrEmpty(userId))
        {
            var readStates = _appDbContext.ReadStates.Where(x => x.UserId == userId && books.Select(b => b.Id).Contains((int)x.BookId)).ToArray();
            books = books.GroupJoin(readStates, b => b.Id, r => (int)r.BookId, (b, r) =>
            {
                var book = b;
                book.MarkAsRead = r.SingleOrDefault()?.MarkedAsRead ?? false;
                return book;
            });
        }
        return books.ProjectToDto().ToList();

    }

    public BookDto? GetBook(int id, string userId = "")
    {
        using (var ctx = dbContext.ConnectionCreate())
        {
            using (var multi = ctx.QueryMultiple("""
                SELECT * from books where id = @id;
                SELECT a.* from books_authors_link bal join authors a on a.id = bal.author where bal.book = @id;
                SELECT s.* from books_series_link bsl join series s on s.Id = bsl.series where bsl.book = @id;
                SELECT r.id, r.rating as ratingValue, r.link from books_ratings_link brl join ratings r on r.Id = brl.rating where brl.book = @id;
                SELECT l.* from books_languages_link bll join languages l on l.Id = bll.lang_code where bll.book = @id;
                SELECT i.* from identifiers i where i.book = @id;
                SELECT t.* from books_tags_link btl join tags t on t.Id = btl.tag where btl.book = @id;
                SELECT p.* from books_publishers_link bpl join publishers p on p.Id = bpl.publisher where bpl.book = @id;
                SELECT c.* from comments c where c.book = @id;
                SELECT d.* from data d where d.book = @id;
                SELECT cc.* from custom_columns as cc;
                """, new { id }))
            {

                Book? book = null;
                try
                {
                   book = multi.ReadFirst<Book>();
                }
                catch (Exception ex)
                {
                    // no book found
                    return null;
                }
                finally
                {

                }

                if (book == null)
                    return null;

                book.Authors = multi.Read<Author>().ToList();
                book.Series = multi.Read<Series>().ToList();
                book.Rating = multi.Read<Rating>().ToList();
                book.Languages = multi.Read<Language>().ToList();
                book.Identifiers = multi.Read<Identifier>().ToList();
                book.Tags = multi.Read<Tag>().ToList();
                book.Publisher = multi.Read<Publisher>().ToList();
                book.Comments = multi.Read<Comment>().FirstOrDefault();
                book.Data = multi.Read<Datum>().ToList();

                var cc = multi.Read<CustomColumn>().ToDictionary(cc => cc.Id, cc => cc);
                book.CustomColumns = cc;
                var sql = string.Empty;
                foreach (var col in cc)
                {
                    sql += $" SELECT cc.* from books_custom_column_{col.Key}_link as bccl join custom_column_{col.Key} cc on cc.Id = bccl.value where bccl.book = @id; ";
                }
                if (!string.IsNullOrEmpty(sql))
                {
                    using (var multi_cc = ctx.QueryMultiple(sql, new { id }))
                    {

                        foreach (var col in cc)
                        {
                            book.CustomColumns[col.Key].Data = multi_cc.Read<GenericCustomColumn>().ToList();
                        }
                    }
                }

                if (!string.IsNullOrEmpty(userId))
                    book.MarkAsRead = _appDbContext.ReadStates.SingleOrDefault(x => x.UserId == userId && book.Id == (int)x.BookId)?.MarkedAsRead ?? false;

                return book.ToDto();
            }
        }

    }

    public string GetBookCover(int id)
    {
        using (var ctx = dbContext.ConnectionCreate())
        {
            var bookPath =
            ctx.QueryFirstOrDefault<string>($"""
            SELECT b.path FROM books as b
            WHERE b.Id == @id
            """, new { id });
            var path = bookPath ?? string.Empty;
            path = path.EndsWith(Path.DirectorySeparatorChar) ? path : path + Path.DirectorySeparatorChar;
            path += "cover.jpg";
            return path;
        }
    }

    public string GetBookFile(int id, string format)
    {
        using (var ctx = dbContext.ConnectionCreate())
        {
            var (bookPath, fileName) =
            ctx.QueryFirstOrDefault<(string, string)>($"""
            SELECT b.path, d.name FROM books as b
            JOIN data as d on d.book = b.id
            WHERE b.Id == @id
            and d.format = @format
            """, new { id, format });
            var path = bookPath;
            path = path.EndsWith(Path.DirectorySeparatorChar) ? path : path + Path.DirectorySeparatorChar;
            path += fileName + "." + format.ToLowerInvariant();
            return path;
        }

    }

    public List<TagDto> GetAllTags()
    {
        using (var ctx = dbContext.ConnectionCreate())
        {
            var tags =
            ctx.Query<TagDto>($"""
            select t.Id, MAX(t.name) as name, MAX(t.link) as link, count() as bookCount from tags t
            join books_tags_link btl on btl.tag = t.Id
            group by t.Id
            """);
            return tags.ToList();
        }
    }


    public List<SeriesDto> GetAllSeries()
    {
        using (var ctx = dbContext.ConnectionCreate())
        {
            var series =
            ctx.Query<SeriesDto>($"""
            select s.Id, MAX(s.name) as name, MAX(s.sort) as sort, MAX(s.link) as link, count() as bookCount from series s
            join books_series_link bsl on bsl.series = s.Id
            group by s.Id
            """);
            return series.ToList();
        }
    }

    public List<AuthorDto> GetAllAuthors()
    {
        using (var ctx = dbContext.ConnectionCreate())
        {
            var authors =
            ctx.Query<AuthorDto>($"""
            select s.Id, MAX(s.name) as name, MAX(s.sort) as sort, MAX(s.link) as link, count() as bookCount from authors s
            join books_authors_link bsl on bsl.author = s.Id
            group by s.Id
            """);
            return authors.ToList();
        }
    }

    public List<PublisherDto> GetAllPublishers()
    {
        using (var ctx = dbContext.ConnectionCreate())
        {
            var publishers =
            ctx.Query<PublisherDto>($"""
            select s.Id, MAX(s.name) as name, MAX(s.sort) as sort, MAX(s.link) as link, count() as bookCount from publishers s
            join books_publishers_link bsl on bsl.publisher = s.Id
            group by s.Id
            """);
            return publishers.ToList();
        }
    }
    public List<LanguageDto> GetAllLanguages()
    {
        using (var ctx = dbContext.ConnectionCreate())
        {
            var languages =
            ctx.Query<LanguageDto>($"""
            select s.Id, MAX(s.lang_code) as langCode, MAX(s.link) as link, count() as bookCount from languages s
            join books_languages_link bsl on bsl.lang_code = s.Id
            group by s.Id
            """);
            return languages.ToList();
        }
    }
    public List<RatingDto> GetAllRatings()
    {
        using (var ctx = dbContext.ConnectionCreate())
        {
            var ratings =
            ctx.Query<RatingDto>($"""
            select s.Id, MAX(s.rating) as rating, MAX(s.link) as link, count() as bookCount from ratings s
            join books_ratings_link bsl on bsl.rating = s.Id
            group by s.Id
            """);
            return ratings.ToList();
        }
    }

    public List<FormatDto> GetAllFormats()
    {
        using (var ctx = dbContext.ConnectionCreate())
        {
            var formats =
            ctx.Query<FormatDto>($"""
            select d.format, count() as bookCount from data d
            group by d.format
            """);
            return formats.ToList();
        }
    }

    public List<CustomColumn> GetCustomColumns()
    {
        using (var ctx = dbContext.ConnectionCreate())
        {
            var cc =
            ctx.Query<CustomColumn>($"""
            select * from custom_columns
            """);
            return cc.ToList();
        }
    }

    public List<GenericCustomColumnDto> GetAllCustomColumns(int columnId)
    {
        using (var ctx = dbContext.ConnectionCreate())
        {
            var cc =
            ctx.Query<GenericCustomColumnDto>($"""
            select {columnId} as columnId, s.Id, MAX(s.value) as value, MAX(s.link) as link, count() as bookCount from custom_column_{columnId} s
            join books_custom_column_{columnId}_link bsl on bsl.value = s.Id
            group by s.Id
            """);
            return cc.ToList();
        }
    }
}