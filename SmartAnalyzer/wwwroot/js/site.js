// Auto-dismiss alerts after 5 seconds
document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('.alert').forEach(alert => {
        setTimeout(() => {
            const bsAlert = bootstrap.Alert.getOrCreateInstance(alert);
            bsAlert?.close();
        }, 5000);
    });
});

// Export a Chart.js chart with legend shown inline
async function exportWithInlineLegend(chart, canvasId, filename) {
    if (!chart) { console.warn('Chart not ready'); return; }
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;

    // Save original legend state
    const origDisplay  = chart.options.plugins.legend.display;
    const origPosition = chart.options.plugins.legend.position;
    const origLabels   = chart.options.plugins.legend.labels || {};

    // Show legend inside chart for export
    chart.options.plugins.legend.display  = true;
    chart.options.plugins.legend.position = 'bottom';
    chart.options.plugins.legend.labels   = {
        ...origLabels,
        font: { family: 'Cairo', size: 12 },
        padding: 14,
        boxWidth: 14
    };
    chart.update('none');

    // Wait for 2 animation frames to ensure canvas is fully painted
    await new Promise(r => requestAnimationFrame(r));
    await new Promise(r => requestAnimationFrame(r));

    // Copy to new canvas with white background
    const exp = document.createElement('canvas');
    exp.width  = canvas.width;
    exp.height = canvas.height;
    const ctx = exp.getContext('2d');
    ctx.fillStyle = '#ffffff';
    ctx.fillRect(0, 0, exp.width, exp.height);
    ctx.drawImage(canvas, 0, 0);

    // Download
    const date = new Date().toISOString().slice(0, 10);
    const link = document.createElement('a');
    link.download = `${filename}-${date}.png`;
    link.href = exp.toDataURL('image/png', 1.0);
    link.click();

    // Restore original legend
    chart.options.plugins.legend.display  = origDisplay;
    chart.options.plugins.legend.position = origPosition;
    chart.options.plugins.legend.labels   = origLabels;
    chart.update('none');
}

// Export a plain canvas with white background
function exportChartCanvas(canvasId, filename) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;
    const exp = document.createElement('canvas');
    exp.width  = canvas.width;
    exp.height = canvas.height;
    const ctx = exp.getContext('2d');
    ctx.fillStyle = '#ffffff';
    ctx.fillRect(0, 0, exp.width, exp.height);
    ctx.drawImage(canvas, 0, 0);
    const date = new Date().toISOString().slice(0, 10);
    const link = document.createElement('a');
    link.download = `${filename}-${date}.png`;
    link.href = exp.toDataURL('image/png', 1.0);
    link.click();
}
