window.dentalIdle = (function () {
    let timerId = null;
    let dotnetRef = null;
    let timeoutMs = 0;
    let listenersAttached = false;

    function reset() {
        if (timerId) {
            clearTimeout(timerId);
        }
        if (dotnetRef && timeoutMs > 0) {
            timerId = setTimeout(function () {
                dotnetRef.invokeMethodAsync('OnIdleTimeout');
            }, timeoutMs);
        }
    }

    function attachListeners() {
        if (listenersAttached) return;
        ['mousemove', 'keydown', 'click', 'touchstart', 'scroll'].forEach(function (evt) {
            document.addEventListener(evt, reset, { passive: true });
        });
        listenersAttached = true;
    }

    return {
        start: function (ref, ms) {
            dotnetRef = ref;
            timeoutMs = ms;
            attachListeners();
            reset();
        },
        reset: function () {
            reset();
        },
        stop: function () {
            if (timerId) {
                clearTimeout(timerId);
                timerId = null;
            }
            dotnetRef = null;
        }
    };
})();
