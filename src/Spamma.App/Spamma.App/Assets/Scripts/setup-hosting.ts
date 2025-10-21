export class SetupHosting {
    constructor() {
        if (document.readyState !== 'loading') {
            this.initialize();
        } else {
            document.addEventListener('DOMContentLoaded', e => this.initialize());
        }
    }

    private initialize() {

            // Handle reconfigure button
            const reconfigureBtn = document.getElementById('reconfigure-application-btn') as HTMLButtonElement;
            const existingHostingSctions = document.getElementById('existing-hosting-actions') as HTMLElement;
            const configForm = document.getElementById('application-configuration-form') as HTMLFormElement;

            if (reconfigureBtn && configForm) {
                reconfigureBtn.addEventListener('click', function() {
                    // Hide existing config warning and show form
                    existingHostingSctions.style.display = 'none';
                    configForm.style.display = 'block';
                });
            }

            // Update MX record example in real-time
            const hostnameInput = document.getElementById('mail-server-hostname') as HTMLInputElement;
            const priorityInput = document.getElementById('mx-priority') as HTMLInputElement;
            const mxExample = document.getElementById('mx-record-example') as HTMLElement;

            function updateMxExample() {
                if (hostnameInput && priorityInput && mxExample) {
                    const hostname = hostnameInput.value || 'mail.yourdomain.com';
                    const priority = priorityInput.value || '10';
                    mxExample.textContent = `yourdomain.com. MX ${priority} ${hostname}`;
                }
            }

            if (hostnameInput && priorityInput) {
                hostnameInput.addEventListener('input', updateMxExample);
                priorityInput.addEventListener('input', updateMxExample);
            }

    }
}

const setupHosting = new SetupHosting();