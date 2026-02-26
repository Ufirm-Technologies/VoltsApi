using System.Text.Json.Serialization;

namespace vaulterpAPI.Models.Blog
{
    //public class BlogDto
    //{
    //    public Guid? Id { get; set; }

    //    public string Title { get; set; }
    //    public string Slug { get; set; }

    //    public string? Content { get; set; }

    //    public Guid? AuthorId { get; set; }
    //    public string AuthorName { get; set; }

    //    public string FeaturedImage { get; set; }
    //    public string[] Tags { get; set; }

    //    public string Status { get; set; }

    //    public int Views { get; set; }
    //    public int Likes { get; set; }
    //    public IFormFile Image { get; set; }
    //}

    public class BlogDto
    {
        public Guid? Id { get; set; }

        public string Title { get; set; }
        public string Slug { get; set; }

        // OLD FLOW (Simple Blog)
        public string? Content { get; set; }

        // NEW FLOW (Structured Blog JSON)
        public string? SectionsJson { get; set; }

        public Guid? AuthorId { get; set; }
        public string AuthorName { get; set; }

        public string FeaturedImage { get; set; }
        public string[] Tags { get; set; }

        public string Status { get; set; }

        public int Views { get; set; }
        public int Likes { get; set; }

        public IFormFile Image { get; set; }
    }


    public class StructuredBlogRequest
    {
        public Guid? Id { get; set; }
        public string Title { get; set; }
        public string Slug { get; set; }
        public Guid? AuthorId { get; set; }
        public string? AuthorName { get; set; }
        public string[]? Tags { get; set; }
        public string? Status { get; set; }

        public string? FeaturedImage { get; set; }
        public List<BlogSection> Sections { get; set; }
    }

    public class BlogSection
    {
        public string? ImageUrl { get; set; }
        public string? Heading { get; set; }
        public string? Text { get; set; }
    }

    public class StructuredContent
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("sections")]
        public List<SectionItem> Sections { get; set; }
    }

    public class SectionItem
    {
        [JsonPropertyName("order")]
        public int Order { get; set; }

        [JsonPropertyName("imageUrl")]
        public string? ImageUrl { get; set; }

        [JsonPropertyName("heading")]
        public string? Heading { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }


}
