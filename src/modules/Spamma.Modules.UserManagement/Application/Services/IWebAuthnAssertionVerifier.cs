namespace Spamma.Modules.UserManagement.Application.Services;

internal interface IWebAuthnAssertionVerifier
{
    bool Verify(
        byte[] storedAttestationObject,
        string storedAlgorithm,
        byte[] authenticatorData,
        byte[] clientDataJson,
        byte[] signature,
        string expectedChallengeBase64,
        string expectedOrigin,
        string expectedRpId);
}
