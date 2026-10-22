using System.Text;
using MySerialKey.GiqhDev.Licensing;

namespace MySerialKey.GiqhDev.Activation.Offline.Internal;

internal static class ActivationResponseSerializer
{
    private const int SchemaVersion = 1;
    private const int MaximumStringBytes = 131072;
    private static readonly byte[] Magic = { 0x4D, 0x53, 0x41, 0x50 };
    private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

    /// <summary>Encodes response metadata and the embedded MSL1 license into a deterministic payload.</summary>
    public static byte[] Serialize(ActivationResponsePayload values)
    {
        Validate(values);
        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream, StrictUtf8, true))
        {
            writer.Write(Magic);
            writer.Write(SchemaVersion);
            WriteString(writer, values.RequestId);
            writer.Write(values.RequestNonceHash.Length);
            writer.Write(values.RequestNonceHash);
            WriteString(writer, values.ProductCode);
            WriteString(writer, values.InstallationId);
            writer.Write(values.IssuedAtUtc.UtcDateTime.Ticks);
            writer.Write(values.ExpiresAtUtc.UtcDateTime.Ticks);
            WriteString(writer, values.License.ToString());
            writer.Flush();
            return stream.ToArray();
        }
    }

    /// <summary>Decodes and validates a versioned activation-response payload.</summary>
    public static ActivationResponsePayload Deserialize(byte[] payload)
    {
        try
        {
            using (var stream = new MemoryStream(payload, false))
            using (var reader = new BinaryReader(stream, StrictUtf8, true))
            {
                byte[] magic = reader.ReadBytes(Magic.Length);
                if (!magic.SequenceEqual(Magic) || reader.ReadInt32() != SchemaVersion)
                {
                    throw new FormatException("Unsupported activation-response schema.");
                }

                string requestId = ReadString(reader);
                int hashLength = reader.ReadInt32();
                if (hashLength != 32)
                {
                    throw new FormatException("Invalid activation-response nonce hash.");
                }

                byte[] nonceHash = reader.ReadBytes(hashLength);
                if (nonceHash.Length != hashLength)
                {
                    throw new FormatException("The nonce hash is truncated.");
                }

                string productCode = ReadString(reader);
                string installationId = ReadString(reader);
                DateTimeOffset issuedAtUtc = ReadUtc(reader);
                DateTimeOffset expiresAtUtc = ReadUtc(reader);
                if (!SignedLicense.TryParse(ReadString(reader), out SignedLicense? license))
                {
                    throw new FormatException("The embedded signed license is malformed.");
                }

                if (stream.Position != stream.Length)
                {
                    throw new FormatException("The activation response contains trailing data.");
                }

                var values = new ActivationResponsePayload(
                    requestId,
                    nonceHash,
                    productCode,
                    installationId,
                    issuedAtUtc,
                    expiresAtUtc,
                    license!);
                Validate(values);
                return values;
            }
        }
        catch (Exception exception) when (
            exception is EndOfStreamException ||
            exception is DecoderFallbackException ||
            exception is ArgumentException)
        {
            throw new FormatException("The activation-response payload is malformed.", exception);
        }
    }

    /// <summary>Validates response chronology, identifiers, hashes, and embedded token limits.</summary>
    private static void Validate(ActivationResponsePayload values)
    {
        ValidateText(values.RequestId, "Request identifier", 4096);
        if (!Guid.TryParseExact(values.RequestId, "N", out _))
        {
            throw new ArgumentException("Request identifier must be a GUID in N format.");
        }

        if (values.RequestNonceHash.Length != 32)
        {
            throw new ArgumentException("Request nonce hash must contain 32 bytes.");
        }

        ValidateText(values.ProductCode, "Product code", 4096);
        ValidateText(values.InstallationId, "Installation identifier", 4096);
        if (values.ExpiresAtUtc <= values.IssuedAtUtc)
        {
            throw new ArgumentException("Response expiration must be later than issue time.");
        }
    }

    /// <summary>Reads a UTC timestamp while rejecting invalid tick values.</summary>
    private static DateTimeOffset ReadUtc(BinaryReader reader)
    {
        return new DateTimeOffset(new DateTime(reader.ReadInt64(), DateTimeKind.Utc));
    }

    /// <summary>Validates required response text against a field-specific UTF-8 limit.</summary>
    private static void ValidateText(string value, string name, int maximumBytes)
    {
        if (string.IsNullOrWhiteSpace(value) || StrictUtf8.GetByteCount(value) > maximumBytes)
        {
            throw new ArgumentException(name + " is empty or too long.");
        }
    }

    /// <summary>Writes a UTF-8 response field with its byte length.</summary>
    private static void WriteString(BinaryWriter writer, string value)
    {
        byte[] bytes = StrictUtf8.GetBytes(value);
        if (bytes.Length > MaximumStringBytes)
        {
            throw new ArgumentException("Activation-response text is too long.");
        }

        writer.Write(bytes.Length);
        writer.Write(bytes);
    }

    /// <summary>Reads a bounded response field using strict UTF-8 decoding.</summary>
    private static string ReadString(BinaryReader reader)
    {
        int length = reader.ReadInt32();
        if (length < 0 || length > MaximumStringBytes)
        {
            throw new FormatException("Invalid activation-response text length.");
        }

        byte[] bytes = reader.ReadBytes(length);
        if (bytes.Length != length)
        {
            throw new FormatException("Activation-response text is truncated.");
        }

        return StrictUtf8.GetString(bytes);
    }
}
