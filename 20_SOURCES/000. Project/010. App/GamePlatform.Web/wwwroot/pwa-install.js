(() => {
    let deferredPrompt = null;
    const listeners = new Set();

    const isStandalone = () =>
        window.matchMedia?.("(display-mode: standalone)").matches === true
        || window.matchMedia?.("(display-mode: fullscreen)").matches === true
        || window.navigator.standalone === true;

    const isIos = () => /iphone|ipad|ipod/i.test(window.navigator.userAgent)
        && !window.MSStream;

    const isSamsungInternet = () => /SamsungBrowser\//i.test(window.navigator.userAgent);

    const status = () => ({
        canInstall: deferredPrompt !== null,
        isIos: isIos(),
        isSamsungInternet: isSamsungInternet(),
        isStandalone: isStandalone()
    });

    const notify = () => {
        const value = status();
        for (const listener of listeners) {
            listener.invokeMethodAsync("UpdateInstallState", value).catch(() => listeners.delete(listener));
        }
    };

    window.addEventListener("beforeinstallprompt", event => {
        event.preventDefault();
        deferredPrompt = event;
        notify();
    });

    window.addEventListener("appinstalled", () => {
        deferredPrompt = null;
        notify();
    });

    if ("serviceWorker" in navigator) {
        window.addEventListener("load", async () => {
            if (isSamsungInternet()) {
                const registrations = await navigator.serviceWorker.getRegistrations().catch(() => []);
                await Promise.all(registrations
                    .filter(registration => registration.scope.startsWith(location.origin + "/"))
                    .map(registration => registration.unregister()));
                await window.caches?.delete("maru-expedition-shell-v1");
                return;
            }

            navigator.serviceWorker.register("/service-worker.js", { scope: "/" }).catch(() => {});
        });
    }

    window.maruPwa = {
        register(listener) {
            listeners.add(listener);
            return status();
        },
        unregister(listener) {
            listeners.delete(listener);
        },
        async requestInstall() {
            if (isStandalone()) return "installed";
            if (isIos()) return "ios";
            if (isSamsungInternet()) return "samsung";
            if (!deferredPrompt) return "unavailable";
            const prompt = deferredPrompt;
            deferredPrompt = null;
            await prompt.prompt();
            const choice = await prompt.userChoice;
            notify();
            return choice.outcome === "accepted" ? "accepted" : "dismissed";
        }
    };
})();
