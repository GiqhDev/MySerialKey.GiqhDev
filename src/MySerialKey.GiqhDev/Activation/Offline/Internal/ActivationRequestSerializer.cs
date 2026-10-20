using System.Text;

namespace MySerialKey.GiqhDev.Activation.Offline.Internal;

internal static class ActivationRequestSerializer
{
    private const int SchemaVersion = 1;
    private const int MaximumStringBytes = 4096;
    private static readonly byte[] Magic = { 0x4D, 0x53, 0x41, 0x52 };
    private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

    public static void Validate(ActivationRequest request)
    {
        ValidateRequired(request.RequestId, "Request identifier");
        if (!Guid.TryParseExact(request.RequestId, "N", out _))
        {
            throw new ArgumentException("Request identifier must be a GUID in N format.");
        }

        if (request.NonceBytes.Length != 32)
        {
            throw new ArgumentException("Request nonce must contain 32 bytes.");
        }

        ValidateRequired(request.ProductCode, "Product code");
        ValidateRequired(request.InstallationId, "Installation identifier");
        ValidateOptional(request.ApplicationVersion, "Application version");
        if (request.ExpiresAtUtc <= request.CreatedAtUtc)
        {
            throw new ArgumentException("Request expiration must be later than creation.");
        }
    }

    public static byte[] Serialize(ActivationRequest request)
    {
        Validate(request);
        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream, StrictUtf8, true))
        {
            writer.Write(Magic);
            writer.Write(SchemaVersion);
            WriteString(writer, request.RequestId);
            writer.Write(request.NonceBytes.Length);
            writer.Write(request.NonceBytes);
            WriteString(writer, request.ProductCode);
            WriteString(writer, request.InstallationId);
            writer.Write(request.CreatedAtUtc.UtcDateTime.Ticks);
            writer.Write(request.ExpiresAtUtc.UtcDateTime.Ticks);
            WriteOptional(writer, request.ApplicationVersion);
            writer.Flush();
            return stream.ToArray();
        }
    }

    public static ActivationRequest Deserialize(byte[] payload)
    {
        try
        {
            using (var stream = new MemoryStream(payload, false))
            using (var reader = new BinaryReader(stream, StrictUtf8, true))
            {
                byte[] magic = reader.ReadBytes(Magic.Length);
                if (!magic.SequenceEqual(Magic) || reader.ReadInt32() != SchemaVersion)
                {
                    throw new FormatException("Unsupported activation-request schema.");
                }

                string requestId = ReadString(reader);
                int nonceLength = reader.ReadInt32();
                if (nonceLength != 32)
                {
                    throw new FormatException("Invalid activation-request nonce length.");
                }

                byte[] nonce = reader.ReadBytes(nonceLength);
                if (nonce.Length != nonceLength)
                {
                    throw new FormatException("The activation-request nonce is truncated.");
                }

                var request = new ActivationRequest(
                    requestId,
                    nonce,
                    ReadString(reader),
                    ReadString(reader),
                    ReadUtc(reader),
                    ReadUtc(reader),
                    ReadOptional(reader));
                if (stream.Position != stream.Length)
                {
                    throw new FormatException("The activation request contains trailing data.");
                }

                Validate(request);
                return request;
            }
        }
        catch (Exception exception) when (
            exception is EndOfStreamException ||
            exception is DecoderFallbackException ||
            exception is ArgumentException)
        {
            throw new FormatException("The activation-request payload is malformed.", exception);
        }
    }

    private static DateTimeOffset ReadUtc(BinaryReader reader)
    {
        long ticks = reader.ReadInt64();
        return new DateTimeOffset(new DateTime(ticks, DateTimeKind.Utc));
    }

    private static void ValidateRequired(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value) || StrictUtf8.GetByteCount(value) > MaximumStringBytes)
        {
            throw new ArgumentException(name + " is empty or too long.");
        }
    }

    private static void ValidateOptional(string? value, string name)
    {
        if (value is not null &&
            (string.IsNullOrWhiteSpace(value) || StrictUtf8.GetByteCount(value) > MaximumStringBytes))
        {
            throw new ArgumentException(name + " is empty or too long.");
        }
    }

    private static void WriteString(BinaryWriter writer, string value)
    {
        byte[] bytes = StrictUtf8.GetBytes(value);
        writer.Write(bytes.Length);
        writer.Write(bytes);
    }

    private static string ReadString(BinaryReader reader)
    {
        int length = reader.ReadInt32();
        if (length < 0 || length > MaximumStringBytes)
        {
            throw new FormatException("Invalid activation-request text length.");
        }

        byte[] bytes = reader.ReadBytes(length);
        if (bytes.Length != length)
        {
            throw new FormatException("Activation-request text is truncated.");
        }

        return StrictUtf8.GetString(bytes);
    }

    private static void WriteOptional(BinaryWriter writer, string? value)
    {
        writer.Write(value is not null);
        if (value is not null)
        {
            WriteString(writer, value);
        }
    }

    private static string? ReadOptional(BinaryReader reader)
    {
        byte marker = reader.ReadByte();
        if (marker == 0)
        {
            return null;
        }

        if (marker != 1)
        {
            throw new FormatException("Invalid optional activation-request value.");
        }

        return ReadString(reader);
    }
}
