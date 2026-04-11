using System.Text.Json.Serialization;

namespace ReturnsService.DTOs
{
    public class ReturnActionDto
    {
        [JsonPropertyName("note")]
        public string? Note { get; set; }
    }
}
