using Microsoft.AspNetCore.Mvc;
using Npgsql;
using NpgsqlTypes;
using System.Reflection;
using vaulterpAPI.Models;
using vaulterpAPI.Models.Blogs;

namespace vaulterpAPI.Controllers.Blogs
{
    [ApiController]
    [Route("api/blog")]
    public class BlogsController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public BlogsController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string GetConnectionString() =>
            _configuration.GetConnectionString("DefaultConnection");

        // -----------------------------
        // GET: api/blogs
        // -----------------------------
        // -----------------------------
        // GET: api/blogs?page=1&pageSize=10
        // -----------------------------
        [HttpGet]
        public IActionResult GetAllBlogs(int page = 1, int pageSize = 10)
        {
            var blogs = new List<BlogsDto>();

            try
            {
                if (page <= 0) page = 1;
                if (pageSize <= 0 || pageSize > 50) pageSize = 10;

                var offset = (page - 1) * pageSize;

                using var conn = new NpgsqlConnection(GetConnectionString());
                using var cmd = new NpgsqlCommand(@"
            SELECT id, title, slug, featured_image, status, views, likes
            FROM avaantra.blogs
            ORDER BY created_at DESC
            LIMIT @limit OFFSET @offset
        ", conn);

                cmd.Parameters.AddWithValue("@limit", pageSize);
                cmd.Parameters.AddWithValue("@offset", offset);

                conn.Open();
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    blogs.Add(new BlogsDto
                    {
                        Id = Guid.Parse(reader["id"].ToString()),
                        Title = reader["title"].ToString(),
                        Slug = reader["slug"].ToString(),
                        FeaturedImage = reader["featured_image"]?.ToString(),
                        Status = reader["status"].ToString(),
                        Views = Convert.ToInt32(reader["views"]),
                        Likes = Convert.ToInt32(reader["likes"])
                    });
                }

                return Ok(new
                {
                    page,
                    pageSize,
                    count = blogs.Count,
                    data = blogs
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch blogs", error = ex.Message });
            }
        }


        // -----------------------------
        // GET: api/blogs/{slug}
        // -----------------------------
        [HttpGet("{slug}")]
        public IActionResult GetBlogBySlug(string slug)
        {
            try
            {
                using var conn = new NpgsqlConnection(GetConnectionString());
                using var cmd = new NpgsqlCommand(@"
                    SELECT *
                    FROM avaantra.blogs
                    WHERE slug = @slug
                ", conn);

                cmd.Parameters.AddWithValue("@slug", slug);

                conn.Open();
                using var reader = cmd.ExecuteReader();

                if (!reader.Read())
                    return NotFound(new { message = "Blog not found" });

                return Ok(MapBlog(reader));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch blog", error = ex.Message });
            }
        }

        // -----------------------------
        // POST: api/blogs
        // -----------------------------
        [HttpPost]
        [RequestSizeLimit(10_000_000)] // 10MB
        public async Task<IActionResult> CreateBlog([FromForm] BlogsDto model)
        {
            try
            {
                string imagePath = null;

                if (model.Image != null)
                {
                    var uploadsFolder = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        "Assets",
                        "Blogsimage"
                    );

                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    var fileName = Guid.NewGuid() + Path.GetExtension(model.Image.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.Image.CopyToAsync(stream);
                    }

                    imagePath = $"https://admin.urest.in:8089/Assets/Blogsimage/{fileName}";
                }


                using var conn = new NpgsqlConnection(GetConnectionString());
                using var cmd = new NpgsqlCommand(@"
            INSERT INTO avaantra.blogs
            (title, slug, content, author_id, author_name, featured_image, tags, status)
            VALUES
            (@title, @slug, @content, @author_id, @author_name, @featured_image, @tags, @status)
            RETURNING id
        ", conn);

                cmd.Parameters.AddWithValue("@title", model.Title);
                cmd.Parameters.AddWithValue("@slug", model.Slug);
                cmd.Parameters.AddWithValue(
    "@content",
    NpgsqlDbType.Jsonb,
    string.IsNullOrEmpty(model.Content) ? "{}" : model.Content
);
                cmd.Parameters.AddWithValue("@author_id", (object?)model.AuthorId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@author_name", (object?)model.AuthorName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@featured_image", (object?)imagePath ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@tags", model.Tags ?? Array.Empty<string>());
                cmd.Parameters.AddWithValue("@status", model.Status ?? "draft");

                await conn.OpenAsync();
                var id = await cmd.ExecuteScalarAsync();

                return Ok(new { message = "Blog created", blogId = id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to create blog", error = ex.Message });
            }
        }


        // -----------------------------
        // PUT: api/blogs/{id}
        // -----------------------------
        [HttpPut("{id}")]
        public IActionResult UpdateBlog(Guid id, [FromBody] BlogsDto dto)
        {
            try
            {
                using var conn = new NpgsqlConnection(GetConnectionString());
                using var cmd = new NpgsqlCommand(@"
                    UPDATE avaantra.blogs
                    SET
                        title = @title,
                        slug = @slug,
                        content = @content,
                        author_id = @author_id,
                        author_name = @author_name,
                        featured_image = @featured_image,
                        tags = @tags,
                        status = @status,
                        updated_at = NOW()
                    WHERE id = @id
                ", conn);

                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@title", dto.Title);
                cmd.Parameters.AddWithValue("@slug", dto.Slug);
                cmd.Parameters.AddWithValue(
            "@content",
            NpgsqlDbType.Jsonb,
            string.IsNullOrEmpty(dto.Content) ? "{}" : dto.Content
        );

                cmd.Parameters.AddWithValue("@author_id", (object?)dto.AuthorId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@author_name", (object?)dto.AuthorName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@featured_image", (object?)dto.FeaturedImage ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@tags", dto.Tags ?? Array.Empty<string>());
                cmd.Parameters.AddWithValue("@status", dto.Status ?? "draft");

                conn.Open();
                int rows = cmd.ExecuteNonQuery();

                if (rows == 0)
                    return NotFound(new { message = "Blog not found" });

                return Ok(new { message = "Blog updated" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to update blog", error = ex.Message });
            }
        }

        // -----------------------------
        // DELETE: api/blogs/{id}
        // -----------------------------
        [HttpDelete("{id}")]
        public IActionResult DeleteBlog(Guid id)
        {
            try
            {
                using var conn = new NpgsqlConnection(GetConnectionString());
                using var cmd = new NpgsqlCommand(@"
                    DELETE FROM avaantra.blogs
                    WHERE id = @id
                ", conn);

                cmd.Parameters.AddWithValue("@id", id);

                conn.Open();
                int rows = cmd.ExecuteNonQuery();

                if (rows == 0)
                    return NotFound(new { message = "Blog not found" });

                return Ok(new { message = "Blog deleted" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to delete blog", error = ex.Message });
            }
        }

        // -----------------------------
        // Mapper
        // -----------------------------
        private BlogsDto MapBlog(NpgsqlDataReader reader)
        {
            return new BlogsDto
            {
                Id = Guid.Parse(reader["id"].ToString()),
                Title = reader["title"].ToString(),
                Slug = reader["slug"].ToString(),
                Content = reader["content"]?.ToString(),
                AuthorId = reader["author_id"] as Guid?,
                AuthorName = reader["author_name"]?.ToString(),
                FeaturedImage = reader["featured_image"]?.ToString(),
                Tags = reader["tags"] as string[],
                Status = reader["status"].ToString(),
                Views = Convert.ToInt32(reader["views"]),
                Likes = Convert.ToInt32(reader["likes"])
            };
        }


        //[HttpPost("CreateStructuredBlog")]
        //[RequestSizeLimit(50_000_000)]
        //public async Task<IActionResult> CreateStructuredBlog([FromForm] BlogDto model)
        //{
        //    try
        //    {
        //        if (string.IsNullOrWhiteSpace(model.Title))
        //            return BadRequest("Title is required");

        //        if (string.IsNullOrWhiteSpace(model.Slug))
        //            return BadRequest("Slug is required");

        //        if (string.IsNullOrWhiteSpace(model.SectionsJson))
        //            return BadRequest("Structured content (SectionsJson) is required");

        //        // ✅ NEW: Validate structured JSON
        //        if (!BlogContentValidator.IsValidStructuredContent(model.SectionsJson))
        //            return BadRequest("Invalid structured blog format");

        //        // 🔹 Optional Featured Image Upload (same as old logic)
        //        string featuredImagePath = null;

        //        if (model.Image != null)
        //        {
        //            var uploadsFolder = Path.Combine(
        //                Directory.GetCurrentDirectory(),
        //                "wwwroot",
        //                "Assets",
        //                "Blogsimage"
        //            );

        //            if (!Directory.Exists(uploadsFolder))
        //                Directory.CreateDirectory(uploadsFolder);

        //            var fileName = Guid.NewGuid() + Path.GetExtension(model.Image.FileName);
        //            var filePath = Path.Combine(uploadsFolder, fileName);

        //            using (var stream = new FileStream(filePath, FileMode.Create))
        //            {
        //                await model.Image.CopyToAsync(stream);
        //            }

        //            featuredImagePath = $"https://admin.urest.in:8089/Assets/Blogsimage/{fileName}";
        //        }

        //        // 🔹 Final Content (Structured JSON)
        //        string finalContent = model.SectionsJson;

        //        using var conn = new Npgsql.NpgsqlConnection(GetConnectionString());

        //        using var cmd = new Npgsql.NpgsqlCommand(@"
        //    INSERT INTO clinexy.blogs
        //    (id, title, slug, content, author_id, author_name, 
        //     featured_image, tags, status, views, likes, created_at, updated_at)
        //    VALUES
        //    (@id, @title, @slug, @content, @author_id, @author_name, 
        //     @featured_image, @tags, @status, 0, 0, NOW(), NOW())
        //    RETURNING id
        //", conn);

        //        var blogId = Guid.NewGuid();

        //        cmd.Parameters.AddWithValue("@id", blogId);
        //        cmd.Parameters.AddWithValue("@title", model.Title);
        //        cmd.Parameters.AddWithValue("@slug", model.Slug);

        //        cmd.Parameters.AddWithValue(
        //            "@content",
        //            NpgsqlTypes.NpgsqlDbType.Jsonb,
        //            finalContent
        //        );

        //        cmd.Parameters.AddWithValue("@author_id",
        //            (object?)model.AuthorId ?? DBNull.Value);

        //        cmd.Parameters.AddWithValue("@author_name",
        //            (object?)model.AuthorName ?? DBNull.Value);

        //        cmd.Parameters.AddWithValue("@featured_image",
        //            (object?)featuredImagePath ?? DBNull.Value);

        //        cmd.Parameters.AddWithValue("@tags",
        //            model.Tags ?? Array.Empty<string>());

        //        cmd.Parameters.AddWithValue("@status",
        //            string.IsNullOrEmpty(model.Status) ? "draft" : model.Status);

        //        await conn.OpenAsync();
        //        var insertedId = await cmd.ExecuteScalarAsync();

        //        return Ok(new
        //        {
        //            message = "Structured blog created successfully",
        //            blogId = insertedId
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new
        //        {
        //            message = "Failed to create structured blog",
        //            error = ex.Message
        //        });
        //    }
        //}

        //public static class BlogContentValidator
        //{
        //    public static bool IsValidStructuredContent(string json)
        //    {
        //        if (string.IsNullOrWhiteSpace(json))
        //            return false;

        //        try
        //        {
        //            var doc = System.Text.Json.JsonDocument.Parse(json);

        //            if (!doc.RootElement.TryGetProperty("type", out var typeProp))
        //                return false;

        //            if (typeProp.GetString() != "structured")
        //                return false;

        //            if (!doc.RootElement.TryGetProperty("sections", out var sectionsProp))
        //                return false;

        //            if (sectionsProp.ValueKind != System.Text.Json.JsonValueKind.Array)
        //                return false;

        //            return true;
        //        }
        //        catch
        //        {
        //            return false;
        //        }
        //    }
        //}

        [HttpPost("UploadSectionImage")]
        [RequestSizeLimit(10_000_000)]
        public async Task<IActionResult> UploadSectionImage(IFormFile image)
        {
            if (image == null || image.Length == 0)
                return BadRequest("No image provided");

            var uploadsFolder = Path.Combine(
                Directory.GetCurrentDirectory(), "wwwroot", "Assets", "Blogsimage"
            );

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var fileName = Guid.NewGuid() + Path.GetExtension(image.FileName);
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
                await image.CopyToAsync(stream);

            return Ok(new
            {
                url = $"https://admin.urest.in:8089/Assets/Blogsimage/{fileName}"
            });
        }

        // ─────────────────────────────────────────
        // API 2: Create Structured Blog
        // POST /api/Blog/CreateStructuredBlog
        // ─────────────────────────────────────────
        [HttpPost("CreateStructuredBlog")]
        public async Task<IActionResult> CreateStructuredBlog([FromBody] StructuredBlogRequests model)
        {
            try
            {
                // ── Validations ──
                if (string.IsNullOrWhiteSpace(model.Title))
                    return BadRequest("Title is required");

                if (string.IsNullOrWhiteSpace(model.Slug))
                    return BadRequest("Slug is required");

                if (model.Sections == null || model.Sections.Count == 0)
                    return BadRequest("At least one section is required");

                // ── First section image = featured image ──
                string? featuredImage = model.Sections[0].ImageUrl;

                // ── Build structured JSON ──
                var structuredContent = new
                {
                    type = "structured",
                    sections = model.Sections.Select((s, i) => new
                    {
                        order = i + 1,
                        imageUrl = s.ImageUrl,
                        heading = s.Heading,
                        text = s.Text
                    })
                };

                string finalContent = System.Text.Json.JsonSerializer.Serialize(structuredContent);

                // ── Insert into DB ──
                using var conn = new Npgsql.NpgsqlConnection(GetConnectionString());
                using var cmd = new Npgsql.NpgsqlCommand(@"
                INSERT INTO avaantra.blogs
                (id, title, slug, content, author_id, author_name, featured_image, tags, status, views, likes, created_at, updated_at)
                VALUES
                (@id, @title, @slug, @content, @author_id, @author_name, @featured_image, @tags, @status, 0, 0, NOW(), NOW())
                RETURNING id
            ", conn);

                var blogId = Guid.NewGuid();

                cmd.Parameters.AddWithValue("@id", blogId);
                cmd.Parameters.AddWithValue("@title", model.Title);
                cmd.Parameters.AddWithValue("@slug", model.Slug);
                cmd.Parameters.AddWithValue("@content", NpgsqlTypes.NpgsqlDbType.Jsonb, finalContent);
                cmd.Parameters.AddWithValue("@author_id", (object?)model.AuthorId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@author_name", (object?)model.AuthorName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@featured_image", (object?)featuredImage ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@tags", (object?)model.Tags ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@status", string.IsNullOrEmpty(model.Status) ? "draft" : model.Status);

                await conn.OpenAsync();
                var insertedId = await cmd.ExecuteScalarAsync();

                return Ok(new
                {
                    message = "Blog created successfully",
                    blogId = insertedId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Failed to create blog",
                    error = ex.Message
                });
            }
        }

        // ─────────────────────────────────────────
        // GET Single Blog by Slug
        // GET /api/Blog/GetBlog/{slug}
        // ─────────────────────────────────────────
        [HttpGet("GetBlog/{slug}")]
        public async Task<IActionResult> GetBlog(string slug)
        {
            try
            {
                using var conn = new Npgsql.NpgsqlConnection(GetConnectionString());
                using var cmd = new Npgsql.NpgsqlCommand(@"
            SELECT id, title, slug, content, author_id, author_name, 
                   featured_image, tags, status, views, likes, created_at
            FROM avaantra.blogs
            WHERE slug = @slug
        ", conn);

                cmd.Parameters.AddWithValue("@slug", slug);
                await conn.OpenAsync();

                using var reader = await cmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                    return NotFound(new { message = "Blog not found" });

                // Parse the JSONB content column
                var contentJson = reader.GetString(3);
                var content = System.Text.Json.JsonSerializer.Deserialize<StructuredContents>(contentJson);

                return Ok(new
                {
                    id = reader.GetGuid(0),
                    title = reader.GetString(1),
                    slug = reader.GetString(2),
                    authorId = reader.IsDBNull(4) ? null : reader.GetValue(4),
                    authorName = reader.IsDBNull(5) ? null : reader.GetString(5),
                    featuredImage = reader.IsDBNull(6) ? null : reader.GetString(6),
                    tags = reader.IsDBNull(7) ? null : reader.GetValue(7),
                    status = reader.GetString(8),
                    views = reader.GetInt32(9),
                    likes = reader.GetInt32(10),
                    createdAt = reader.GetDateTime(11),
                    sections = content?.Sections  // ✅ typed list with image+heading+text
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch blog", error = ex.Message });
            }
        }

        // ─────────────────────────────────────────
        // GET All Blogs with full sections
        // GET /api/Blog/GetAllBlogs
        // ─────────────────────────────────────────
        [HttpGet("GetAllBlogs")]
        public async Task<IActionResult> GetAllBlogs()
        {
            try
            {
                using var conn = new Npgsql.NpgsqlConnection(GetConnectionString());
                using var cmd = new Npgsql.NpgsqlCommand(@"
            SELECT id, title, slug, content, author_id, author_name, 
                   featured_image, tags, status, views, likes, created_at
            FROM avaantra.blogs
            ORDER BY created_at DESC
        ", conn);

                await conn.OpenAsync();
                using var reader = await cmd.ExecuteReaderAsync();

                var blogs = new List<object>();

                while (await reader.ReadAsync())
                {
                    // Parse sections from JSONB content column
                    var contentJson = reader.GetString(3);
                    var content = System.Text.Json.JsonSerializer.Deserialize<StructuredContents>(contentJson);

                    blogs.Add(new
                    {
                        id = reader.GetGuid(0),
                        title = reader.GetString(1),
                        slug = reader.GetString(2),
                        authorId = reader.IsDBNull(4) ? null : reader.GetValue(4),
                        authorName = reader.IsDBNull(5) ? null : reader.GetString(5),
                        featuredImage = reader.IsDBNull(6) ? null : reader.GetString(6),
                        tags = reader.IsDBNull(7) ? null : reader.GetValue(7),
                        status = reader.GetString(8),
                        views = reader.GetInt32(9),
                        likes = reader.GetInt32(10),
                        createdAt = reader.GetDateTime(11),
                        sections = content?.Sections  // ✅ full sections included
                    });
                }

                return Ok(new
                {
                    total = blogs.Count,
                    blogs
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch blogs", error = ex.Message });
            }
        }

        [HttpPut("UpdateBlog/{id}")]
        public async Task<IActionResult> UpdateBlog(Guid id, [FromBody] StructuredBlogRequests model)
        {
            try
            {
                // ── Validations ──
                if (string.IsNullOrWhiteSpace(model.Title))
                    return BadRequest("Title is required");

                if (string.IsNullOrWhiteSpace(model.Slug))
                    return BadRequest("Slug is required");

                if (model.Sections == null || model.Sections.Count == 0)
                    return BadRequest("At least one section is required");

                // ── Featured Image ──
                string? featuredImage = !string.IsNullOrEmpty(model.FeaturedImage)
                    ? model.FeaturedImage
                    : model.Sections[0].ImageUrl;

                // ── Build structured JSON ──
                var structuredContent = new
                {
                    type = "structured",
                    sections = model.Sections.Select((s, i) => new
                    {
                        order = i + 1,
                        imageUrl = s.ImageUrl,
                        heading = s.Heading,
                        text = s.Text
                    })
                };

                string finalContent = System.Text.Json.JsonSerializer.Serialize(structuredContent);

                // ── Update in DB ──
                using var conn = new Npgsql.NpgsqlConnection(GetConnectionString());
                using var cmd = new Npgsql.NpgsqlCommand(@"
            UPDATE avaantra.blogs
            SET title          = @title,
                slug           = @slug,
                content        = @content,
                author_id      = @author_id,
                author_name    = @author_name,
                featured_image = @featured_image,
                tags           = @tags,
                status         = @status,
                updated_at     = NOW()
            WHERE id = @id
            RETURNING id
        ", conn);

                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@title", model.Title);
                cmd.Parameters.AddWithValue("@slug", model.Slug);
                cmd.Parameters.AddWithValue("@content", NpgsqlTypes.NpgsqlDbType.Jsonb, finalContent);
                cmd.Parameters.AddWithValue("@author_id", (object?)model.AuthorId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@author_name", (object?)model.AuthorName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@featured_image", (object?)featuredImage ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@tags", (object?)model.Tags ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@status", string.IsNullOrEmpty(model.Status) ? "draft" : model.Status);

                await conn.OpenAsync();
                var updatedId = await cmd.ExecuteScalarAsync();

                if (updatedId == null)
                    return NotFound(new { message = "Blog not found" });

                return Ok(new
                {
                    message = "Blog updated successfully",
                    blogId = updatedId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Failed to update blog",
                    error = ex.Message
                });
            }
        }

    }

}

