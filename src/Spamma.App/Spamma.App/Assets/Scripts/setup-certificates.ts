class SetupCertificatesConfigurator {
    private eventSource: EventSource | null = null;

    constructor() {
        if (document.readyState !== 'loading') {
            this.initialize();
        } else {
            document.addEventListener('DOMContentLoaded', () => this.initialize());
        }
    }

    private initialize(): void {
        this.setupEventListeners();
        this.updateUI();
    }

    private setupEventListeners(): void {
        const optSkip = document.getElementById('opt-skip') as HTMLInputElement;
        const optLetsencrypt = document.getElementById('opt-letsencrypt') as HTMLInputElement;
        const generateBtn = document.getElementById('generate-btn') as HTMLButtonElement;

        if (optSkip) {
            optSkip.addEventListener('change', () => this.updateUI());
        }

        if (optLetsencrypt) {
            optLetsencrypt.addEventListener('change', () => this.updateUI());
        }

        if (generateBtn) {
            generateBtn.addEventListener('click', (e) => this.handleGenerateClick(e));
        }
    }

    private updateUI(): void {
        const optSkip = document.getElementById('opt-skip') as HTMLInputElement;
        const optLetsencrypt = document.getElementById('opt-letsencrypt') as HTMLInputElement;
        const letsencryptConfig = document.getElementById('letsencrypt-config') as HTMLDivElement;
        const actionButtons = document.getElementById('action-buttons') as HTMLDivElement;
        const generateBtn = document.getElementById('generate-btn') as HTMLButtonElement;
        const skipLabel = document.getElementById('skip-label') as HTMLLabelElement;
        const letsencryptLabel = document.getElementById('letsencrypt-label') as HTMLLabelElement;

        // Update label styling based on selection
        if (optSkip?.checked) {
            skipLabel?.classList.add('border-blue-500', 'bg-blue-50');
            skipLabel?.classList.remove('border-gray-200', 'hover:border-gray-300');
            letsencryptLabel?.classList.remove('border-blue-500', 'bg-blue-50');
            letsencryptLabel?.classList.add('border-gray-200', 'hover:border-gray-300');
            letsencryptConfig?.classList.add('hidden');
            actionButtons?.classList.remove('hidden');
            // Show the "Back" and "Continue" buttons for skip option
        } else if (optLetsencrypt?.checked) {
            letsencryptLabel?.classList.add('border-blue-500', 'bg-blue-50');
            letsencryptLabel?.classList.remove('border-gray-200', 'hover:border-gray-300');
            skipLabel?.classList.remove('border-blue-500', 'bg-blue-50');
            skipLabel?.classList.add('border-gray-200', 'hover:border-gray-300');
            letsencryptConfig?.classList.remove('hidden');
            actionButtons?.classList.add('hidden');
            // Hide the action buttons when generating certificate
        }
    }

    private async handleGenerateClick(event: Event): Promise<void> {
        event.preventDefault();

        const domainInput = document.getElementById('domain') as HTMLInputElement;
        const emailInput = document.getElementById('email') as HTMLInputElement;
        const generatingProgress = document.getElementById('generating-progress') as HTMLDivElement;
        const generateBtn = event.target as HTMLButtonElement;
        const progressBar = generatingProgress?.querySelector('.bg-blue-600') as HTMLDivElement;
        const progressText = generatingProgress?.querySelector('p') as HTMLParagraphElement;
        const errorMessage = document.getElementById('error-message') as HTMLDivElement;
        const errorText = document.getElementById('error-text') as HTMLParagraphElement;

        const domain = domainInput?.value?.trim();
        const email = emailInput?.value?.trim();

        // Clear previous error
        errorMessage?.classList.add('hidden');

        if (!domain || !email) {
            errorText.textContent = 'Please enter both domain and email address';
            errorMessage?.classList.remove('hidden');
            return;
        }

        // Show progress container
        generatingProgress?.classList.remove('hidden');
        generateBtn.disabled = true;

        try {
            await this.streamCertificateGeneration(domain, email, progressBar, progressText, errorMessage, errorText);
        } catch (error) {
            console.error('Certificate generation failed:', error);
            const errorMsg = error instanceof Error ? error.message : 'Certificate generation failed';
            errorText.textContent = errorMsg;
            errorMessage?.classList.remove('hidden');
            generatingProgress?.classList.add('hidden');
            generateBtn.disabled = false;
        }
    }

    private streamCertificateGeneration(
        domain: string,
        email: string,
        progressBar: HTMLDivElement,
        progressText: HTMLParagraphElement,
        errorMessage: HTMLDivElement,
        errorText: HTMLParagraphElement
    ): Promise<void> {
        return new Promise((resolve, reject) => {
            // Update UI to show progress
            if (progressText) {
                progressText.textContent = 'Connecting to certificate service...';
            }

            // POST the request with SSE response
            fetch('/api/setup/generate-certificate-stream', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ domain, email }),
            })
                .then((response) => {
                    if (!response.ok) {
                        throw new Error(`HTTP error! status: ${response.status}`);
                    }

                    // Read the response as text stream
                    if (!response.body) {
                        throw new Error('No response body');
                    }

                    const reader = response.body.getReader();
                    const decoder = new TextDecoder();
                    let buffer = '';
                    let completed = false;

                    const processChunk = (): void => {
                        reader.read().then(({ done, value }) => {
                            if (done) {
                                // Process any remaining buffer content
                                if (!completed && buffer.length > 0) {
                                    const lines = buffer.split('\n');
                                    for (const line of lines) {
                                        const trimmedLine = line.trim();
                                        if (trimmedLine.startsWith('data: ')) {
                                            const message = trimmedLine.substring(6);
                                            console.log('Final buffer message:', message);
                                            
                                            if (message.startsWith('complete:')) {
                                                completed = true;
                                                if (progressBar) {
                                                    progressBar.style.width = '100%';
                                                }
                                                if (progressText) {
                                                    progressText.textContent = 'Certificate generated successfully! Redirecting...';
                                                }
                                                setTimeout(() => {
                                                    window.location.href = '/setup/admin';
                                                }, 2000);
                                                resolve();
                                                return;
                                            }

                                            if (message.startsWith('error:')) {
                                                completed = true;
                                                const errorMsg = message.substring(6);
                                                console.error('Certificate error:', errorMsg);
                                                if (errorMessage && errorText) {
                                                    errorMessage.classList.remove('hidden');
                                                    errorText.textContent = errorMsg;
                                                }
                                                reject(new Error(errorMsg));
                                                return;
                                            }
                                        }
                                    }
                                }
                                
                                if (!completed) {
                                    console.error('Stream ended without completion. Final buffer:', buffer);
                                    reject(new Error('Certificate generation did not complete'));
                                }
                                return;
                            }

                            buffer += decoder.decode(value, { stream: true });
                            const lines = buffer.split('\n');

                            // Process complete lines (all but the last, which might be incomplete)
                            for (let i = 0; i < lines.length - 1; i++) {
                                const line = lines[i].trim();
                                if (line.startsWith('data: ')) {
                                    const message = line.substring(6);
                                    console.log('Received message:', message);

                                    // Check for completion messages first
                                    if (message.startsWith('complete:')) {
                                        completed = true;
                                        if (progressBar) {
                                            progressBar.style.width = '100%';
                                        }
                                        if (progressText) {
                                            progressText.textContent = 'Certificate generated successfully! Redirecting...';
                                        }
                                        setTimeout(() => {
                                            window.location.href = '/setup/admin';
                                        }, 2000);
                                        resolve();
                                        return;
                                    }

                                    if (message.startsWith('error:')) {
                                        completed = true;
                                        const errorMsg = message.substring(6);
                                        // Display error in page instead of alert
                                        if (errorMessage && errorText) {
                                            errorMessage.classList.remove('hidden');
                                            errorText.textContent = errorMsg;
                                        }
                                        reject(new Error(errorMsg));
                                        return;
                                    }

                                    this.handleProgressMessage(message, progressBar, progressText, errorMessage, errorText);
                                }
                            }

                            // Keep incomplete line in buffer
                            buffer = lines[lines.length - 1];
                            processChunk();
                        }).catch(reject);
                    };

                    processChunk();
                })
                .catch(reject);
        });
    }

    private handleProgressMessage(
        message: string,
        progressBar: HTMLDivElement,
        progressText: HTMLParagraphElement,
        errorMessage: HTMLDivElement,
        errorText: HTMLParagraphElement
    ): void {
        if (message.startsWith('Step')) {
            // Clear error on new step
            if (errorMessage) {
                errorMessage.classList.add('hidden');
            }

            // Parse step message: "Step X of Y: description"
            const match = message.match(/Step (\d+) of (\d+):/);
            if (match) {
                const current = parseInt(match[1], 10);
                const total = parseInt(match[2], 10);
                const percentage = Math.round((current / total) * 100);

                if (progressBar) {
                    progressBar.style.width = `${percentage}%`;
                }
                if (progressText) {
                    progressText.textContent = message;
                }

                console.log(`Progress: ${percentage}% - ${message}`);
            }
        }
    }

    private handleFormSubmit(event: Event): void {
        event.preventDefault();
        const optSkip = document.getElementById('opt-skip') as HTMLInputElement;

        if (optSkip?.checked) {
            // Submit the form normally for skip option
            const form = event.target as HTMLFormElement;
            form.submit();
        }
        // For letsencrypt, the generate button handles it
    }
}

// Initialize when DOM is ready
new SetupCertificatesConfigurator();

