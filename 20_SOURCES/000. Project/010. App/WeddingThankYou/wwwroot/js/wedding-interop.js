// Wedding Platform JS Interop — eval 없이 직접 함수 호출

window.weddingInterop = {

    applyTheme: function (themeName) {
        document.body.className = document.body.className
            .replace(/w-theme-\S+/g, '').trim();
        if (themeName) document.body.classList.add('w-theme-' + themeName);
    },

    initMusicAutoplay: function () {
        var played = false;
        function tryPlay() {
            if (played) return;
            var a = document.querySelector('audio[loop]');
            if (!a) return;
            a.play().then(function () { played = true; }).catch(function () { });
        }
        document.addEventListener('click', tryPlay);
        document.addEventListener('touchstart', tryPlay);
    },

    /**
     * 음악 버튼/배경 클릭/브라우저 자체 컨트롤 등 어떤 경로로 재생·정지되든
     * audio 엘리먼트의 실제 play/pause 이벤트를 그대로 Blazor에 알려서
     * 아이콘 상태(🎵/⏸)가 항상 실제 재생 상태와 일치하도록 동기화합니다.
     */
    initMusicSync: function (dotnetRef) {
        var a = document.querySelector('audio[loop]');
        if (!a || a.dataset.syncBound) return;
        a.dataset.syncBound = '1';
        a.addEventListener('play', function () { dotnetRef.invokeMethodAsync('OnMusicStateChanged', true); });
        a.addEventListener('pause', function () { dotnetRef.invokeMethodAsync('OnMusicStateChanged', false); });
    },

    playMusic: function () {
        var a = document.querySelector('audio[loop]');
        if (!a) return Promise.resolve(false);
        return a.play().then(function () { return true; }).catch(function () { return !a.paused; });
    },

    pauseMusic: function () {
        var a = document.querySelector('audio[loop]');
        if (a) a.pause();
        return Promise.resolve(false);
    },

    isMusicPlaying: function () {
        var a = document.querySelector('audio[loop]');
        return !!(a && !a.paused && !a.ended);
    },

    scrollToElement: function (id) {
        var el = document.getElementById(id);
        if (el) el.scrollIntoView({ behavior: 'smooth' });
    },

    copyToClipboard: function (text) {
        if (navigator.clipboard) {
            navigator.clipboard.writeText(text).catch(function () { });
        }
    },

    shareOrCopy: function (url, title) {
        if (navigator.share) {
            return navigator.share({ title: title || document.title, url: url })
                .then(function () { return 'shared'; })
                .catch(function () {
                    return window.weddingInterop.copyTextFallback(url);
                });
        }
        return window.weddingInterop.copyTextFallback(url);
    },

    copyTextFallback: function (text) {
        if (navigator.clipboard) {
            return navigator.clipboard.writeText(text)
                .then(function () { return 'copied'; })
                .catch(function () { return 'failed'; });
        }
        return Promise.resolve('failed');
    },

    initLeafletMap: function () {
        function tryInit() {
            if (typeof L === 'undefined') { setTimeout(tryInit, 300); return; }
            var el = document.getElementById('w-leaflet-map');
            if (!el || el._leaflet_id) return;
            var lat = parseFloat(el.dataset.lat);
            var lng = parseFloat(el.dataset.lng);
            var name = el.dataset.name || '';
            var map = L.map(el, { zoomControl: true, scrollWheelZoom: false })
                       .setView([lat, lng], 16);
            L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                attribution: '© OpenStreetMap contributors', maxZoom: 19
            }).addTo(map);
            L.marker([lat, lng]).addTo(map).bindPopup(name).openPopup();
        }
        tryInit();
    },

    /**
     * 어드민 3컬럼 셸의 프리뷰 컬럼 폭을 드래그로 조정.
     * 폭은 localStorage 에 저장되어 다음 접속 시 복원됨.
     */
    initAdminSplitter: function () {
        var shell = document.querySelector('.w-admin-shell');
        var splitter = document.querySelector('.w-admin-splitter');
        if (!shell || !splitter || splitter.dataset.bound) return;
        splitter.dataset.bound = '1';

        var STORAGE_KEY = 'w-admin-preview-width';
        var MIN = 320;
        var MAX_RATIO = 0.7; // 뷰포트 폭의 70%까지

        // 저장된 폭 복원
        try {
            var saved = parseInt(localStorage.getItem(STORAGE_KEY), 10);
            if (!isNaN(saved) && saved >= MIN) {
                shell.style.setProperty('--w-preview-width', saved + 'px');
            }
        } catch (e) { }

        var dragging = false;
        var startX = 0;
        var startWidth = 0;

        function onDown(e) {
            dragging = true;
            startX = (e.touches ? e.touches[0].clientX : e.clientX);
            var current = getComputedStyle(shell).getPropertyValue('--w-preview-width');
            startWidth = parseInt(current, 10) || 420;
            document.body.style.cursor = 'col-resize';
            document.body.style.userSelect = 'none';
            e.preventDefault();
        }

        function onMove(e) {
            if (!dragging) return;
            var x = (e.touches ? e.touches[0].clientX : e.clientX);
            var delta = startX - x; // 오른쪽으로 드래그하면 폭이 줄어들도록
            var next = startWidth + delta;
            var max = Math.floor(window.innerWidth * MAX_RATIO);
            if (next < MIN) next = MIN;
            if (next > max) next = max;
            shell.style.setProperty('--w-preview-width', next + 'px');
        }

        function onUp() {
            if (!dragging) return;
            dragging = false;
            document.body.style.cursor = '';
            document.body.style.userSelect = '';
            try {
                var current = getComputedStyle(shell).getPropertyValue('--w-preview-width');
                var w = parseInt(current, 10);
                if (w) localStorage.setItem(STORAGE_KEY, w);
            } catch (e) { }
        }

        splitter.addEventListener('mousedown', onDown);
        splitter.addEventListener('touchstart', onDown, { passive: false });
        document.addEventListener('mousemove', onMove);
        document.addEventListener('touchmove', onMove, { passive: false });
        document.addEventListener('mouseup', onUp);
        document.addEventListener('touchend', onUp);

        // 더블클릭으로 기본값(420px)으로 리셋
        splitter.addEventListener('dblclick', function () {
            shell.style.setProperty('--w-preview-width', '420px');
            try { localStorage.removeItem(STORAGE_KEY); } catch (e) { }
        });
    },

    /** 프리뷰 iframe 강제 새로고침 */
    reloadPreviewIframe: function () {
        var f = document.getElementById('w-preview-iframe');
        if (f && f.contentWindow) {
            try { f.contentWindow.location.reload(); } catch (e) { f.src = f.src; }
        }
    },

    /**
     * 청첩장 폰용 햄버거 FAB 드래그 이동.
     * 위치는 localStorage 에 저장되고 다음 접속 시 복원됨.
     * 5px 이상 이동하면 드래그로 간주해 클릭 이벤트 억제.
     */
    initInviteMenuFab: function () {
        var fab = document.querySelector('.w-invite-menu-fab');
        if (!fab || fab.dataset.dragBound) return;
        fab.dataset.dragBound = '1';

        var STORAGE_KEY = 'w-invite-fab-pos';

        function applyPos(p) {
            fab.style.left = p.x + 'px';
            fab.style.top = p.y + 'px';
            fab.style.right = 'auto';
            fab.style.bottom = 'auto';
        }

        // 저장된 위치 복원
        try {
            var raw = localStorage.getItem(STORAGE_KEY);
            if (raw) {
                var pos = JSON.parse(raw);
                if (pos && typeof pos.x === 'number' && typeof pos.y === 'number') {
                    // 뷰포트 변화 대응: 화면 밖이면 안쪽으로 clamp
                    var w = fab.offsetWidth || 56;
                    var h = fab.offsetHeight || 56;
                    pos.x = Math.max(8, Math.min(window.innerWidth - w - 8, pos.x));
                    pos.y = Math.max(8, Math.min(window.innerHeight - h - 8, pos.y));
                    applyPos(pos);
                }
            }
        } catch (e) { }

        var dragging = false;
        var moved = false;
        var startX = 0, startY = 0;
        var startLeft = 0, startTop = 0;

        function onDown(e) {
            var t = e.touches ? e.touches[0] : e;
            startX = t.clientX;
            startY = t.clientY;
            var rect = fab.getBoundingClientRect();
            startLeft = rect.left;
            startTop = rect.top;
            dragging = true;
            moved = false;
            fab.classList.add('is-dragging');
        }

        function onMove(e) {
            if (!dragging) return;
            var t = e.touches ? e.touches[0] : e;
            var dx = t.clientX - startX;
            var dy = t.clientY - startY;
            if (!moved && (Math.abs(dx) > 5 || Math.abs(dy) > 5)) moved = true;
            if (moved) {
                var w = fab.offsetWidth;
                var h = fab.offsetHeight;
                var nx = Math.max(8, Math.min(window.innerWidth - w - 8, startLeft + dx));
                var ny = Math.max(8, Math.min(window.innerHeight - h - 8, startTop + dy));
                applyPos({ x: nx, y: ny });
                e.preventDefault && e.preventDefault();
            }
        }

        function onUp(e) {
            if (!dragging) return;
            dragging = false;
            fab.classList.remove('is-dragging');
            if (moved) {
                try {
                    localStorage.setItem(STORAGE_KEY, JSON.stringify({
                        x: parseFloat(fab.style.left) || 0,
                        y: parseFloat(fab.style.top) || 0
                    }));
                } catch (e) { }
                // 드래그였으므로 뒤이어 발생할 click 이벤트 억제
                var suppress = function (ev) {
                    ev.stopPropagation();
                    ev.preventDefault();
                    fab.removeEventListener('click', suppress, true);
                };
                fab.addEventListener('click', suppress, true);
            }
        }

        fab.addEventListener('mousedown', onDown);
        fab.addEventListener('touchstart', onDown, { passive: false });
        document.addEventListener('mousemove', onMove);
        document.addEventListener('touchmove', onMove, { passive: false });
        document.addEventListener('mouseup', onUp);
        document.addEventListener('touchend', onUp);
    },

    /** 데스크톱 프리뷰 접힘 상태 저장 (localStorage) */
    getPreviewCollapsed: function () {
        try { return localStorage.getItem('w-admin-preview-collapsed') === '1'; }
        catch (e) { return false; }
    },
    setPreviewCollapsed: function (collapsed) {
        try {
            if (collapsed) localStorage.setItem('w-admin-preview-collapsed', '1');
            else localStorage.removeItem('w-admin-preview-collapsed');
        } catch (e) { }
    },

    getSuperAdminSession: function () {
        try { return sessionStorage.getItem('w-super-admin-auth') === '1'; }
        catch (e) { return false; }
    },

    setSuperAdminSession: function (authenticated) {
        try {
            if (authenticated) sessionStorage.setItem('w-super-admin-auth', '1');
            else sessionStorage.removeItem('w-super-admin-auth');
        } catch (e) { }
    }
};
