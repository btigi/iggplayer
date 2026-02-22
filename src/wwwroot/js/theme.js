const STORAGE_KEY = 'iggplayer-theme';

export function getTheme() {
    return document.documentElement.dataset.theme || 'dark';
}

export function setTheme(theme) {
    document.documentElement.dataset.theme = theme;
    try {
        localStorage.setItem(STORAGE_KEY, theme);
    } catch (_) {}
}

export function initTheme() {
    let theme;
    try {
        theme = localStorage.getItem(STORAGE_KEY);
    } catch (_) {}
    if (!theme) {
        theme = window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    }
    document.documentElement.dataset.theme = theme;
}
