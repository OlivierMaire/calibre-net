using System.Text.Json.Serialization;

namespace Calibre_net.Shared.Contracts;
public partial class BookDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = null!;

    [JsonPropertyName("sort")]
    public string? Sort { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTimeOffset? Timestamp { get; set; }

    [JsonPropertyName("pubdate")]
    public DateTimeOffset? Pubdate { get; set; }

    [JsonPropertyName("seriesIndex")]
    public double SeriesIndex { get; set; }

    [JsonPropertyName("authorsort")]
    public string? AuthorSort { get; set; }

    [JsonConverter(typeof(SensibleDataConverter<string>))]
    [JsonPropertyName("path")]
    public string Path { get; set; } = null!;

    public string? Uuid { get; set; }

    [JsonPropertyName("hascover")]
    public bool? HasCover { get; set; }

    [JsonPropertyName("lastmodified")]
    public DateTimeOffset LastModified { get; set; }

    [JsonPropertyName("authors")]
    public List<AuthorDto> Authors {get;set;} = [];
    [JsonPropertyName("series")]
    public SeriesDto Series {get;set;} = null!;

    [JsonPropertyName("rating")]
    public RatingDto Rating {get;set;} = null!;
    
    [JsonPropertyName("languages")]
    public List<LanguageDto> Languages {get;set;} = [];

    [JsonPropertyName("identifiers")]
    public List<IdentifierDto> Identifiers {get;set;} = [];

    [JsonPropertyName("tags")]
    public List<TagDto> Tags {get;set;} = [];

    [JsonPropertyName("publisher")]
    public PublisherDto Publisher {get;set;} = null!;

    [JsonPropertyName("custom_columns")]
    virtual public IDictionary<int, CustomColumnDto> CustomColumns {get;set;} = new Dictionary<int,CustomColumnDto>();


    [JsonPropertyName("comments")]
    public CommentDto Comments {get;set;} = new();

    
    [JsonPropertyName("data")]
    public List<DataDto> Data {get;set;} = [];

    [JsonIgnore]
    public string BookLink => $"/book/{Id}";

    
    [JsonIgnore]
    public string CoverUrl => this.HasCover ?? false ? $"/api/v1/book/cover/{this.Id}" :
    "/no-image.png";

    [JsonPropertyName("mark_as_read")]
    public bool MarkAsRead {get;set;} = false;
}
