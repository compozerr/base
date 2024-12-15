namespace Auth.Models;

public class GithubUserLogin : UserLogin
{
    public GithubUserLogin()
    {
        Provider = Provider.GitHub;
    }

    public required string AccessToken { get; set; }
}
