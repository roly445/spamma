// Main application TypeScript entry point
import {WebAuthnUtils} from './webauthn-utils';

console.log('App TypeScript loaded');

// Example function to export to global scope for Blazor interop
function initializeApp(): void {
    document.querySelectorAll('form[data-disable-form]')
        .forEach(form => 
        form.addEventListener('submit', event => {
            form.querySelectorAll<HTMLButtonElement>('button').forEach(button => button.disabled = true);
        }));
}

// Export WebAuthn utilities to global scope for Blazor interop
(window as any).WebAuthnUtils = new WebAuthnUtils();

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    initializeApp();
});

// Register a document-level click handler that invokes a .NET method when a click occurs outside the given selector.
// Returns a handler id which can be used to remove the listener later.
(window as any).registerOutsideClickHandler = function (selector: string, dotNetHelper: any, methodName: string) {
    (window as any).__outsideClickHandlers = (window as any).__outsideClickHandlers || {};
    const id = Math.random().toString(36).slice(2);
    const handler = (e: Event) => {
        const panel = document.querySelector(selector) as HTMLElement | null;
        if (!panel) return;
        // If the clicked element is not inside the panel, invoke .NET method
        if (!panel.contains(e.target as Node)) {
            try {
                dotNetHelper.invokeMethodAsync(methodName);
            }
            catch (err) {
                console.error('Error invoking .NET method from outside click handler', err);
            }
        }
    };
    document.addEventListener('click', handler, true);
    (window as any).__outsideClickHandlers[id] = handler;
    return id;
};

(window as any).removeOutsideClickHandler = function (id: string) {
    if (!(window as any).__outsideClickHandlers) return;
    const handler = (window as any).__outsideClickHandlers[id];
    if (!handler) return;
    document.removeEventListener('click', handler, true);
    delete (window as any).__outsideClickHandlers[id];
};