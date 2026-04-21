import { describe, it, expect, beforeEach, vi } from 'vitest';
import { SetupEmailConfigurator } from './setup-email';

describe('SetupEmailConfigurator', () => {
    beforeEach(() => {
        // Reset the DOM before each test
        document.body.innerHTML = '';
    });

    it('should initialize without throwing', () => {
        // Arrange & Act
        const instantiate = () => new SetupEmailConfigurator();

        // Assert
        expect(instantiate).not.toThrow();
    });

    it('should wire up preset buttons using data-preset attribute', () => {
        // Arrange
        document.body.innerHTML = `
            <button data-preset="development">Development</button>
            <button data-preset="gmail">Gmail</button>
            <input id="smtp-host" type="text" />
            <input id="smtp-port" type="text" />
            <input id="use-ssl" type="checkbox" />
        `;

        // Act
        const configurator = new SetupEmailConfigurator();

        // Assert
        const devButton = document.querySelector('[data-preset="development"]') as HTMLButtonElement;
        const gmailButton = document.querySelector('[data-preset="gmail"]') as HTMLButtonElement;
        
        expect(devButton).toBeTruthy();
        expect(gmailButton).toBeTruthy();
    });

    it('should apply development preset when development button clicked', () => {
        // Arrange
        document.body.innerHTML = `
            <button data-preset="development">Development</button>
            <input id="smtp-host" type="text" />
            <input id="smtp-port" type="text" />
            <input id="use-ssl" type="checkbox" />
        `;

        const configurator = new SetupEmailConfigurator();

        const hostInput = document.getElementById('smtp-host') as HTMLInputElement;
        const portInput = document.getElementById('smtp-port') as HTMLInputElement;
        const sslInput = document.getElementById('use-ssl') as HTMLInputElement;

        // Act
        const devButton = document.querySelector('[data-preset="development"]') as HTMLButtonElement;
        devButton.click();

        // Assert
        expect(hostInput.value).toBe('localhost');
        expect(portInput.value).toBe('1025');
        expect(sslInput.checked).toBe(false);
    });

    it('should apply gmail preset when gmail button clicked', () => {
        // Arrange
        document.body.innerHTML = `
            <button data-preset="gmail">Gmail</button>
            <input id="smtp-host" type="text" />
            <input id="smtp-port" type="text" />
            <input id="use-ssl" type="checkbox" />
        `;

        const configurator = new SetupEmailConfigurator();

        const hostInput = document.getElementById('smtp-host') as HTMLInputElement;
        const portInput = document.getElementById('smtp-port') as HTMLInputElement;
        const sslInput = document.getElementById('use-ssl') as HTMLInputElement;

        // Act
        const gmailButton = document.querySelector('[data-preset="gmail"]') as HTMLButtonElement;
        gmailButton.click();

        // Assert
        expect(hostInput.value).toBe('smtp.gmail.com');
        expect(portInput.value).toBe('587');
        expect(sslInput.checked).toBe(true);
    });

    it('should trigger input and change events for Blazor binding updates', () => {
        // Arrange
        document.body.innerHTML = `
            <button data-preset="development">Development</button>
            <input id="smtp-host" type="text" />
            <input id="smtp-port" type="text" />
            <input id="use-ssl" type="checkbox" />
        `;

        const hostInput = document.getElementById('smtp-host') as HTMLInputElement;
        const inputSpy = vi.fn();
        const changeSpy = vi.fn();

        hostInput.addEventListener('input', inputSpy);
        hostInput.addEventListener('change', changeSpy);

        const configurator = new SetupEmailConfigurator();

        // Act
        const devButton = document.querySelector('[data-preset="development"]') as HTMLButtonElement;
        devButton.click();

        // Assert
        expect(inputSpy).toHaveBeenCalled();
        expect(changeSpy).toHaveBeenCalled();
    });

    it('should return list of preset providers', () => {
        // Arrange & Act
        const configurator = new SetupEmailConfigurator();
        const presetList = configurator.getPresetList();

        // Assert
        expect(presetList).toContain('development');
        expect(presetList).toContain('gmail');
        expect(presetList).toContain('sendgrid');
        expect(presetList).toContain('mailgun');
    });

    it('should return preset configuration when getPreset is called', () => {
        // Arrange & Act
        const configurator = new SetupEmailConfigurator();
        const gmailPreset = configurator.getPreset('gmail');

        // Assert
        expect(gmailPreset).toEqual({
            host: 'smtp.gmail.com',
            port: 587,
            ssl: true
        });
    });

    it('should allow adding custom presets', () => {
        // Arrange
        const configurator = new SetupEmailConfigurator();
        
        // Act
        configurator.addCustomPreset('custom', {
            host: 'smtp.custom.com',
            port: 465,
            ssl: true
        });

        const customPreset = configurator.getPreset('custom');

        // Assert
        expect(customPreset).toEqual({
            host: 'smtp.custom.com',
            port: 465,
            ssl: true
        });
    });
});
