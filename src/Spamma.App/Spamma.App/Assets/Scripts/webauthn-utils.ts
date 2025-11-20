/**
 * WebAuthn Utilities - Revealing Module Pattern
 */

export class WebAuthnUtils {
  isWebAuthnSupported(): boolean {
    return !!(window.PublicKeyCredential);
  }
  
  currentHostname(): string {
    return window.location.hostname;
  }

  async isPlatformAuthenticatorAvailable(): Promise<boolean> {
    if (!this.isWebAuthnSupported()) return false;
    return !!(await PublicKeyCredential.isUserVerifyingPlatformAuthenticatorAvailable());
  }

  async isConditionalUiSupported(): Promise<boolean> {
    if (!this.isWebAuthnSupported()) return false;
    return !!(await PublicKeyCredential.isConditionalMediationAvailable?.());
  }

  generateChallenge(): Uint8Array {
    return crypto.getRandomValues(new Uint8Array(32));
  }

  bufferToBase64(buffer: ArrayBuffer | Uint8Array): string {
    const bytes = new Uint8Array(buffer);
    let binary = '';
    for (let i = 0; i < bytes.byteLength; i++) {
      binary += String.fromCharCode(bytes[i]);
    }
    return btoa(binary);
  }

  base64ToBuffer(base64: string): ArrayBuffer {
    const binary = atob(base64);
    const bytes = new Uint8Array(binary.length);
    for (let i = 0; i < binary.length; i++) {
      bytes[i] = binary.charCodeAt(i);
    }
    return bytes.buffer;
  }

  async registerCredential(
    email: string,
    displayName: string,
    userId: string,
    rpName: string,
    rpId: string
  ): Promise<any> {
    if (!this.isWebAuthnSupported()) return null;

    const challenge = this.generateChallenge();
    const userIdBuffer = new Uint8Array(new TextEncoder().encode(userId));

    const publicKeyCredentialCreationOptions: PublicKeyCredentialCreationOptions = {
      challenge: challenge as BufferSource,
      rp: { name: rpName, id: rpId },
      user: { id: userIdBuffer as BufferSource, name: email, displayName: displayName },
      pubKeyCredParams: [
        { alg: -7, type: 'public-key' },
        { alg: -257, type: 'public-key' },
      ],
      attestation: 'direct',
      authenticatorSelection: { 
        authenticatorAttachment: 'platform', 
        residentKey: 'required',  // Required for usernameless authentication
        requireResidentKey: true  // Backwards compatibility
      },
      timeout: 60000,
    };

    try {
      const credential = (await navigator.credentials.create({
        publicKey: publicKeyCredentialCreationOptions,
      })) as PublicKeyCredential | null;

      if (!credential) return null;

      const response = credential.response as AuthenticatorAttestationResponse;
      return {
        id: credential.id,
        rawId: this.bufferToBase64(credential.rawId),
        response: {
          clientDataJSON: this.bufferToBase64(response.clientDataJSON),
          attestationObject: this.bufferToBase64(response.attestationObject),
        },
        type: credential.type,
      };
    } catch (error) {
      console.error('Registration error:', error);
      return null;
    }
  }

  async authenticateWithCredential(allowCredentials?: string[], rpId?: string): Promise<any> {
    if (!this.isWebAuthnSupported()) return null;

    const challenge = this.generateChallenge();

    const publicKeyCredentialRequestOptions: PublicKeyCredentialRequestOptions = {
      challenge: challenge as BufferSource,
      rpId: rpId,
      allowCredentials: allowCredentials
        ? allowCredentials.map((credId) => ({
            type: 'public-key',
            id: this.base64ToBuffer(credId) as BufferSource,
          }))
        : undefined,
      userVerification: 'preferred',
      timeout: 60000,
    };

    try {
      const assertion = (await navigator.credentials.get({
        publicKey: publicKeyCredentialRequestOptions,
      })) as PublicKeyCredential | null;

      if (!assertion) return null;

      const response = assertion.response as AuthenticatorAssertionResponse;
      return {
        id: assertion.id,
        rawId: this.bufferToBase64(assertion.rawId),
        response: {
          clientDataJSON: this.bufferToBase64(response.clientDataJSON),
          authenticatorData: this.bufferToBase64(response.authenticatorData),
          signature: this.bufferToBase64(response.signature),
          userHandle: response.userHandle ? this.bufferToBase64(response.userHandle) : '',
        },
        type: assertion.type,
      };
    } catch (error) {
      console.error('Authentication error:', error);
      return null;
    }
  }

  parseAuthenticatorData(authenticatorData: ArrayBuffer): any {
    const data = new Uint8Array(authenticatorData);
    const signCount = (data[33] << 24) | (data[34] << 16) | (data[35] << 8) | data[36];
    return { signCount: signCount, attestedCredentialData: data.buffer.slice(37) };
  }
}

// Export singleton instance for Blazor interop
const webAuthUtils = new WebAuthnUtils();

// Export individual functions that Blazor can call directly
export const isWebAuthnSupported = () => webAuthUtils.isWebAuthnSupported();
export const registerCredential = (email: string, displayName: string, userId: string, rpName: string, rpId: string) => 
  webAuthUtils.registerCredential(email, displayName, userId, rpName, rpId);
export const authenticateWithCredential = (allowCredentials: any, rpId: string) =>
  webAuthUtils.authenticateWithCredential(allowCredentials, rpId);
export const bufferToBase64 = (buffer: ArrayBuffer) => webAuthUtils.bufferToBase64(buffer);
export const base64ToBuffer = (base64: string) => webAuthUtils.base64ToBuffer(base64);
