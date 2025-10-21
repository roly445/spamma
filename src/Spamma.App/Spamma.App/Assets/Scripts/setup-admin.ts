export class SetupAdmin {
    constructor() {
        this.initialize();
    }

    private initialize() {
        document.addEventListener('DOMContentLoaded', function() {
            const additionalAdminBtn = document.getElementById('create-additional-admin-btn');
            additionalAdminBtn?.addEventListener('click', function() {
                const existingSection = this.closest('div').parentElement;
                const formSection = document.getElementById('admin-form-section');

                if (existingSection) existingSection.style.display = 'none';
                if (formSection) formSection.style.display = 'block';
            });
        });

        function setExample(fieldId, value) {
            const element = document.getElementById(fieldId) as HTMLInputElement;
            if (element) {
                element.value = value;
                element.dispatchEvent(new Event('change', { bubbles: true }));
            }
        }
    }
}