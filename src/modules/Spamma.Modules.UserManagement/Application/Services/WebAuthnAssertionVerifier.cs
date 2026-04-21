using System.Formats.Cbor;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Spamma.Modules.UserManagement.Application.Services;

internal sealed class WebAuthnAssertionVerifier : IWebAuthnAssertionVerifier
{
    public bool Verify(
        byte[] storedAttestationObject,
        string storedAlgorithm,
        byte[] authenticatorData,
        byte[] clientDataJson,
        byte[] signature,
        string expectedChallengeBase64,
        string expectedOrigin,
        string expectedRpId)
    {
        try
        {
            if (!VerifyClientDataJson(clientDataJson, expectedChallengeBase64, expectedOrigin))
            {
                return false;
            }

            if (!VerifyAuthenticatorData(authenticatorData, expectedRpId))
            {
                return false;
            }

            var coseKeyBytes = ExtractCoseKeyFromAttestationObject(storedAttestationObject);

            var clientDataHash = SHA256.HashData(clientDataJson);

            var verificationData = new byte[authenticatorData.Length + clientDataHash.Length];
            Buffer.BlockCopy(authenticatorData, 0, verificationData, 0, authenticatorData.Length);
            Buffer.BlockCopy(clientDataHash, 0, verificationData, authenticatorData.Length, clientDataHash.Length);

            return storedAlgorithm switch
            {
                "-7" => VerifyEs256Signature(coseKeyBytes, verificationData, signature),
                "-257" => VerifyRs256Signature(coseKeyBytes, verificationData, signature),
                _ => false,
            };
        }
        catch
        {
            return false;
        }
    }

    private static bool VerifyClientDataJson(
        byte[] clientDataJson,
        string expectedChallengeBase64,
        string expectedOrigin)
    {
        var clientDataStr = Encoding.UTF8.GetString(clientDataJson);
        using var doc = JsonDocument.Parse(clientDataStr);
        var root = doc.RootElement;

        if (!root.TryGetProperty("type", out var typeProp) || typeProp.GetString() != "webauthn.get")
        {
            return false;
        }

        if (!root.TryGetProperty("challenge", out var challengeProp))
        {
            return false;
        }

        var base64Url = challengeProp.GetString();
        if (string.IsNullOrEmpty(base64Url))
        {
            return false;
        }

        // Convert base64url to standard base64 for byte-level comparison
        var base64 = base64Url.Replace('-', '+').Replace('_', '/');
        var rem = base64.Length % 4;
        if (rem != 0)
        {
            base64 += new string('=', 4 - rem);
        }

        var challengeBytes = Convert.FromBase64String(base64);
        var expectedBytes = Convert.FromBase64String(expectedChallengeBase64);

        if (!challengeBytes.SequenceEqual(expectedBytes))
        {
            return false;
        }

        if (!root.TryGetProperty("origin", out var originProp) || originProp.GetString() != expectedOrigin)
        {
            return false;
        }

        return true;
    }

    private static bool VerifyAuthenticatorData(byte[] authenticatorData, string expectedRpId)
    {
        if (authenticatorData.Length < 37)
        {
            return false;
        }

        // First 32 bytes must equal SHA-256(rpId)
        var expectedRpIdHash = SHA256.HashData(Encoding.UTF8.GetBytes(expectedRpId));
        if (!authenticatorData.AsSpan(0, 32).SequenceEqual(expectedRpIdHash))
        {
            return false;
        }

        // Bit 0 of flags (byte 32) = user present (UP) — must always be set per WebAuthn spec
        if ((authenticatorData[32] & 0x01) == 0)
        {
            return false;
        }

        return true;
    }

