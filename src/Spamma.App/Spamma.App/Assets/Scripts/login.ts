/**
 * Passkey authentication flow for the login page.
 * Handles WebAuthn credential assertion and server authentication.
 */
import {ArrayHelper, XhrHelper} from "./helpers";

interface IAssertionOptionsResult {
    challenge: string;
    timeout: number;
    rpId: string;
    allowCredentials: {
        id: string;
        transports?: AuthenticatorTransport[];
        type: PublicKeyCredentialType;
    }[];
    userVerification?: UserVerificationRequirement;
    extensions: AuthenticationExtensionsClientInputs;
    status: string;
    errorMessage: string;
}

interface IAssertionVerificationResult {
    url: string;
    assertionVerificationResult: {
        credentialId: string;
        counter: number
        status: string;
        errorMessage: string;
    };
}

export class Login {
    private assertionOptionsUri = '/api/auth/assertion-options';
    private makeAssertionUri = '/api/auth/make-assertion';

    constructor() {
        if (document.readyState !== 'loading') {
            this.initialize();
        } else {
            document.addEventListener('DOMContentLoaded', () => this.initialize());
        }
    }

    private initialize(): void {
        
        const constThis = this;

        // Check if WebAuthnUtils is available, with fallback to native PublicKeyCredential API
        const isWebAuthnSupported = () => {
            if ((window as any).WebAuthnUtils && typeof (window as any).WebAuthnUtils.isWebAuthnSupported === 'function') {
                return (window as any).WebAuthnUtils.isWebAuthnSupported();
            }
            return !!(window as any).PublicKeyCredential;
        };

        if (isWebAuthnSupported()) {
            const passkeyContainer = document.querySelector<HTMLDivElement>('#passkeyContainer');
            if (passkeyContainer) {
                passkeyContainer.classList.remove('hidden');
                const passkeyLoginBtn = document.getElementById('passkeyLoginBtn') as HTMLButtonElement;
                passkeyLoginBtn.addEventListener('click', (e: Event) => constThis.handlePasskeyClick(e))
            }
            
        }
        
    }
    
    private async handlePasskeyClick(e: Event)
    {
        e.preventDefault();

        const passkeyLoginBtn = document.getElementById('passkeyLoginBtn') as HTMLButtonElement;
        const emailInput = document.getElementById('email') as HTMLInputElement;
        const submitBtn = document.querySelector('button[type="submit"]') as HTMLButtonElement;

        // Disable all controls
        this.setFormDisabled(true);

        const makeAssertionOptionsResult = await XhrHelper.PostInternalOfT<IAssertionOptionsResult>(this.assertionOptionsUri);
        if (makeAssertionOptionsResult.isFailure) {
            this.showPasskeyError('Authentication failed. Please try again.');
            this.setFormDisabled(false);
            return;
        }

        const makeAssertionOptions = makeAssertionOptionsResult.value;
        if (makeAssertionOptions.status !== 'ok') {
            this.showPasskeyError('Authentication failed. Please try again.');
            this.setFormDisabled(false);
            return;
        }

        const challenge = makeAssertionOptions.challenge.replace(/-/g, '+').replace(/_/g, '/');

        const publicKeyCredentialRequestOptions: PublicKeyCredentialRequestOptions = {
            userVerification: makeAssertionOptions.userVerification,
            rpId: makeAssertionOptions.rpId,
            extensions: makeAssertionOptions.extensions,
            timeout: makeAssertionOptions.timeout,
            challenge: Uint8Array.from(atob(challenge), c => c.charCodeAt(0)),
            allowCredentials: []
        };

        makeAssertionOptions.allowCredentials.forEach((listItem) => {
            const fixedId = (listItem.id as unknown as string).replace(/\_/g, '/').replace(/\-/g, '+');
            const publicKeyCredentialDescriptor: PublicKeyCredentialDescriptor = {
                transports: listItem.transports,
                type: listItem.type,
                id: Uint8Array.from(atob(fixedId), c => c.charCodeAt(0))
            };

            publicKeyCredentialRequestOptions.allowCredentials.push(publicKeyCredentialDescriptor);
        });

        let credential;
        try {
            credential = await navigator.credentials.get({ publicKey: publicKeyCredentialRequestOptions });
        } catch (err) {
            this.showPasskeyError('Authentication failed. Please try again.');
            this.setFormDisabled(false);
            return;
        }

        await this.verifyAssertionWithServer(credential);
    }

