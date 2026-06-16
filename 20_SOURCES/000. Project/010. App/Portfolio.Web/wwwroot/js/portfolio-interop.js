window.portfolioInterop = {
    scrollToTop: () => window.scrollTo({ top: 0, behavior: 'smooth' }),
    copyText: (text) => navigator.clipboard?.writeText(text)
};
