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
    }
};