    private setFormDisabled(disabled: boolean): void {
        const emailInput = document.getElementById('email') as HTMLInputElement;
        const submitBtn = document.querySelector('button[type="submit"]') as HTMLButtonElement;
        const passkeyLoginBtn = document.getElementById('passkeyLoginBtn') as HTMLButtonElement;

        if (emailInput) {
            emailInput.disabled = disabled;
        }
        if (submitBtn) {
            submitBtn.disabled = disabled;
        }
        if (passkeyLoginBtn) {
            passkeyLoginBtn.disabled = disabled;
        }
    }

    private async verifyAssertionWithServer(assertedCredential) {

        // Convert buffers to standard base64 (matching registration format)
        const bufferToBase64 = (buffer: ArrayBuffer | Uint8Array): string => {
            const bytes = new Uint8Array(buffer);
            let binary = '';
            for (let i = 0; i < bytes.byteLength; i++) {
                binary += String.fromCharCode(bytes[i]);
            }
            return btoa(binary);
        };

        const authData = new Uint8Array(assertedCredential.response.authenticatorData);
        const clientDataJSON = new Uint8Array(assertedCredential.response.clientDataJSON);
        const rawId = new Uint8Array(assertedCredential.rawId);
        const sig = new Uint8Array(assertedCredential.response.signature);
        const data = {
            assertion: {
                id: bufferToBase64(rawId),
                rawId: bufferToBase64(rawId),
                type: assertedCredential.type,
                extensions: assertedCredential.getClientExtensionResults(),
                response: {
                    authenticatorData: bufferToBase64(authData),
                    clientDataJson: bufferToBase64(clientDataJSON),
                    signature: bufferToBase64(sig)
                }
            },
        };

        const result = await XhrHelper.PostJsonInternalOfT<IAssertionVerificationResult>(this.makeAssertionUri, data);

        if (result.isFailure) {
            this.showPasskeyError('Authentication failed. Please try again.');
            this.setFormDisabled(false);
            return;
        }

        if (result.value.assertionVerificationResult.status !== 'ok') {
            this.showPasskeyError('Authentication failed. Please try again.');
            this.setFormDisabled(false);
            return;
        }

        window.location.href = result.value.url;
    }
    

    // async initiatePasskeyLogin(): Promise<void> {
    //    
    //    
    //
    //     try {
    //         // Check WebAuthn support
    //        
    //
    //         // Attempt passkey authentication
    //        
    //
    //        
    // }

    private showPasskeyError(message: string): void {
        // Create/update error message element
        let errorDiv = document.getElementById('passkeyErrorContainer') as HTMLDivElement | null;
        if (!errorDiv) {
            errorDiv = document.createElement('div');
            errorDiv.id = 'passkeyErrorContainer';
            errorDiv.className = 'rounded-md bg-red-50 p-4 mb-4';
            // Insert at the beginning of the form
            const editForm = document.querySelector('form[data-disable-form]');
            if (editForm && editForm.firstChild) {
                editForm.insertBefore(errorDiv, editForm.firstChild);
            } else if (editForm) {
                editForm.appendChild(errorDiv);
            }
        }

        errorDiv.innerHTML = `
        <div class="flex">
            <div class="flex-shrink-0">
                <svg class="h-5 w-5 text-red-400" fill="currentColor" viewBox="0 0 20 20">
                    <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clip-rule="evenodd"></path>
                </svg>
            </div>
            <div class="ml-3">
                <p class="text-sm text-red-800">${message}</p>
            </div>
        </div>
    `;
    }
}



// Initialize and export to global scope
const login = new Login();
