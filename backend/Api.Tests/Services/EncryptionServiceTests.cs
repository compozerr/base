using Api.Options;
using Api.Services;

namespace Api.Tests.Services;

public class EncryptionServiceTests
{
    private readonly EncryptionService _encryptionService;
    private const string TestSecret = "TestSecretKey123";

    public EncryptionServiceTests()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new EncryptionOptions { Secret = TestSecret });
        _encryptionService = new EncryptionService(options);
    }

    [Fact]
    public void Encrypt_ShouldEncryptPlainText()
    {
        // Arrange
        var plainText = "Hello World!";

        // Act
        var encrypted = _encryptionService.Encrypt(plainText);

        // Assert
        Assert.NotEqual(plainText, encrypted);
        Assert.NotEmpty(encrypted);
    }

    [Fact]
    public void Decrypt_ShouldDecryptToOriginalText()
    {
        // Arrange
        var originalText = "Hello World!";
        var encrypted = _encryptionService.Encrypt(originalText);

        // Act
        var decrypted = _encryptionService.Decrypt(encrypted);

        // Assert
        Assert.Equal(originalText, decrypted);
    }

    [Fact]
    public void EncryptDecrypt_WithEmptyString_ShouldWork()
    {
        // Arrange
        var emptyText = string.Empty;

        // Act
        var encrypted = _encryptionService.Encrypt(emptyText);
        var decrypted = _encryptionService.Decrypt(encrypted);

        // Assert
        Assert.Equal(emptyText, decrypted);
    }

    [Fact]
    public void EncryptDecrypt_WithLongText_ShouldWork()
    {
        // Arrange
        var longText = new string('x', 1000);

        // Act
        var encrypted = _encryptionService.Encrypt(longText);
        var decrypted = _encryptionService.Decrypt(encrypted);

        // Assert
        Assert.Equal(longText, decrypted);
    }
}