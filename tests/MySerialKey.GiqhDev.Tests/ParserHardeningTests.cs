using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySerialKey.GiqhDev.Activation.Offline;
using MySerialKey.GiqhDev.Licensing;
using MySerialKey.GiqhDev.Licensing.Revocation;

namespace MySerialKey.GiqhDev.Tests;

[TestClass]
public sealed class ParserHardeningTests
{
    [TestMethod]
    public void PublicTokenParsers_HandleDeterministicFuzzWithoutUnexpectedExceptions()
    {
        var random = new Random(20261022);
        for (int iteration = 0; iteration < 5000; iteration++)
        {
            string arbitrary = RandomAscii(random, random.Next(0, 2049));
            AssertParserContracts(arbitrary);

            string structured = string.Concat(
                Prefix(random),
                ".",
                Base64UrlLike(random, random.Next(0, 65)),
                ".",
                Base64UrlLike(random, random.Next(0, 513)),
                ".",
                Base64UrlLike(random, random.Next(0, 129)));
            AssertParserContracts(structured);
        }
    }

    [TestMethod]
    public void PublicTokenParsers_RejectInputsBeyondTheirMaximumEnvelopeSize()
    {
        string oversized = new string('A', 1024 * 1024 + 1);

        Assert.IsFalse(SignedLicense.TryParse(oversized, out SignedLicense? license));
        Assert.IsNull(license);
        Assert.IsFalse(ActivationRequest.TryParse(oversized, out ActivationRequest? request));
        Assert.IsNull(request);
        Assert.IsFalse(ActivationResponse.TryParse(oversized, out ActivationResponse? response));
        Assert.IsNull(response);
        Assert.IsFalse(
            SignedRevocationList.TryParse(oversized, out SignedRevocationList? revocationList));
        Assert.IsNull(revocationList);
    }

    private static void AssertParserContracts(string token)
    {
        bool licenseParsed = SignedLicense.TryParse(token, out SignedLicense? license);
        bool requestParsed = ActivationRequest.TryParse(token, out ActivationRequest? request);
        bool responseParsed = ActivationResponse.TryParse(token, out ActivationResponse? response);
        bool revocationParsed = SignedRevocationList.TryParse(
            token,
            out SignedRevocationList? revocationList);

        if (!licenseParsed)
        {
            Assert.IsNull(license);
        }

        if (!requestParsed)
        {
            Assert.IsNull(request);
        }

        if (!responseParsed)
        {
            Assert.IsNull(response);
        }

        if (!revocationParsed)
        {
            Assert.IsNull(revocationList);
        }
    }

    private static string Prefix(Random random)
    {
        string[] prefixes =
        {
            "MSL1.ES256",
            "MSAR1",
            "MSAP1.ES256",
            "MSRL1.ES256",
            RandomAscii(random, random.Next(0, 17))
        };
        return prefixes[random.Next(prefixes.Length)];
    }

    private static string Base64UrlLike(Random random, int length)
    {
        const string alphabet =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";
        var builder = new StringBuilder(length);
        for (int index = 0; index < length; index++)
        {
            builder.Append(alphabet[random.Next(alphabet.Length)]);
        }

        return builder.ToString();
    }

    private static string RandomAscii(Random random, int length)
    {
        var builder = new StringBuilder(length);
        for (int index = 0; index < length; index++)
        {
            builder.Append((char)random.Next(0x20, 0x7F));
        }

        return builder.ToString();
    }
}
