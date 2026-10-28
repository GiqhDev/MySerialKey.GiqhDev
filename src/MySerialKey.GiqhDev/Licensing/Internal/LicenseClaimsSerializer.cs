using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MySerialKey.GiqhDev.Licensing.Internal;

internal static class LicenseClaimsSerializer
{
    public const int CurrentSchemaVersion = 2;
    public const int MinimumSchemaVersion = 1;
    private static readonly byte[] Magic = { 0x4D, 0x53, 0x4C, 0x50 };
    private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

    /// <summary>Validates claim semantics and size limits before any signature is produced.</summary>
    public static void ValidateForSigning(LicenseClaims claims)
    {
        if (!IsSupportedSchema(claims.SchemaVersion))
        {
            throw new ArgumentException("The license schema version is not supported.", nameof(claims));
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

        if (claims.SchemaVersion == 1 &&
            (claims.NextCheckInUtc.HasValue ||
             claims.EntitlementStatus != LicenseEntitlementStatus.Active))
        {
            throw new ArgumentException(
                "Schema version 1 cannot contain refresh or entitlement-state claims.",
                nameof(claims));
        }

        if (!Enum.IsDefined(typeof(LicenseEntitlementStatus), claims.EntitlementStatus))
        {
            throw new ArgumentException("The entitlement status is invalid.", nameof(claims));
        }

        if (claims.NextCheckInUtc.HasValue &&
            claims.NextCheckInUtc.Value.ToUniversalTime() <= claims.NotBeforeUtc.ToUniversalTime())
        {
            throw new ArgumentException("Next check-in must be later than the not-before time.", nameof(claims));
        }

        if (claims.NextCheckInUtc.HasValue &&
            claims.ExpiresAtUtc.HasValue &&
            claims.NextCheckInUtc.Value.ToUniversalTime() >=
                claims.ExpiresAtUtc.Value.ToUniversalTime())
        {
            throw new ArgumentException(
                "Next check-in must be earlier than license expiration.",
                nameof(claims));
        }

        if (claims.Features is null)
        {
            throw new ArgumentException("Features cannot be null.", nameof(claims));
        }

        if (claims.Features.Count > LicenseFormatLimits.MaximumFeatures)
        {
            throw new ArgumentException("Too many license features were supplied.", nameof(claims));
        }

        foreach (string feature in claims.Features)
        {
            ValidateRequired(feature, "Feature");
        }
    }

    /// <summary>Serializes claims deterministically so equivalent values produce identical signed bytes.</summary>
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

            if (claims.SchemaVersion >= 2)
            {
                writer.Write(claims.NextCheckInUtc.HasValue);
                if (claims.NextCheckInUtc.HasValue)
                {
                    writer.Write(claims.NextCheckInUtc.Value.UtcDateTime.Ticks);
                }

                writer.Write((int)claims.EntitlementStatus);
            }

            writer.Flush();
            if (stream.Length > LicenseFormatLimits.MaximumSignedLicensePayloadBytes)
            {
                throw new ArgumentException("The encoded license payload is too large.", nameof(claims));
            }

            return stream.ToArray();
        }
    }

    /// <summary>Decodes a supported license payload after its enclosing signature has been verified.</summary>
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

            if (featureCount < 0 || featureCount > LicenseFormatLimits.MaximumFeatures)
            {
                throw new FormatException("Invalid license feature count.");
            }

            var features = new List<string>(featureCount);
            for (int i = 0; i < featureCount; i++)
            {
                features.Add(ReadString(reader));
            }

            DateTimeOffset? nextCheckInUtc = null;
            LicenseEntitlementStatus entitlementStatus = LicenseEntitlementStatus.Active;
            if (schemaVersion >= 2)
            {
                nextCheckInUtc = reader.ReadBoolean() ? ReadUtcTimestamp(reader) : null;
                entitlementStatus = (LicenseEntitlementStatus)reader.ReadInt32();
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
                NextCheckInUtc = nextCheckInUtc,
                EntitlementStatus = entitlementStatus,
                InstallationId = installationId,
                MaxTerminals = maxTerminals,
                Features = features
            };
        }
    }

    /// <summary>Reports whether a payload schema can be read by this library version.</summary>
    public static bool IsSupportedSchema(int schemaVersion)
    {
        return schemaVersion >= MinimumSchemaVersion &&
            schemaVersion <= CurrentSchemaVersion;
    }

    /// <summary>Validates a required claim string and its encoded size.</summary>
    private static void ValidateRequired(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(name + " cannot be empty.");
        }

        if (StrictUtf8.GetByteCount(value) > LicenseFormatLimits.MaximumClaimTextUtf8Bytes)
        {
            throw new ArgumentException(name + " is too long.");
        }
    }

    /// <summary>Writes a nullable claim string using an explicit presence marker.</summary>
    private static void WriteNullableString(BinaryWriter writer, string? value)
    {
        writer.Write(value is not null);
        if (value is not null)
        {
            WriteString(writer, value);
        }
    }

    /// <summary>Reads a nullable claim string encoded with a presence marker.</summary>
    private static string? ReadNullableString(BinaryReader reader)
    {
        return reader.ReadBoolean() ? ReadString(reader) : null;
    }

    /// <summary>Writes a claim string with a deterministic UTF-8 byte-length prefix.</summary>
    private static void WriteString(BinaryWriter writer, string value)
    {
        byte[] bytes = StrictUtf8.GetBytes(value);
        if (bytes.Length > LicenseFormatLimits.MaximumClaimTextUtf8Bytes)
        {
            throw new ArgumentException("License text value is too long.");
        }

        writer.Write(bytes.Length);
        writer.Write(bytes);
    }

    /// <summary>Reads a bounded claim string using strict UTF-8 decoding.</summary>
    private static string ReadString(BinaryReader reader)
    {
        int length = reader.ReadInt32();
        if (length < 0 || length > LicenseFormatLimits.MaximumClaimTextUtf8Bytes)
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

    /// <summary>Reads a UTC timestamp while rejecting invalid tick values.</summary>
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
