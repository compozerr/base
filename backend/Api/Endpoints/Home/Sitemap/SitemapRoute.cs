using System.Text;

namespace Api.Endpoints.Home.Sitemap;

public static class SitemapRoute
{
    public static RouteHandlerBuilder AddSitemapRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet("/sitemap.xml", () =>
        {
            var lastmod = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var baseUrl = "https://compozerr.com";

            var urls = new (string path, string priority, string changefreq)[]
            {
                ("/", "1.0", "weekly"),
                ("/n8n", "0.9", "weekly"),
                ("/pricing", "0.8", "weekly"),
                ("/contact", "0.5", "monthly"),
                ("/privacy-policy", "0.3", "monthly"),
                ("/terms-of-service", "0.3", "monthly"),
            };

            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

            foreach (var (path, priority, changefreq) in urls)
            {
                sb.AppendLine("  <url>");
                sb.AppendLine($"    <loc>{baseUrl}{path}</loc>");
                sb.AppendLine($"    <lastmod>{lastmod}</lastmod>");
                sb.AppendLine($"    <changefreq>{changefreq}</changefreq>");
                sb.AppendLine($"    <priority>{priority}</priority>");
                sb.AppendLine("  </url>");
            }

            sb.AppendLine("</urlset>");

            return Results.Content(sb.ToString(), "application/xml", Encoding.UTF8);
        })
        .ExcludeFromDescription();
    }
}
