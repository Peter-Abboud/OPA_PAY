/**
 * OPA Pay recipient autocomplete — dropdown on focus, input, and hover.
 */
window.OpaRecipientLookup = (function () {
    function debounce(fn, ms) {
        let t;
        return function (...args) {
            clearTimeout(t);
            t = setTimeout(() => fn.apply(this, args), ms);
        };
    }

    function buildPanel(input) {
        let panel = input.parentElement.querySelector('.opa-suggest-panel');
        if (!panel) {
            panel = document.createElement('div');
            panel.className = 'opa-suggest-panel';
            input.parentElement.classList.add('opa-suggest-wrap');
            input.parentElement.appendChild(panel);
        }
        return panel;
    }

    function hidePanel(panel) {
        panel.classList.remove('show');
        panel.innerHTML = '';
    }

    function showPanel(panel) {
        panel.classList.add('show');
    }

    function renderItems(panel, items, onPick) {
        panel.innerHTML = '';
        if (!items || items.length === 0) {
            panel.innerHTML = '<div class="opa-suggest-empty">No matches — type at least 2 letters</div>';
            showPanel(panel);
            return;
        }

        items.forEach(item => {
            const div = document.createElement('button');
            div.type = 'button';
            div.className = 'opa-suggest-item';
            const accounts = (item.opaAccountNumbers || []).join(', ') || '—';
            const banks = (item.bankNames || []).join(', ') || '—';
            const countries = (item.countries || []).join(', ') || '—';
            const mobiles = (item.mobileNumbers || []).join(', ') || '—';
            div.innerHTML = `
                <strong>${item.fullName}</strong>
                <small class="d-block text-muted">${item.source || ''}</small>
                <small class="d-block"><span class="text-primary">OPA:</span> ${accounts}</small>
                <small class="d-block"><span class="text-secondary">Bank:</span> ${banks} · <span class="text-secondary">Country:</span> ${countries}</small>
                ${mobiles !== '—' ? `<small class="d-block">Mobile: ${mobiles}</small>` : ''}`;
            div.addEventListener('mousedown', e => {
                e.preventDefault();
                onPick(item);
                hidePanel(panel);
            });
            panel.appendChild(div);
        });
        showPanel(panel);
    }

    function initNameLookup(options) {
        const input = document.querySelector(options.nameInput);
        if (!input) return;

        const accountInput = options.accountInput ? document.querySelector(options.accountInput) : null;
        const bankInput = options.bankInput ? document.querySelector(options.bankInput) : null;
        const countryInput = options.countryInput ? document.querySelector(options.countryInput) : null;
        const mobileInput = options.mobileInput ? document.querySelector(options.mobileInput) : null;
        const panel = buildPanel(input);

        const fetchSuggestions = debounce(async () => {
            const term = input.value.trim();
            if (term.length < 2) {
                hidePanel(panel);
                return;
            }
            const url = `${options.lookupUrl}?term=${encodeURIComponent(term)}`;
            const res = await fetch(url);
            const data = await res.json();
            renderItems(panel, data, item => {
                input.value = item.fullName || '';
                if (accountInput && item.opaAccountNumbers?.length)
                    accountInput.value = item.opaAccountNumbers[0];
                if (bankInput && item.bankNames?.length)
                    bankInput.value = item.bankNames[0];
                if (countryInput && item.countries?.length)
                    countryInput.value = item.countries[0];
                if (mobileInput && item.mobileNumbers?.length)
                    mobileInput.value = item.mobileNumbers[0];
            });
        }, 250);

        input.addEventListener('input', fetchSuggestions);
        input.addEventListener('focus', fetchSuggestions);
        input.addEventListener('mouseenter', () => {
            if (input.value.trim().length >= 2) fetchSuggestions();
        });

        document.addEventListener('click', e => {
            if (!input.parentElement.contains(e.target))
                hidePanel(panel);
        });
    }

    function initMobilePickers(recipientsJson, options) {
        const recipients = JSON.parse(recipientsJson || '[]');
        const nameInput = document.querySelector(options.nameInput);
        const mobileInput = document.querySelector(options.mobileInput);
        if (!nameInput || !mobileInput) return;

        const namePanel = buildPanel(nameInput);
        const mobilePanel = buildPanel(mobileInput);

        function pickRecipient(r) {
            nameInput.value = r.fullName || '';
            if (r.mobileNumbers?.length)
                mobileInput.value = r.mobileNumbers[0];
        }

        function showRecipients(panel, filter, onPick) {
            const term = (filter || '').toLowerCase();
            const filtered = recipients.filter(r =>
                !term ||
                (r.fullName || '').toLowerCase().includes(term) ||
                (r.mobileNumbers || []).some(m => m.includes(term))
            );
            panel.innerHTML = '';
            if (filtered.length === 0) {
                panel.innerHTML = '<div class="opa-suggest-empty">No saved recipients</div>';
            } else {
                filtered.slice(0, 12).forEach(r => {
                    const btn = document.createElement('button');
                    btn.type = 'button';
                    btn.className = 'opa-suggest-item';
                    const mob = (r.mobileNumbers || []).join(', ') || '—';
                    btn.innerHTML = `<strong>${r.fullName}</strong><small class="d-block">${mob}</small>`;
                    btn.addEventListener('mousedown', e => {
                        e.preventDefault();
                        onPick(r);
                        hidePanel(panel);
                    });
                    panel.appendChild(btn);
                });
            }
            showPanel(panel);
        }

        const showName = () => showRecipients(namePanel, nameInput.value, pickRecipient);
        const showMobile = () => showRecipients(mobilePanel, mobileInput.value, pickRecipient);

        nameInput.addEventListener('focus', showName);
        nameInput.addEventListener('mouseenter', showName);
        nameInput.addEventListener('input', showName);

        mobileInput.addEventListener('focus', showMobile);
        mobileInput.addEventListener('mouseenter', showMobile);
        mobileInput.addEventListener('input', showMobile);

        document.addEventListener('click', e => {
            if (!nameInput.parentElement.contains(e.target)) hidePanel(namePanel);
            if (!mobileInput.parentElement.contains(e.target)) hidePanel(mobilePanel);
        });
    }

    return { initNameLookup, initMobilePickers };
})();
