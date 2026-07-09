using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySerialKey.GiqhDev;

namespace MySerialKey.GiqhDev.Tests;

[TestClass]
public sealed class SerialAuthenticationTests
{
    private static readonly byte[] SecretA = Encoding.UTF8.GetBytes("secret-a-for-tests");
    private static readonly byte[] SecretB = Encoding.UTF8.GetBytes("secret-b-for-tests");

    [TestMethod]
    public void ValidateAuthenticated_AcceptsMatchingSecret()
    {
        string serial = SerialKeyGenerator.GenerateAuthenticated(SecretA);

        SerialValidationResult result = SerialKeyValidator.ValidateAuthenticated(serial, SecretA);

        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public void ValidateAuthenticated_RejectsDifferentSecret()
    {
        string serial = SerialKeyGenerator.GenerateAuthenticated(SecretA);

        SerialValidationResult result = SerialKeyValidator.ValidateAuthenticated(serial, SecretB);

        Assert.IsFalse(result.IsValid);
        Assert.AreEqual(SerialValidationError.AuthenticationFailed, result.Error);
    }

    [TestMethod]
    public void ValidateAuthenticated_RejectsAlteredCharacter()
    {
        string serial = SerialKeyGenerator.GenerateAuthenticated(SecretA);
        char replacement = serial[0] == 'A' ? 'B' : 'A';
        string altered = replacement + serial.Substring(1);

        SerialValidationResult result = SerialKeyValidator.ValidateAuthenticated(altered, SecretA);

        Assert.IsFalse(result.IsValid);
    }

    [TestMethod]
    public void ValidateAuthenticated_RejectsAlteredSeparatorStructurally()
    {
        string serial = SerialKeyGenerator.GenerateAuthenticated(SecretA);
        string altered = serial.Remove(5, 1).Insert(5, ":");

        SerialValidationResult result = SerialKeyValidator.ValidateAuthenticated(altered, SecretA);

        Assert.IsFalse(result.IsValid);
    }

    [TestMethod]
    public void ValidateAuthenticated_RejectsTruncatedInput()
    {
        string serial = SerialKeyGenerator.GenerateAuthenticated(SecretA);

        SerialValidationResult result = SerialKeyValidator.ValidateAuthenticated(serial.Substring(0, serial.Length - 1), SecretA);

        Assert.IsFalse(result.IsValid);
    }

    [TestMethod]
    public void ValidateAuthenticated_RejectsExtendedInput()
    {
        string serial = SerialKeyGenerator.GenerateAuthenticated(SecretA);

        SerialValidationResult result = SerialKeyValidator.ValidateAuthenticated(serial + "A", SecretA);

        Assert.IsFalse(result.IsValid);
    }
}
