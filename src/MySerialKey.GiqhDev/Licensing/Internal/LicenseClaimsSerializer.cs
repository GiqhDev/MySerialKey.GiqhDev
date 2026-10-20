using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MySerialKey.GiqhDev.Licensing.Internal;

internal static class LicenseClaimsSerializer
{
    public const int CurrentSchemaVersion = 1;
    private const int MaximumStringBytes = 16384;
    private const int MaximumFeatures = 256;
    private static readonly byte[] Magic = { 0x4D, 0x53, 0x4C, 0x50 };
    private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

    public static void ValidateForSigning(LicenseClaims claims)
    {
        if (claims.SchemaVersion != CurrentSchemaVersion)
        {
            throw new ArgumentException("Only license schema version 1 can be signed.", nameof(claims));
        }

        ValidateRequired(claims.LicenseId, "License identifier");
        ValidateRequired(claims.ProductCode, "Product code");

        if (claims.MaxTerminals <= 0)
        {
            throw new ArgumentException("Maximum terminals must be greater than zero.", nameof(claims));
        }

        if (claims.ExpiresAtUtc.HasValue &&
            claims.ExpiresAtUtc.Value.ToUniversalTime() <= claims.NotBeforeUtc.ToUniversalTime())
        {
            throw new ArgumentException("Expiration must be later than the not-before time.", nameof(claims));
        }

        if (claims.Features is null)
        {
            throw new ArgumentException("Features cannot be null.", nameof(claims));
        }

        if (claims.Features.Count > MaximumFeatures)
        {
            throw new ArgumentException("Too many license features were supplied.", nameof(claims));
        }

        foreach (string feature in claims.Features)
        {
            ValidateRequired(feature, "Feature");
        }
    }

    public static byte[] Serialize(LicenseClaims claims)
    {
        string[] features = claims.Features
            .Distinct(StringComparer.Ordinal)
            .OrderBy(feature => feature, StringComparer.Ordinal)
            .ToArray();

        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream, StrictUtf8, true))
        {
            writer.Write(Magic);
            writer.Write(claims.SchemaVersion);
            WriteString(writer, claims.LicenseId);
            WriteString(writer, claims.ProductCode);
            WriteNullableString(writer, claims.CustomerId);
            WriteNullableString(writer, claims.Edition);
            writer.Write(claims.IssuedAtUtc.UtcDateTime.Ticks);
            writer.Write(claims.NotBeforeUtc.UtcDateTime.Ticks);
            writer.Write(claims.ExpiresAtUtc.HasValue);
            if (claims.ExpiresAtUtc.HasValue)
            {
                writer.Write(claims.ExpiresAtUtc.Value.UtcDateTime.Ticks);
            }

            WriteNullableString(writer, claims.InstallationId);
            writer.Write(claims.MaxTerminals);
            writer.Write(features.Length);
            foreach (string feature in features)
            {
                WriteString(writer, feature);
            }

            writer.Flush();
            return stream.ToArray();
        }
    }

    public static LicenseClaims Deserialize(byte[] payload)
    {
        using (var stream = new MemoryStream(payload, false))
        using (var reader = new BinaryReader(stream, StrictUtf8, true))
        {
            byte[] magic = reader.ReadBytes(Magic.Length);
            if (magic.Length != Magic.Length || !magic.SequenceEqual(Magic))
            {
                throw new FormatException("Invalid license payload marker.");
            }

            int schemaVersion = reader.ReadInt32();
            string licenseId = ReadString(reader);
            string productCode = ReadString(reader);
            string? customerId = ReadNullableString(reader);
            string? edition = ReadNullableString(reader);
            DateTimeOffset issuedAtUtc = ReadUtcTimestamp(reader);
            DateTimeOffset notBeforeUtc = ReadUtcTimestamp(reader);
            DateTimeOffset? expiresAtUtc = reader.ReadBoolean() ? ReadUtcTimestamp(reader) : null;
            string? installationId = ReadNullableString(reader);
            int maxTerminals = reader.ReadInt32();
            int featureCount = reader.ReadInt32();

            if (featureCount < 0 || featureCount > MaximumFeatures)
            {
                throw new FormatException("Invalid license feature count.");
            }

            var features = new List<string>(featureCount);
            for (int i = 0; i < featureCount; i++)
            {
                features.Add(ReadString(reader));
            }

            if (stream.Position != stream.Length)
            {
                throw new FormatException("The license payload contains trailing data.");
            }

            return new LicenseClaims(licenseId, productCode, issuedAtUtc)
            {
                SchemaVersion = schemaVersion,
                CustomerId = customerId,
                Edition = edition,
                NotBeforeUtc = notBeforeUtc,
                ExpiresAtUtc = expiresAtUtc,
                InstallationId = installationId,
                MaxTerminals = maxTerminals,
                Features = features
            };
        }
    }

    private static void ValidateRequired(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(name + " cannot be empty.");
        }

        if (StrictUtf8.GetByteCount(value) > MaximumStringBytes)
        {
            throw new ArgumentException(name + " is too long.");
        }
    }

    private static void WriteNullableString(BinaryWriter writer, string? value)
    {
        writer.Write(value is not null);
        if (value is not null)
        {
            WriteString(writer, value);
        }
    }

    private static string? ReadNullableString(BinaryReader reader)
    {
        return reader.ReadBoolean() ? ReadString(reader) : null;
    }

    private static void WriteString(BinaryWriter writer, string value)
    {
        byte[] bytes = StrictUtf8.GetBytes(value);
        if (bytes.Length > MaximumStringBytes)
        {
            throw new ArgumentException("License text value is too long.");
        }

        writer.Write(bytes.Length);
        writer.Write(bytes);
    }

    private static string ReadString(BinaryReader reader)
    {
        int length = reader.ReadInt32();
        if (length < 0 || length > MaximumStringBytes)
        {
            throw new FormatException("Invalid license text length.");
        }

        byte[] bytes = reader.ReadBytes(length);
        if (bytes.Length != length)
        {
            throw new FormatException("Unexpected end of license payload.");
        }

        return StrictUtf8.GetString(bytes);
    }

    private static DateTimeOffset ReadUtcTimestamp(BinaryReader reader)
    {
        long ticks = reader.ReadInt64();
        if (ticks < DateTime.MinValue.Ticks || ticks > DateTime.MaxValue.Ticks)
        {
            throw new FormatException("Invalid license timestamp.");
        }

        return new DateTimeOffset(ticks, TimeSpan.Zero);
    }
}
