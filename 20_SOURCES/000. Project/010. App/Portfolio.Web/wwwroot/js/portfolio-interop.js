window.portfolioInterop = {
    scrollToTop: () => window.scrollTo({ top: 0, behavior: 'smooth' }),
    scrollToId: (id) => document.getElementById(id)?.scrollIntoView({ behavior: 'smooth' }),
    copyText: (text) => navigator.clipboard?.writeText(text)
};
