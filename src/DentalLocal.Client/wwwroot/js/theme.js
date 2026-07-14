window.dentalTheme = {
    apply: function (mode, accent) {
        document.documentElement.setAttribute('data-theme', mode);
        document.documentElement.setAttribute('data-accent', accent);
    },
    get: function (key) {
        try {
            return localStorage.getItem(key);
        } catch (e) {
            return null;
        }
    },
    set: function (key, value) {
        try {
            localStorage.setItem(key, value);
        } catch (e) {
            /* no-op */
        }
    }
};
