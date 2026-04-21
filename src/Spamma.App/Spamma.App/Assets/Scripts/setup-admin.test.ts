import { describe, it, expect, beforeEach, vi } from 'vitest';
import { SetupAdmin } from './setup-admin';

describe('SetupAdmin', () => {
    beforeEach(() => {
        // Reset the DOM before each test
        document.body.innerHTML = '';
    });

    it('should initialize without throwing', () => {
        // Arrange & Act
        const instantiate = () => new SetupAdmin();

        // Assert
        expect(instantiate).not.toThrow();
    });

    it('should attach event listener to create-additional-admin-btn', () => {
        // Arrange
        document.body.innerHTML = `
            <div>
                <div>
                    <button id="create-additional-admin-btn">Create Additional Admin</button>
                </div>
            </div>
            <div id="admin-form-section" style="display: none;">Form Section</div>
        `;

        const addEventListenerSpy = vi.spyOn(document.getElementById('create-additional-admin-btn')!, 'addEventListener');

        // Act
        new SetupAdmin();
        document.dispatchEvent(new Event('DOMContentLoaded'));

        // Assert
        expect(addEventListenerSpy).toHaveBeenCalledWith('click', expect.any(Function));
    });

    it('should toggle visibility when create-additional-admin-btn is clicked', () => {
        // Arrange
        document.body.innerHTML = `
            <div id="existing-section">
                <div>
                    <button id="create-additional-admin-btn">Create Additional Admin</button>
                </div>
            </div>
            <div id="admin-form-section" style="display: none;">Form Section</div>
        `;

        // Act
        new SetupAdmin();
        document.dispatchEvent(new Event('DOMContentLoaded'));

        const btn = document.getElementById('create-additional-admin-btn') as HTMLButtonElement;
        btn.click();

        // Assert
        const existingSection = btn.closest('div')?.parentElement as HTMLElement;
        const formSection = document.getElementById('admin-form-section') as HTMLElement;

        expect(existingSection.style.display).toBe('none');
        expect(formSection.style.display).toBe('block');
    });
});
