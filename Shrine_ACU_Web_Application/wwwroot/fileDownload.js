window.shrineDownload = window.shrineDownload || {};

window.shrineDownload.downloadFileFromBase64 = (fileName, contentType, base64Content) => {
    const bytes = Uint8Array.from(atob(base64Content), c => c.charCodeAt(0));
    const blob = new Blob([bytes], { type: contentType || 'application/octet-stream' });
    const url = URL.createObjectURL(blob);

    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = fileName || 'download';
    document.body.appendChild(anchor);
    anchor.click();
    document.body.removeChild(anchor);

    setTimeout(() => URL.revokeObjectURL(url), 0);
};
