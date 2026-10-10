(function () {
    'use strict';

    // =====================================================
    // TABS  (supports /Settings#help)
    // =====================================================
    var tabs = Array.prototype.slice.call(document.querySelectorAll('.st-tab'));
    var panels = Array.prototype.slice.call(document.querySelectorAll('.st-panel'));

    function showPanel(name, updateHash) {
        var found = false;

        tabs.forEach(function (tab) {
            var on = tab.dataset.panel === name;
            if (on) found = true;
            tab.classList.toggle('active', on);
            tab.setAttribute('aria-selected', on ? 'true' : 'false');
        });

        if (!found) return showPanel('appearance', updateHash);

        panels.forEach(function (panel) {
            panel.classList.toggle('active', panel.id === 'panel-' + name);
        });

        if (updateHash && history.replaceState) {
            history.replaceState(null, '', '#' + name);
        }
    }

    tabs.forEach(function (tab) {
        tab.addEventListener('click', function () {
            showPanel(tab.dataset.panel, true);
        });
    });

    showPanel((location.hash || '').replace('#', '') || 'appearance', false);


    // =====================================================
    // THEME PICKER
    // =====================================================
    var group = document.getElementById('themeGroup');
    var options = Array.prototype.slice.call(document.querySelectorAll('.st-theme'));
    var status = document.getElementById('themeStatus');

    if (!group || !window.msTheme) return;

    function syncTheme() {
        var pref = window.msTheme.get();
        var resolved = document.documentElement.getAttribute('data-theme') || 'light';

        options.forEach(function (option) {
            var on = option.dataset.mode === pref;
            option.setAttribute('aria-checked', on ? 'true' : 'false');
            option.tabIndex = on ? 0 : -1;
        });

        if (status) {
            status.textContent = pref === 'system'
                ? 'Following your device (currently ' + resolved + '). Saved on this device only.'
                : 'Using the ' + pref + ' theme. Saved on this device only.';
        }
    }

    function choose(mode) {
        window.msTheme.set(mode);
        syncTheme();
    }

    options.forEach(function (option) {
        option.addEventListener('click', function () {
            choose(option.dataset.mode);
        });
    });

    // arrow keys move through the radio group
    group.addEventListener('keydown', function (e) {
        var keys = ['ArrowRight', 'ArrowDown', 'ArrowLeft', 'ArrowUp'];
        if (keys.indexOf(e.key) === -1) return;

        e.preventDefault();

        var current = options.findIndex(function (o) { return o.getAttribute('aria-checked') === 'true'; });
        var step = (e.key === 'ArrowRight' || e.key === 'ArrowDown') ? 1 : -1;
        var next = options[(current + step + options.length) % options.length];

        choose(next.dataset.mode);
        next.focus();
    });

    // refresh the status text if the device theme changes while "System" is selected
    var mq = window.matchMedia('(prefers-color-scheme: dark)');
    if (mq.addEventListener) mq.addEventListener('change', syncTheme);

    window.addEventListener('storage', syncTheme);

    syncTheme();
})();