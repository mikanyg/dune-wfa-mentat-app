// Theme handling. The preference ("light", "dark" or "system") is kept in localStorage;
// the resolved theme is written to <html data-theme>. Also applied before Blazor starts (see index.html).
window.mahdi = (function () {
    const key = "mahdi.theme";
    const media = window.matchMedia("(prefers-color-scheme: dark)");

    function resolve(preference) {
        return preference === "light" || preference === "dark" ? preference : (media.matches ? "dark" : "light");
    }

    function apply(preference) {
        const theme = resolve(preference);
        document.documentElement.dataset.theme = theme;
        const meta = document.querySelector('meta[name="theme-color"]');
        if (meta) {
            meta.setAttribute("content", theme === "dark" ? "#12141f" : "#efe2c4");
        }
        return theme;
    }

    function read() {
        try {
            return localStorage.getItem(key) || "system";
        } catch {
            return "system";
        }
    }

    media.addEventListener("change", () => apply(read()));

    return {
        storageGet: (name) => {
            try {
                return localStorage.getItem(name);
            } catch {
                return null;
            }
        },
        storageSet: (name, value) => {
            try {
                localStorage.setItem(name, value);
                return true;
            } catch {
                return false;
            }
        },
        storageRemove: (name) => {
            try {
                localStorage.removeItem(name);
            } catch {
                // Nothing stored, nothing to remove.
            }
        },
        initTheme: () => apply(read()),
        getThemePreference: read,
        setThemePreference: (preference) => {
            try {
                localStorage.setItem(key, preference);
            } catch {
                // Storage can be unavailable (private browsing); the theme still applies for this visit.
            }
            return apply(preference);
        },
    };
})();

window.mahdi.initTheme();
