// Families.Web — Blazor interop helpers

window.familyApp = {
    scrollToTop: () => window.scrollTo({ top: 0, behavior: 'smooth' }),
    copyText: async (text) => {
        try {
            await navigator.clipboard.writeText(text);
            return true;
        } catch {
            return false;
        }
    },
    uploadFileAt: async (inputId, fileIndex, total, ticket, dotNetRef, messages) => {
        const input = document.getElementById(inputId);
        const files = input ? Array.from(input.files || []) : [];
        if (fileIndex < 0 || fileIndex >= files.length || files.length !== total) {
            throw new Error(messages.inputUnavailable);
        }

        return await uploadOneFile(
            files[fileIndex], ticket, fileIndex + 1, total, dotNetRef, messages);
    }
};

function uploadOneFile(file, ticket, index, total, dotNetRef, messages) {
    return new Promise((resolve, reject) => {
        const xhr = new XMLHttpRequest();
        xhr.open("POST", "/api/families/uploads", true);
        xhr.withCredentials = true;
        xhr.timeout = 30 * 60 * 1000;
        xhr.setRequestHeader("X-Family-Upload-Ticket", ticket);

        xhr.upload.onprogress = (event) => {
            const size = event.lengthComputable ? event.total : file.size;
            dotNetRef.invokeMethodAsync("ReportUploadProgress", index, total, event.loaded, size)
                .catch(() => { /* Upload remains independent if the Blazor circuit reconnects. */ });
        };

        xhr.onload = () => {
            let payload = null;
            try {
                payload = xhr.responseText ? JSON.parse(xhr.responseText) : null;
            } catch {
                payload = null;
            }

            if (xhr.status >= 200 && xhr.status < 300 && payload) {
                resolve(payload);
                return;
            }

            const fallback = payload && payload.error === "upload_cancelled"
                ? messages.cancelled
                : xhr.status === 410
                ? messages.ticketExpired
                : xhr.status === 413
                    ? messages.requestTooLarge
                    : xhr.status === 403
                        ? messages.unauthorized
                        : `${messages.serverRejected} (HTTP ${xhr.status})`;
            reject(new Error(fallback));
        };
        xhr.onerror = () => reject(new Error(messages.networkError));
        xhr.ontimeout = () => reject(new Error(messages.timeout));
        xhr.onabort = () => reject(new Error(messages.cancelled));

        const form = new FormData();
        form.append("file", file, file.name);
        xhr.send(form);
    });
}
