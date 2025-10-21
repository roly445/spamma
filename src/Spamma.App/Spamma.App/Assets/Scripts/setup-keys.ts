interface EntropyPoint {
    x: number;
    y: number;
    timestamp: number;
    velocity: number;
}

export class SetupKeyGenerator {
    private entropyData: EntropyPoint[] = [];
    private readonly REQUIRED_ENTROPY_POINTS = 100;
    

    constructor() {
        if (document.readyState !== 'loading') {
            this.initialize();
        } else {
            document.addEventListener('DOMContentLoaded', e => this.initialize());
        }
    }


    public initialize(): void {
        this.setupEntropyCollection();
        this.setupSubmit();
        this.setupRegenerationHandlers();

    }

    private setupEntropyCollection(): void {
        const entropyArea = document.getElementById('entropy-area');
        const entropyProgress = document.getElementById('entropy-progress');
        const entropyPercent = document.getElementById('entropy-percent');
        const entropyDots = document.getElementById('entropy-dots');
        const generateBtn = document.getElementById('generate-btn') as HTMLButtonElement;

        if (!entropyArea || !generateBtn) return; // Already completed

        // Track mouse movements
        entropyArea.addEventListener('mousemove', (e: MouseEvent) => {
            this.collectEntropy(e, entropyArea, entropyProgress, entropyPercent, entropyDots, generateBtn);
        });
    }
    
    private setupSubmit(): void {
        const keysForm = document.getElementById('keys-form');
         if (!keysForm) return;
         
        keysForm.addEventListener('submit', (e: Event) => {
            this.generateAndSubmitKeys(e);
        });
    }

    private collectEntropy(
        e: MouseEvent,
        entropyArea: HTMLElement,
        entropyProgress: HTMLElement | null,
        entropyPercent: HTMLElement | null,
        entropyDots: HTMLElement | null,
        generateBtn: HTMLButtonElement
    ): void {
        if (this.entropyData.length < this.REQUIRED_ENTROPY_POINTS) {
            const rect = entropyArea.getBoundingClientRect();
            const x = e.clientX - rect.left;
            const y = e.clientY - rect.top;
            const timestamp = Date.now();
            
            this.entropyData.push({
                x,
                y,
                timestamp,
                velocity: this.entropyData.length > 0 ? 
                    Math.sqrt(
                        Math.pow(x - this.entropyData[this.entropyData.length - 1].x, 2) + 
                        Math.pow(y - this.entropyData[this.entropyData.length - 1].y, 2)
                    ) : 0
            });

            this.updateProgress(entropyProgress, entropyPercent, entropyDots, generateBtn);
        }
    }

    private updateProgress(
        entropyProgress: HTMLElement | null,
        entropyPercent: HTMLElement | null,
        entropyDots: HTMLElement | null,
        generateBtn: HTMLButtonElement
    ): void {
        const progress = Math.min((this.entropyData.length / this.REQUIRED_ENTROPY_POINTS) * 100, 100);
        
        if (entropyProgress) {
            entropyProgress.style.width = `${progress}%`;
        }
        
        if (entropyPercent) {
            entropyPercent.textContent = `${Math.round(progress)}%`;
        }

        // Add visual dot
        if (this.entropyData.length % 5 === 0 && entropyDots) {
            const dot = document.createElement('div');
            dot.className = 'w-2 h-2 bg-blue-500 rounded-full';
            entropyDots.appendChild(dot);
        }

        // Enable generate button when enough entropy
        if (this.entropyData.length >= this.REQUIRED_ENTROPY_POINTS) {
            generateBtn.disabled = false;
            generateBtn.className = 'inline-flex items-center px-6 py-3 bg-blue-600 text-white font-medium rounded-lg hover:bg-blue-700 transition-all duration-300';
            
            if (entropyProgress) {
                entropyProgress.className = 'bg-green-600 h-3 rounded-full transition-all duration-300';
            }
        }
    }

