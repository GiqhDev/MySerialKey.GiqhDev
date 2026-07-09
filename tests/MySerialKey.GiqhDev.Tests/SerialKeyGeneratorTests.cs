using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySerialKey.GiqhDev;

namespace MySerialKey.GiqhDev.Tests;

[TestClass]
public sealed class SerialKeyGeneratorTests
{
    [TestMethod]
    public void Generate_ReturnsSerialWithDefaultPattern()
    {
        string serial = SerialKeyGenerator.Generate();

        Assert.AreEqual(25, serial.Length);
        Assert.AreEqual('-', serial[5]);
        Assert.AreEqual('-', serial[12]);
        Assert.AreEqual('-', serial[18]);
        Assert.AreEqual(22, serial.Count(character => character != '-'));
        Assert.IsTrue(SerialKeyValidator.Validate(serial).IsValid);
    }

    [TestMethod]
    public void Generate_UsesAllowedCharacters()
    {
        string serial = SerialKeyGenerator.Generate();

        foreach (char character in serial.Replace("-", string.Empty))
        {
            Assert.IsTrue(SerialKeyDefaults.DefaultAlphabet.Contains(character));
        }
    }

    [TestMethod]
    public void Generate_ProducesDifferentValues()
    {
        var serials = new HashSet<string>();

        for (int i = 0; i < 100; i++)
        {
            serials.Add(SerialKeyGenerator.Generate());
        }

        Assert.IsTrue(serials.Count > 95);
    }

    [TestMethod]
    public void Generate_RespectsCustomOptions()
    {
        var options = new SerialKeyGenerationOptions
        {
            Segments = new[] { 4, 4 },
            Separator = ':',
            Alphabet = "ABCD2345"
        };

        string serial = SerialKeyGenerator.Generate(options);

        Assert.AreEqual(9, serial.Length);
        Assert.AreEqual(':', serial[4]);
        Assert.IsTrue(SerialKeyValidator.Validate(serial, options).IsValid);
    }

    [TestMethod]
    public void Generate_ThrowsForInvalidOptions()
    {
        var options = new SerialKeyGenerationOptions
        {
            Segments = new[] { 5 },
            Alphabet = "AAAA"
        };

        try
        {
            SerialKeyGenerator.Generate(options);
            Assert.Fail("Expected invalid options to throw.");
        }
        catch (ArgumentException)
        {
        }
    }

    [TestMethod]
    public void Generate_IsThreadSafe()
    {
        var serials = new HashSet<string>();
        object gate = new object();

        Parallel.For(0, 200, _ =>
        {
            string serial = SerialKeyGenerator.Generate();
            Assert.IsTrue(SerialKeyValidator.Validate(serial).IsValid);

            lock (gate)
            {
                serials.Add(serial);
            }
        });

        Assert.AreEqual(200, serials.Count);
    }
}
