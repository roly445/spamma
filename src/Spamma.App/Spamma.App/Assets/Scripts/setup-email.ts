interface SmtpPreset {
    host: string;
    port: number;
    ssl: boolean;
}

class SetupEmailConfigurator {
    private readonly presets: Record<string, SmtpPreset> = {
        development: {
            host: 'localhost',
            port: 1025,
            ssl: false
        },
        gmail: {
            host: 'smtp.gmail.com',
            port: 587,
            ssl: true
        },
        sendgrid: {
            host: 'smtp.sendgrid.net',
            port: 587,
            ssl: true
        },
        mailgun: {
            host: 'smtp.mailgun.org',
            port: 587,
            ssl: true
        }
    };

    constructor() {
        if (document.readyState !== 'loading') {
            this.initialize();
        } else {
            document.addEventListener('DOMContentLoaded', e => this.initialize());
        }
    }

    private initialize(): void {
        this.setupPresetHandlers();
        this.setupReconfigureHandler();
    }

    private setupPresetHandlers(): void {
        // Add event listeners for all preset buttons
        Object.keys(this.presets).forEach(provider => {
            const button = document.querySelector(`[onclick="setSmtpPreset('${provider}')"]`) as HTMLButtonElement;
            if (button) {
                // Remove the inline onclick and add proper event listener
                button.removeAttribute('onclick');
                button.addEventListener('click', () => {
                    this.setSmtpPreset(provider);
                });
            }
        });
    }

    private setupReconfigureHandler(): void {
        const reconfigureBtn = document.getElementById('reconfigure-email-btn');
        reconfigureBtn?.addEventListener('click', () => {
            const existingSection = reconfigureBtn.closest('div')?.parentElement;
            const formSection = document.getElementById('email-configuration-form');
            
            if (existingSection) existingSection.style.display = 'none';
            if (formSection) formSection.style.display = 'block';
        });
    }

    public setSmtpPreset(provider: string): void {
        const preset = this.presets[provider];
        if (!preset) {
            console.warn(`Unknown SMTP provider: ${provider}`);
            return;
        }

        const hostInput = document.getElementById('smtp-host') as HTMLInputElement;
        const portInput = document.getElementById('smtp-port') as HTMLInputElement;
        const sslInput = document.getElementById('use-ssl') as HTMLInputElement;
        
        if (!hostInput || !portInput || !sslInput) {
            console.warn('SMTP form elements not found');
            return;
        }
        
        // Set the values
        hostInput.value = preset.host;
        portInput.value = preset.port.toString();
        sslInput.checked = preset.ssl;
        
        // Trigger change events to update Blazor bindings
        this.triggerBlazorUpdate(hostInput);
        this.triggerBlazorUpdate(portInput);
        this.triggerBlazorUpdate(sslInput);
        
        // Visual feedback
        this.showPresetApplied(provider);
    }

    private triggerBlazorUpdate(element: HTMLInputElement): void {
        // Trigger both 'input' and 'change' events to ensure Blazor picks up the change
        element.dispatchEvent(new Event('input', { bubbles: true }));
        element.dispatchEvent(new Event('change', { bubbles: true }));
        
        // For some Blazor scenarios, we might also need to trigger 'blur'
        element.dispatchEvent(new Event('blur', { bubbles: true }));
    }

    private showPresetApplied(provider: string): void {
        // Create a temporary notification to show the preset was applied
        const notification = document.createElement('div');
        notification.className = 'fixed top-4 right-4 bg-green-100 border border-green-400 text-green-700 px-4 py-3 rounded z-50 transition-opacity duration-300';
        notification.innerHTML = `
            <div class="flex items-center">
                <svg class="w-4 h-4 mr-2" fill="currentColor" viewBox="0 0 20 20">
                    <path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd"></path>
                </svg>
                <span class="capitalize">${provider}</span> preset applied
            </div>
        `;
        
        document.body.appendChild(notification);
        
        // Remove the notification after 3 seconds
        setTimeout(() => {
            notification.style.opacity = '0';
            setTimeout(() => {
                if (notification.parentNode) {
                    notification.parentNode.removeChild(notification);
                }
            }, 300);
        }, 3000);
    }

    // Additional utility methods for potential future use
    public getPresetList(): string[] {
        return Object.keys(this.presets);
    }

    public getPreset(provider: string): SmtpPreset | undefined {
        return this.presets[provider];
    }

    public addCustomPreset(name: string, preset: SmtpPreset): void {
        this.presets[name] = preset;
    }
}

// Export for global access (similar to setup-keys.ts)
declare global {
    interface Window {
        setupEmailConfigurator: SetupEmailConfigurator;
        setSmtpPreset: (provider: string) => void;
    }
}

// Initialize and export to global scope
const setupEmailConfigurator = new SetupEmailConfigurator();
window.setupEmailConfigurator = setupEmailConfigurator;
window.setSmtpPreset = (provider: string) => setupEmailConfigurator.setSmtpPreset(provider);

export { SetupEmailConfigurator };