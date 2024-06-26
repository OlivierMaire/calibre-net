using System.Text.Json.Serialization;

namespace calibre_net.Shared.Contracts;
public partial class AuthorDto : Searchable
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("sort")]
    public string? Sort { get; set; }

    [JsonPropertyName("link")]
    public string Link { get; set; } = null!;

    [JsonPropertyName("books")]
    public List<BookDto> Books { get; set; } = [];

    [JsonPropertyName("bookCount")]
    public int BookCount {get;set;} = 0;

    [JsonIgnore]
    public override string SearchUrl => $"author/{Id}";

}