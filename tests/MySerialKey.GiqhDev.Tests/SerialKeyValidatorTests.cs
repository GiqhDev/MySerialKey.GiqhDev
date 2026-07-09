using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySerialKey.GiqhDev;

namespace MySerialKey.GiqhDev.Tests;

[TestClass]
public sealed class SerialKeyValidatorTests
{
    [TestMethod]
    public void Validate_AcceptsGeneratedSerial()
    {
        string serial = SerialKeyGenerator.Generate();

        SerialValidationResult result = SerialKeyValidator.Validate(serial);

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(SerialValidationError.None, result.Error);
        Assert.AreEqual(serial, result.NormalizedSerial);
    }

    [TestMethod]
    public void Validate_RejectsNull()
    {
        SerialValidationResult result = SerialKeyValidator.Validate(null);

        Assert.IsFalse(result.IsValid);
        Assert.AreEqual(SerialValidationError.NullOrEmpty, result.Error);
    }

    [TestMethod]
    public void Validate_RejectsEmpty()
    {
        SerialValidationResult result = SerialKeyValidator.Validate(" ");

        Assert.IsFalse(result.IsValid);
        Assert.AreEqual(SerialValidationError.NullOrEmpty, result.Error);
    }

    [TestMethod]
    public void Validate_RejectsInvalidLength()
    {
        SerialValidationResult result = SerialKeyValidator.Validate("ABCDEFG");

        Assert.IsFalse(result.IsValid);
        Assert.AreEqual(SerialValidationError.InvalidLength, result.Error);
    }

    [TestMethod]
    public void Validate_RejectsInvalidSegmentCount()
    {
        SerialValidationResult result = SerialKeyValidator.Validate("AAAAA-BBBBBB-CCCCC-DDDDDD-EEEEE");

        Assert.IsFalse(result.IsValid);
        Assert.AreEqual(SerialValidationError.InvalidLength, result.Error);
    }

    [TestMethod]
    public void Validate_RejectsInvalidCharacter()
    {
        SerialValidationResult result = SerialKeyValidator.Validate("A7K9P-X2M8Q4-B6N3R-Z9T5W0");

        Assert.IsFalse(result.IsValid);
        Assert.AreEqual(SerialValidationError.InvalidCharacter, result.Error);
    }

    [TestMethod]
    public void Validate_NormalizesLowercaseAndSpaces()
    {
        SerialValidationResult result = SerialKeyValidator.Validate("a7k9p x2m8q4 b6n3r z9t5w8");

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual("A7K9P-X2M8Q4-B6N3R-Z9T5W8", result.NormalizedSerial);
    }

    [TestMethod]
    public void Normalize_FormatsRawInput()
    {
        string normalized = SerialKeyNormalizer.Normalize("a7k9px2m8q4b6n3rz9t5w8");

        Assert.AreEqual("A7K9P-X2M8Q4-B6N3R-Z9T5W8", normalized);
    }

    [TestMethod]
    public void Validate_IsValidReturnsConvenienceBoolean()
    {
        string serial = SerialKeyGenerator.Generate();

        Assert.IsTrue(SerialKeyValidator.IsValid(serial));
    }
}
