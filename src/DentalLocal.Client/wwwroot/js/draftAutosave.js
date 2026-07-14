window.dentalDraft = {
    set: function (key, json) {
        try {
            localStorage.setItem('dental_draft_' + key, json);
        } catch (e) {
            /* no-op */
        }
    },
    get: function (key) {
        try {
            return localStorage.getItem('dental_draft_' + key);
        } catch (e) {
            return null;
        }
    },
    remove: function (key) {
        try {
            localStorage.removeItem('dental_draft_' + key);
        } catch (e) {
            /* no-op */
        }
    }
};
