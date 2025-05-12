namespace Cli.Endpoints.Modules.Add;

/// <summary>
/// Represents the result of a module operation, containing either a successful module or an error message.
/// </summary>
public sealed class ModuleResult
{
    /// <summary>
    /// Indicates whether the operation was successful.
    /// </summary>
    public bool Success { get; private set; }
    
    /// <summary>
    /// The module that was successfully retrieved or created. Only valid if Success is true.
    /// </summary>
    public ModuleDto? Module { get; private set; }
    
    /// <summary>
    /// The error message if the operation failed. Only valid if Success is false.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    private ModuleResult(bool success, ModuleDto? module, string? errorMessage)
    {
        Success = success;
        Module = module;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Creates a successful result with the specified module.
    /// </summary>
    /// <param name="module">The module that was successfully retrieved or created.</param>
    /// <returns>A successful ModuleResult containing the module.</returns>
    public static ModuleResult Ok(ModuleDto module) => 
        new ModuleResult(true, module, null);

    /// <summary>
    /// Creates a failed result with the specified error message.
    /// </summary>
    /// <param name="errorMessage">The error message describing why the operation failed.</param>
    /// <returns>A failed ModuleResult containing the error message.</returns>
    public static ModuleResult Fail(string errorMessage) => 
        new ModuleResult(false, null, errorMessage);
}
