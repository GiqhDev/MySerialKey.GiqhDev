using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MySerialKey.GiqhDev.Licensing.Revocation.Internal;

internal static class RevocationListSerializer
{
    public const int CurrentSchemaVersion = 1;
    private const int MaximumStringBytes = 128;
    private const int MaximumEntries = 4000;
    private static readonly byte[] Magic = { 0x4D, 0x53, 0x52, 0x50 };
    private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

    /// <summary>Validates list semantics and bounded payload limits before signing.</summary>
    public static void ValidateForSigning(RevocationListClaims claims)
    {
        if (!IsSupportedSchema(claims.SchemaVersion))
        {
            throw new ArgumentException("The revocation-list schema is not supported.", nameof(claims));
        }

        ValidateRequired(claims.ProductCode, "Product code");
        if (claims.SequenceNumber <= 0)
        {
            throw new ArgumentException("Sequence number must be greater than zero.", nameof(claims));
        }

        if (claims.ExpiresAtUtc.ToUniversalTime() <= claims.IssuedAtUtc.ToUniversalTime())
        {
            throw new ArgumentException("Expiration must be later than issue time.", nameof(claims));
        }

        if (claims.RevokedLicenseIds is null)
        {
            throw new ArgumentException("Revoked license identifiers cannot be null.", nameof(claims));
        }

        if (claims.RevokedLicenseIds.Count > MaximumEntries)
        {
            throw new ArgumentException("Too many revoked license identifiers were supplied.", nameof(claims));
        }

        foreach (string licenseId in claims.RevokedLicenseIds)
        {
            ValidateRequired(licenseId, "License identifier");
        }
    }

    /// <summary>Serializes a sorted, deduplicated list into deterministic bytes.</summary>
    public static byte[] Serialize(RevocationListClaims claims)
    {
        string[] licenseIds = claims.RevokedLicenseIds
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream, StrictUtf8, true))
        {
            writer.Write(Magic);
            writer.Write(claims.SchemaVersion);
            WriteString(writer, claims.ProductCode);
            writer.Write(claims.SequenceNumber);
            writer.Write(claims.IssuedAtUtc.UtcDateTime.Ticks);
            writer.Write(claims.ExpiresAtUtc.UtcDateTime.Ticks);
            writer.Write(licenseIds.Length);
            foreach (string licenseId in licenseIds)
            {
                WriteString(writer, licenseId);
            }

            writer.Flush();
            return stream.ToArray();
        }
    }

    /// <summary>Decodes an authenticated binary revocation-list payload.</summary>
    public static RevocationListClaims Deserialize(byte[] payload)
    {
        using (var stream = new MemoryStream(payload, false))
        using (var reader = new BinaryReader(stream, StrictUtf8, true))
        {
            byte[] magic = reader.ReadBytes(Magic.Length);
            if (magic.Length != Magic.Length || !magic.SequenceEqual(Magic))
            {
                throw new FormatException("Invalid revocation-list payload marker.");
            }

            int schemaVersion = reader.ReadInt32();
            string productCode = ReadString(reader);
            long sequenceNumber = reader.ReadInt64();
            DateTimeOffset issuedAtUtc = ReadUtcTimestamp(reader);
            DateTimeOffset expiresAtUtc = ReadUtcTimestamp(reader);
            int count = reader.ReadInt32();
            if (count < 0 || count > MaximumEntries)
            {
                throw new FormatException("Invalid revoked-license count.");
            }

            var licenseIds = new List<string>(count);
            for (int index = 0; index < count; index++)
            {
                licenseIds.Add(ReadString(reader));
            }

            if (stream.Position != stream.Length)
            {
                throw new FormatException("The revocation-list payload contains trailing data.");
            }

            return new RevocationListClaims(productCode, sequenceNumber, issuedAtUtc, expiresAtUtc)
            {
                SchemaVersion = schemaVersion,
                RevokedLicenseIds = licenseIds
            };
        }
    }

    /// <summary>Reports whether this library can read a payload schema.</summary>
    public static bool IsSupportedSchema(int schemaVersion)
    {
        return schemaVersion == CurrentSchemaVersion;
    }

    /// <summary>Validates a required bounded UTF-8 string.</summary>
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

    /// <summary>Writes a bounded string with an explicit UTF-8 byte-length prefix.</summary>
    private static void WriteString(BinaryWriter writer, string value)
    {
        byte[] bytes = StrictUtf8.GetBytes(value);
        if (bytes.Length > MaximumStringBytes)
        {
            throw new ArgumentException("Revocation-list text value is too long.");
        }

        writer.Write(bytes.Length);
        writer.Write(bytes);
    }

    /// <summary>Reads a bounded string using strict UTF-8 decoding.</summary>
    private static string ReadString(BinaryReader reader)
    {
        int length = reader.ReadInt32();
        if (length < 0 || length > MaximumStringBytes)
        {
            throw new FormatException("Invalid revocation-list text length.");
        }

        byte[] bytes = reader.ReadBytes(length);
        if (bytes.Length != length)
        {
            throw new FormatException("Unexpected end of revocation-list payload.");
        }

        return StrictUtf8.GetString(bytes);
    }

    /// <summary>Reads a UTC timestamp while rejecting invalid tick values.</summary>
    private static DateTimeOffset ReadUtcTimestamp(BinaryReader reader)
    {
        long ticks = reader.ReadInt64();
        if (ticks < DateTime.MinValue.Ticks || ticks > DateTime.MaxValue.Ticks)
        {
            throw new FormatException("Invalid revocation-list timestamp.");
        }

        return new DateTimeOffset(ticks, TimeSpan.Zero);
    }
}
