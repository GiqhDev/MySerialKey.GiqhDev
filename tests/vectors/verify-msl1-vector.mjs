import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { verify } from "node:crypto";

const directory = dirname(fileURLToPath(import.meta.url));
const [publicKeyToken, licenseToken] = readFileSync(
    join(directory, "msl1-es256-vector.txt"),
    "utf8")
    .trim()
    .split(/\r?\n/);

const keyParts = publicKeyToken.split(".");
const licenseParts = licenseToken.split(".");
if (keyParts.length !== 5 || licenseParts.length !== 5) {
    throw new Error("The MSL1 interoperability vector is malformed.");
}

const verified = verify(
    "sha256",
    Buffer.from(licenseParts.slice(0, 4).join("."), "ascii"),
    {
        key: {
            kty: "EC",
            crv: "P-256",
            x: keyParts[3],
            y: keyParts[4]
        },
        format: "jwk",
        dsaEncoding: "ieee-p1363"
    },
    Buffer.from(licenseParts[4], "base64url"));

if (!verified) {
    throw new Error("Node/OpenSSL rejected the MSL1 ES256 signature.");
}

console.log("MSL1 vector verified independently by Node/OpenSSL.");
