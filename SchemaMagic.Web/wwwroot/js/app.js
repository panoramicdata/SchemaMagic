window.openSchemaInNewTab = (htmlContent) => {
    const blob = new Blob([htmlContent], { type: 'text/html' });
    const url = URL.createObjectURL(blob);
    const newWindow = window.open(url, '_blank');
    
    // Clean up the URL after a delay
    setTimeout(() => URL.revokeObjectURL(url), 1000);
};

window.downloadFile = (fileName, content, contentType) => {
    const blob = new Blob([content], { type: contentType });
    const url = URL.createObjectURL(blob);
    
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    
    URL.revokeObjectURL(url);
};

// Set focus on repository input when page loads
window.setRepositoryInputFocus = () => {
    const repoInput = document.getElementById('repoUrl');
    if (repoInput) {
        repoInput.focus();
    }
};

// Enhanced error handling
window.addEventListener('unhandledrejection', (event) => {
    console.error('Unhandled promise rejection:', event.reason);
});

// Performance monitoring
window.addEventListener('load', () => {
    if ('performance' in window) {
        const loadTime = performance.now();
        console.log(`SchemaMagic Web loaded in ${loadTime.toFixed(2)}ms`);
    }
});