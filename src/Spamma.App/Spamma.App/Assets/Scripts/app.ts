// Main application TypeScript entry point
console.log('App TypeScript loaded');

// Example function to export to global scope for Blazor interop
function initializeApp(): void {
    document.querySelectorAll('form[data-disable-form]')
        .forEach(form => 
        form.addEventListener('submit', event => {
            form.querySelectorAll<HTMLButtonElement>('button').forEach(button =>button.disabled=true);
        }));
    
    
};


// Export functions to global scope if needed by Blazor


// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    initializeApp();
});