window.shrineTheme = (() => {
    const storageKey = "shrine_theme_preference";
    const systemDarkQuery = window.matchMedia("(prefers-color-scheme: dark)");
    let initialized = false;

    const normalize = (value) => {
        if (typeof value !== "string") {
            return "system";
        }

        const normalized = value.toLowerCase().trim();
        return normalized === "light" || normalized === "dark" ? normalized : "system";
    };

    const getPreference = () => {
        try {
            return normalize(window.localStorage.getItem(storageKey));
        } catch {
            return "system";
        }
    };

    const resolveTheme = (preference) => {
        const normalized = normalize(preference);
        if (normalized === "system") {
            return systemDarkQuery.matches ? "dark" : "light";
        }

        return normalized;
    };

    const applyRadzenTheme = (resolvedTheme) => {
        const themeLink = document.getElementById("radzen-theme");
        if (!(themeLink instanceof HTMLLinkElement)) {
            return;
        }

        const lightHref = themeLink.dataset.lightHref;
        const darkHref = themeLink.dataset.darkHref;
        const nextHref = resolvedTheme === "dark" ? darkHref : lightHref;

        if (!nextHref || themeLink.getAttribute("href") === nextHref) {
            return;
        }

        themeLink.setAttribute("href", nextHref);
    };

    const apply = (preference) => {
        const normalizedPreference = normalize(preference ?? getPreference());
        const resolvedTheme = resolveTheme(normalizedPreference);
        const root = document.documentElement;

        root.setAttribute("data-theme-preference", normalizedPreference);
        root.setAttribute("data-theme", resolvedTheme);
        root.style.colorScheme = resolvedTheme;
        applyRadzenTheme(resolvedTheme);
    };

    const setPreference = (preference) => {
        const normalizedPreference = normalize(preference);
        
        try {
            window.localStorage.setItem(storageKey, normalizedPreference);
        } catch (e) {
            console.error("Failed to save theme preference:", e);
        }
        
        apply(normalizedPreference);
    };

    const handleSystemPreferenceChange = () => {
        const currentPreference = getPreference();
        if (currentPreference === "system") {
            apply("system");
        }
    };

    const init = () => {
        apply(getPreference());

        if (document.readyState === "loading") {
            document.addEventListener("DOMContentLoaded", () => apply(getPreference()), { once: true });
        }

        if (!initialized) {
            if (typeof systemDarkQuery.addEventListener === "function") {
                systemDarkQuery.addEventListener("change", handleSystemPreferenceChange);
            } else if (typeof systemDarkQuery.addListener === "function") {
                systemDarkQuery.addListener(handleSystemPreferenceChange);
            }

            initialized = true;
        }
    };

    return {
        init,
        getPreference,
        apply,
        setPreference
    };
})();
