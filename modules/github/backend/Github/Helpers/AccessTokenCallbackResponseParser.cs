namespace Github.Helpers;

using System.Web;

public record GithubAccessTokenSuccessResponse(string AccessToken);
public record GithubAccessTokenErrorResponse(string Error, string ErrorDescription, string ErrorUri);
public class GitHubAccessTokenResponse
{
    public GithubAccessTokenSuccessResponse? Success { get; internal set; }
    public GithubAccessTokenErrorResponse? Error { get; internal set; }

    public bool IsSuccess => Error == null;
}

public static class AccessTokenCallbackResponseParser
{
    public static GitHubAccessTokenResponse ParseResponse(string queryString)
    {
        queryString = queryString.TrimStart('?');

        var parsed = HttpUtility.ParseQueryString(queryString);

        var response = new GitHubAccessTokenResponse();

        if (parsed["error"] == null)
        {
            response.Success = new GithubAccessTokenSuccessResponse(
                AccessToken: parsed["access_token"] ?? throw new ArgumentException("access_token is required")
            );
        }
        else
        {
            response.Error = new GithubAccessTokenErrorResponse
            (
                Error: parsed["error"] ?? throw new ArgumentException("error is required"),
                ErrorDescription: parsed["error_description"]?.Replace("+", " ") ?? throw new ArgumentException("error_description is required"),
                ErrorUri: parsed["error_uri"] ?? throw new ArgumentException("error_uri is required")
            );
        }

        return response;
    }
}