    private static byte[] ExtractCoseKeyFromAttestationObject(byte[] attestationObjectBytes)
    {
        byte[]? authData = null;

        var reader = new CborReader(attestationObjectBytes, CborConformanceMode.Lax);
        reader.ReadStartMap();

        while (reader.PeekState() != CborReaderState.EndMap && reader.PeekState() != CborReaderState.Finished)
        {
            if (reader.PeekState() != CborReaderState.TextString)
            {
                reader.SkipValue();
                reader.SkipValue();
                continue;
            }

            var key = reader.ReadTextString();
            if (key == "authData")
            {
                authData = reader.ReadByteString();
            }
            else
            {
                reader.SkipValue();
            }
        }

        reader.ReadEndMap();

        if (authData == null)
        {
            throw new InvalidOperationException("authData not found in attestation object.");
        }

        // authData layout when AT flag (bit 6) is set:
        //   [0..31]  rpIdHash (32 bytes)
        //   [32]     flags
        //   [33..36] signCount (4 bytes, big-endian)
        //   [37..52] aaguid (16 bytes)
        //   [53..54] credentialIdLength (2 bytes, big-endian)
        //   [55..]   credentialId then COSE public key (CBOR)
        if (authData.Length < 55)
        {
            throw new InvalidOperationException("authData too short to contain attested credential data.");
        }

        if ((authData[32] & 0x40) == 0)
        {
            throw new InvalidOperationException("Attested credential data (AT flag) not present in authData.");
        }

        const int CredentialIdLengthOffset = 37 + 16; // rpIdHash + flags + signCount + aaguid
        var credentialIdLength = (authData[CredentialIdLengthOffset] << 8) | authData[CredentialIdLengthOffset + 1];
        var coseKeyOffset = CredentialIdLengthOffset + 2 + credentialIdLength;

        if (authData.Length <= coseKeyOffset)
        {
            throw new InvalidOperationException("No COSE key data in authData.");
        }

        return authData[coseKeyOffset..];
    }

    private static bool VerifyEs256Signature(byte[] coseKeyBytes, byte[] data, byte[] signature)
    {
        byte[]? x = null, y = null;

        try
        {
            var reader = new CborReader(coseKeyBytes, CborConformanceMode.Lax);
            reader.ReadStartMap();

            while (reader.PeekState() != CborReaderState.EndMap && reader.PeekState() != CborReaderState.Finished)
            {
                if (reader.PeekState() is not (CborReaderState.UnsignedInteger or CborReaderState.NegativeInteger))
                {
                    reader.SkipValue();
                    reader.SkipValue();
                    continue;
                }

                switch (reader.ReadInt32())
                {
                    case -2: // x coordinate
                        x = reader.ReadByteString();
                        break;
                    case -3: // y coordinate
                        y = reader.ReadByteString();
                        break;
                    default:
                        reader.SkipValue();
                        break;
                }
            }
        }
        catch
        {
            return false;
        }

        if (x == null || y == null)
        {
            return false;
        }

        try
        {
            var ecParams = new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                Q = new ECPoint { X = PadOrTrimTo(x, 32), Y = PadOrTrimTo(y, 32) },
            };

            using var ecdsa = ECDsa.Create(ecParams);
            return ecdsa.VerifyData(data, signature, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
        }
        catch
        {
            return false;
        }
    }

    private static bool VerifyRs256Signature(byte[] coseKeyBytes, byte[] data, byte[] signature)
    {
        byte[]? n = null, e = null;

        try
        {
            var reader = new CborReader(coseKeyBytes, CborConformanceMode.Lax);
            reader.ReadStartMap();

            while (reader.PeekState() != CborReaderState.EndMap && reader.PeekState() != CborReaderState.Finished)
            {
                if (reader.PeekState() is not (CborReaderState.UnsignedInteger or CborReaderState.NegativeInteger))
                {
                    reader.SkipValue();
                    reader.SkipValue();
                    continue;
                }

                switch (reader.ReadInt32())
                {
                    case -1: // modulus (n)
                        n = reader.ReadByteString();
                        break;
                    case -2: // exponent (e)
                        e = reader.ReadByteString();
                        break;
                    default:
                        reader.SkipValue();
                        break;
                }
            }
        }
        catch
        {
            return false;
        }

        if (n == null || e == null)
        {
            return false;
        }

        try
        {
            var rsaParams = new RSAParameters { Modulus = n, Exponent = e };
            using var rsa = RSA.Create(rsaParams);
            return rsa.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
        catch
        {
            return false;
        }
    }

    private static byte[] PadOrTrimTo(byte[] bytes, int targetLength)
    {
        if (bytes.Length == targetLength)
        {
            return bytes;
        }

        if (bytes.Length > targetLength)
        {
            // Strip leading zeros — COSE keys may omit them
            var trimmed = bytes.SkipWhile(b => b == 0).ToArray();
            if (trimmed.Length > targetLength)
            {
                throw new InvalidOperationException(
                    $"Coordinate length {trimmed.Length} exceeds target {targetLength}.");
            }

            bytes = trimmed;
        }

        var padded = new byte[targetLength];
        Buffer.BlockCopy(bytes, 0, padded, targetLength - bytes.Length, bytes.Length);
        return padded;
    }
}