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
            if (!el) return;
            if (el._weddingLeafletMap) {
                setTimeout(function () { el._weddingLeafletMap.invalidateSize(); }, 80);
                return;
            }
            if (el._leaflet_id) return;
            var lat = parseFloat(el.dataset.lat);
            var lng = parseFloat(el.dataset.lng);
            var name = el.dataset.name || '';
            var map = L.map(el, { zoomControl: true, scrollWheelZoom: false })
                       .setView([lat, lng], 16);
            L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                attribution: '© OpenStreetMap contributors', maxZoom: 19
            }).addTo(map);
            L.marker([lat, lng]).addTo(map).bindPopup(name).openPopup();
            el._weddingLeafletMap = map;
            setTimeout(function () { map.invalidateSize(); }, 80);
            setTimeout(function () { map.invalidateSize(); }, 350);
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

    initAdminPreviewDesignBridge: function (dotnetRef) {
        window.__wAdminPreviewDesignBridgeRef = dotnetRef;
        if (window.__wAdminPreviewDesignBridgeBound) return;
        window.__wAdminPreviewDesignBridgeBound = true;
        window.addEventListener('message', function (event) {
            var data = event.data || {};
            if (data.type !== 'wedding-design-drag') return;
            var ref = window.__wAdminPreviewDesignBridgeRef;
            if (!ref) return;
            ref.invokeMethodAsync(
                'OnPreviewElementMoved',
                data.target || '',
                Number(data.xPercent) || 0,
                Number(data.yPercent) || 0,
                data.viewport || 'desktop'
            ).catch(function () { });
        });
    },

    initDesignDragTargets: function () {
        var targets = document.querySelectorAll('[data-drag-target]');

        function viewportKind() {
            return document.documentElement.clientWidth <= 640 ? 'mobile' : 'desktop';
        }

        function eventPoint(e) {
            if (e.touches && e.touches.length) return e.touches[0];
            if (e.changedTouches && e.changedTouches.length) return e.changedTouches[0];
            return e;
        }

        function dragBounds(el) {
            var style = window.getComputedStyle(el);
            if (style.position === 'fixed') {
                return { left: 0, top: 0, width: window.innerWidth, height: window.innerHeight };
            }

            var container = el.closest('[data-drag-container]') || el.closest('.w-hero') || el.offsetParent || document.documentElement;
            if (container === document.documentElement || container === document.body) {
                return { left: 0, top: 0, width: window.innerWidth, height: window.innerHeight };
            }

            var rect = container.getBoundingClientRect();
            return { left: rect.left, top: rect.top, width: rect.width, height: rect.height };
        }

        function clampCenter(el, centerX, centerY, bounds) {
            var rect = el.getBoundingClientRect();
            var margin = 10;

            var minX = bounds.left + rect.width / 2 + margin;
            var maxX = bounds.left + bounds.width - rect.width / 2 - margin;
            var minY = bounds.top + rect.height / 2 + margin;
            var maxY = bounds.top + bounds.height - rect.height / 2 - margin;

            if (minX > maxX) minX = maxX = bounds.left + bounds.width / 2;
            if (minY > maxY) minY = maxY = bounds.top + bounds.height / 2;

            return {
                x: Math.max(minX, Math.min(maxX, centerX)),
                y: Math.max(minY, Math.min(maxY, centerY))
            };
        }

        function applyPos(el, centerX, centerY, viewport) {
            var bounds = dragBounds(el);
            var clamped = clampCenter(el, centerX, centerY, bounds);
            var xPct = bounds.width ? ((clamped.x - bounds.left) / bounds.width) * 100 : 50;
            var yPct = bounds.height ? ((clamped.y - bounds.top) / bounds.height) * 100 : 50;
            xPct = Math.max(0, Math.min(100, xPct));
            yPct = Math.max(0, Math.min(100, yPct));

            if ((viewport || viewportKind()) === 'mobile') {
                el.style.setProperty('--w-drag-mobile-x', xPct + '%');
                el.style.setProperty('--w-drag-mobile-y', yPct + '%');
            } else {
                el.style.setProperty('--w-drag-x', xPct + '%');
                el.style.setProperty('--w-drag-y', yPct + '%');
            }
            el.classList.add('w-draggable-positioned');
            return { xPercent: xPct, yPercent: yPct };
        }

        targets.forEach(function (el) {
            if (el.dataset.designDragBound) return;
            el.dataset.designDragBound = '1';
            el.classList.add('w-design-draggable');

            var dragging = false;
            var moved = false;
            var suppressClick = false;
            var startX = 0, startY = 0;
            var startCenterX = 0, startCenterY = 0;
            var lastPos = null;
            var dragViewport = 'desktop';

            function onDown(e) {
                if (e.button !== undefined && e.button !== 0) return;
                if (e.target && /^(INPUT|TEXTAREA|SELECT|AUDIO|VIDEO)$/i.test(e.target.tagName || '')) return;

                var t = eventPoint(e);
                var rect = el.getBoundingClientRect();
                dragging = true;
                moved = false;
                lastPos = null;
                startX = t.clientX;
                startY = t.clientY;
                startCenterX = rect.left + rect.width / 2;
                startCenterY = rect.top + rect.height / 2;
                dragViewport = viewportKind();
                el.classList.add('is-dragging');
                document.body.style.userSelect = 'none';

                if (e.pointerId !== undefined && el.setPointerCapture) {
                    try { el.setPointerCapture(e.pointerId); } catch (_) { }
                }
            }

            function onMove(e) {
                if (!dragging) return;
                var t = eventPoint(e);
                var dx = t.clientX - startX;
                var dy = t.clientY - startY;
                if (!moved && (Math.abs(dx) > 2 || Math.abs(dy) > 2)) moved = true;
                if (moved) {
                    lastPos = applyPos(el, startCenterX + dx, startCenterY + dy, dragViewport);
                    e.preventDefault && e.preventDefault();
                }
            }

            function onUp(e) {
                if (!dragging) return;
                dragging = false;
                el.classList.remove('is-dragging');
                document.body.style.userSelect = '';

                if (e && e.pointerId !== undefined && el.releasePointerCapture) {
                    try { el.releasePointerCapture(e.pointerId); } catch (_) { }
                }
                if (!moved) return;

                suppressClick = true;
                window.setTimeout(function () { suppressClick = false; }, 450);

                var pos = lastPos;
                if (!pos) {
                    var rect = el.getBoundingClientRect();
                    pos = applyPos(el, rect.left + rect.width / 2, rect.top + rect.height / 2, dragViewport);
                }

                if (window.parent && window.parent !== window) {
                    window.parent.postMessage({
                        type: 'wedding-design-drag',
                        target: el.dataset.dragTarget || '',
                        xPercent: pos.xPercent,
                        yPercent: pos.yPercent,
                        viewport: dragViewport
                    }, '*');
                }
            }

            el.addEventListener('click', function (ev) {
                if (!suppressClick) return;
                ev.stopPropagation();
                if (ev.stopImmediatePropagation) ev.stopImmediatePropagation();
                ev.preventDefault();
                suppressClick = false;
            }, true);

            if (el.classList.contains('w-draggable-positioned')) {
                window.requestAnimationFrame(function () {
                    var rect = el.getBoundingClientRect();
                    applyPos(el, rect.left + rect.width / 2, rect.top + rect.height / 2, viewportKind());
                });
            }

            el.addEventListener('pointerdown', onDown);
            el.addEventListener('pointermove', onMove);
            el.addEventListener('pointerup', onUp);
            el.addEventListener('pointercancel', onUp);
        });
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
            if (e.button !== undefined && e.button !== 0) return;
            var t = e;
            startX = t.clientX;
            startY = t.clientY;
            var rect = fab.getBoundingClientRect();
            startLeft = rect.left;
            startTop = rect.top;
            dragging = true;
            moved = false;
            fab.classList.add('is-dragging');
            if (e.pointerId !== undefined && fab.setPointerCapture) {
                try { fab.setPointerCapture(e.pointerId); } catch (_) { }
            }
            e.preventDefault && e.preventDefault();
        }

        function onMove(e) {
            if (!dragging) return;
            var t = e;
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
            if (e && e.pointerId !== undefined && fab.releasePointerCapture) {
                try { fab.releasePointerCapture(e.pointerId); } catch (_) { }
            }
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

        fab.addEventListener('pointerdown', onDown);
        fab.addEventListener('pointermove', onMove);
        fab.addEventListener('pointerup', onUp);
        fab.addEventListener('pointercancel', onUp);
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

    getSuperAdminSessionToken: function () {
        try { return sessionStorage.getItem('w-super-admin-token') || localStorage.getItem('w-super-admin-token') || ''; }
        catch (e) { return ''; }
    },

    setSuperAdminSessionToken: function (token) {
        try {
            if (token) {
                sessionStorage.setItem('w-super-admin-auth', '1');
                sessionStorage.setItem('w-super-admin-token', token);
                localStorage.setItem('w-super-admin-token', token);
            } else {
                sessionStorage.removeItem('w-super-admin-auth');
                sessionStorage.removeItem('w-super-admin-token');
                localStorage.removeItem('w-super-admin-token');
            }
        } catch (e) { }
    },

    setSuperAdminSession: function (authenticated) {
        try {
            if (authenticated) sessionStorage.setItem('w-super-admin-auth', '1');
            else {
                sessionStorage.removeItem('w-super-admin-auth');
                sessionStorage.removeItem('w-super-admin-token');
                localStorage.removeItem('w-super-admin-token');
            }
        } catch (e) { }
    }
};
