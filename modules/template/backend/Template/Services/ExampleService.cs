namespace Template.Services;

public interface IExampleService
{
    string GetExampleName();
}

public class ExampleService : IExampleService
{
    public string GetExampleName()
    {
        return "Mom";
    }
}