    public generateAndSubmitKeys(e: Event): void {
        const generateBtn = document.getElementById('generate-btn') as HTMLButtonElement;
        const generateBtnText = document.getElementById('generate-btn-text');
        
        if (!generateBtn || !generateBtnText) return;

        // Show loading state
        generateBtn.disabled = true;
        generateBtn.className = 'inline-flex items-center px-6 py-3 bg-blue-400 text-white font-medium rounded-lg cursor-not-allowed transition-all duration-300';
        generateBtnText.innerHTML = '<svg class="animate-spin -ml-1 mr-2 h-5 w-5 text-white" fill="none" viewBox="0 0 24 24"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path></svg>Generating...';
        
        // Create entropy hash from mouse movements
        const entropyString = this.entropyData.map(point => 
            `${point.x},${point.y},${point.timestamp},${point.velocity}`
        ).join('|');
        
        // Generate signing key (32 random bytes -> Base64)
        const signingBytes = new Uint8Array(32);
        crypto.getRandomValues(signingBytes);
        
        // Mix in mouse entropy
        const entropyHash = this.simpleHash(entropyString);
        for (let i = 0; i < signingBytes.length; i++) {
            signingBytes[i] ^= entropyHash[i % entropyHash.length];
        }
        
        const generatedSigningKey = btoa(String.fromCharCode(...signingBytes));
        
        // Generate JWT key (64 character alphanumeric)
        const jwtChars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghijklmnopqrstuvwxyz';
        const jwtBytes = new Uint8Array(64);
        crypto.getRandomValues(jwtBytes);
        
        let generatedJwtKey = '';
        for (let i = 0; i < 64; i++) {
            // Mix in entropy and convert to character
            const entropyInfluence = this.entropyData[i % this.entropyData.length];
            const mixedValue = (jwtBytes[i] + entropyInfluence.x + entropyInfluence.y) % jwtChars.length;
            generatedJwtKey += jwtChars[mixedValue];
        }
        
        // Set the values in the hidden form
        const signingKeyInput = document.getElementById('signing-key-input') as HTMLInputElement;
        const jwtKeyInput = document.getElementById('jwt-key-input') as HTMLInputElement;
        const form = document.getElementById('keys-form') as HTMLFormElement;
        
        if (signingKeyInput && jwtKeyInput && form) {
            signingKeyInput.value = generatedSigningKey;
            jwtKeyInput.value = generatedJwtKey;
        }
        else {
            e.preventDefault()
        }
    }

    // Simple hash function for entropy mixing
    private simpleHash(str: string): number[] {
        let hash = 0;
        const result: number[] = [];
        for (let i = 0; i < str.length; i++) {
            const char = str.charCodeAt(i);
            hash = ((hash << 5) - hash) + char;
            hash = hash & hash; // Convert to 32bit integer
            if (i % 4 === 0) result.push(Math.abs(hash) % 256);
        }
        return result;
    }

    private setupRegenerationHandlers(): void {
        
            // Regenerate keys button
            const regenerateBtn = document.getElementById('regenerate-keys-btn');
            const existingKeysActions = document.getElementById('existing-keys-actions');
            regenerateBtn?.addEventListener('click', () => {
                const warning = document.getElementById('regeneration-warning');
                if (warning) warning.style.display = 'block';
                if (existingKeysActions) existingKeysActions.style.display = 'none';
            });
            
            // Proceed with regeneration
            const proceedBtn = document.getElementById('proceed-regeneration-btn');
            proceedBtn?.addEventListener('click', () => {
                // Hide existing keys section and show generation section
                const existingSection = document.querySelector('.bg-yellow-50')?.closest('div');
                const blueSection = document.querySelector('.bg-blue-50')?.closest('div');
                const warning = document.getElementById('regeneration-warning');
                const generationSection = document.getElementById('key-generation-section');

                
                if (existingSection) existingSection.style.display = 'none';
                if (blueSection) blueSection.style.display = 'none';
                if (warning) warning.style.display = 'none';
                if (generationSection) generationSection.style.display = 'block';
               
            });
            
            // Cancel regeneration
            const cancelBtn = document.getElementById('cancel-regeneration-btn');
            cancelBtn?.addEventListener('click', () => {
                const warning = document.getElementById('regeneration-warning');
                if (warning) warning.style.display = 'none';
                if (existingKeysActions) existingKeysActions.style.display = 'flex';
            });

    }
}

// Initialize and export to global scope
const setupKeyGenerator = new SetupKeyGenerator();