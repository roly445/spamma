export function beforeWebAssemblyStart(options) {
    const loadingOverlay = document.createElement('div');
    loadingOverlay.id = 'blazor-wasm-loading';
    loadingOverlay.className = 'wasm-loading-overlay';
    loadingOverlay.style.display = 'flex';

    // Set the HTML content
    loadingOverlay.innerHTML = `
       <div class="loader h-10 w-10 bg-blue-600 rounded-lg flex items-center justify-center">
  <svg
    class="logo"
    xmlns="http://www.w3.org/2000/svg"
    width="150"
    height="150"
    fill="currentColor"
    viewBox="0 0 24 24"
  >
    <path
      d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"
    ></path>
  </svg>
</div>
    `;
    document.body.appendChild(loadingOverlay);
}

