using System.Text.RegularExpressions;

namespace Mail;

public abstract class ReactEmailTemplate(string htmlFilePath)
{
    public string HtmlFilePath { get; } = htmlFilePath;

    public async Task<string> GetHtmlBodyAsync()
    {
        var htmlContent = await File.ReadAllTextAsync(HtmlFilePath);

        GetType()
            .GetProperties()
            .ToList()
            .ForEach(prop =>
            {
                var value = prop.GetValue(this)?.ToString() ?? string.Empty;
                htmlContent = Regex.Replace(
                    htmlContent,
                    $"%\\s*{prop.Name}\\s*%",
                    value,
                    RegexOptions.IgnoreCase);
            });

        await ValidateEverythingIsReplacedOrThrow(htmlContent);

        return htmlContent;
    }

    private static async Task ValidateEverythingIsReplacedOrThrow(string htmlContent)
    {
        var unmatchedPlaceholders = Regex.Matches(htmlContent, "%\\s*\\w+\\s*%")
                                         .Select(m => m.Value)
                                         .Distinct()
                                         .ToList();

        if (unmatchedPlaceholders.Count > 0)
            throw new InvalidOperationException($"Unmatched placeholder found in template: {unmatchedPlaceholders.Aggregate((a, b) => a + ", " + b)}");
    }
}