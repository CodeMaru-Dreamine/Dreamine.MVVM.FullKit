// Wedding Platform JS Interop — eval 없이 직접 함수 호출

window.weddingInterop = {

    applyTheme: function (themeName, variables) {
        var normalizedTheme = typeof themeName === 'string'
            && /^[a-z0-9-]{1,32}$/.test(themeName)
            ? themeName
            : 'rose';
        Array.from(document.body.classList)
            .filter(function (className) { return className.indexOf('w-theme-') === 0; })
            .forEach(function (className) { document.body.classList.remove(className); });
        document.body.classList.add('w-theme-' + normalizedTheme);

        var supportedVariables = [
            '--w-primary',
            '--w-dark',
            '--w-secondary',
            '--w-accent',
            '--w-text',
            '--w-muted-text',
            '--w-bg',
            '--w-panel-bg',
            '--w-border',
            '--w-button-bg',
            '--w-button-text',
            '--w-nav-bg',
            '--w-nav-text',
            '--w-shadow'
        ];
        supportedVariables.forEach(function (name) {
            document.body.style.removeProperty(name);
        });
        if (normalizedTheme === 'custom' && variables) {
            supportedVariables.forEach(function (name) {
                var value = variables[name];
                if (typeof value === 'string' && value.length <= 80) {
                    document.body.style.setProperty(name, value);
                }
            });
        }
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
     * PageTurn은 현재/target/양면 leaf를 먼저 렌더한 후 시작한다.
     * 실제 표시될 이미지가 decode되기 전에 3D 회전을 시작하면 종이색만
     * 잠깐 노출될 수 있으므로, 필요한 이미지를 eager로 전환하고 decode를
     * 기다린 다음 두 animation frame 뒤 Blazor에 제어를 돌려준다.
     */
    preparePageTurn: function (viewport) {
        if (!viewport) return Promise.resolve();

        var images = Array.from(viewport.querySelectorAll('img'));
        var readiness = images.map(function (img) {
            try {
                img.loading = 'eager';
                img.setAttribute('loading', 'eager');
            } catch (_) { }

            if (img.complete && img.naturalWidth > 0) {
                if (typeof img.decode === 'function') {
                    return img.decode().catch(function () { });
                }
                return Promise.resolve();
            }

            return new Promise(function (resolve) {
                var settled = false;
                var finish = function () {
                    if (settled) return;
                    settled = true;
                    img.removeEventListener('load', finish);
                    img.removeEventListener('error', finish);
                    if (typeof img.decode === 'function' && img.naturalWidth > 0) {
                        img.decode().catch(function () { }).then(resolve);
                    } else {
                        resolve();
                    }
                };
                img.addEventListener('load', finish, { once: true });
                img.addEventListener('error', finish, { once: true });
            });
        });

        // 깨진 네트워크 응답이 전환 잠금을 영구히 붙잡지 않도록 준비 단계만 제한한다.
        var timeout = new Promise(function (resolve) {
            window.setTimeout(resolve, 2200);
        });

        return Promise.race([
            Promise.all(readiness),
            timeout
        ]).then(function () {
            return new Promise(function (resolve) {
                window.requestAnimationFrame(function () {
                    window.requestAnimationFrame(resolve);
                });
            });
        });
    },

    /**
     * PagedBook은 CSS로 숨긴 PC/모바일 트리를 동시에 유지하지 않는다.
     * 실제 reader 폭을 관찰해 Blazor가 데스크톱 펼침면(2쪽) 또는
     * 모바일 단일면(1쪽) 중 하나만 렌더하도록 알려준다.
     */
    observePageTurnViewport: function (viewport, dotnetRef, observeResponsiveBook) {
        if (!viewport || !dotnetRef) return;

        if (viewport._weddingPageTurnObserver) {
            viewport._weddingPageTurnObserver.disconnect();
            delete viewport._weddingPageTurnObserver;
        }
        if (viewport._weddingHorizontalWheelHandler) {
            viewport.removeEventListener(
                'wheel',
                viewport._weddingHorizontalWheelHandler,
                false
            );
            delete viewport._weddingHorizontalWheelHandler;
        }
        if (viewport._weddingTouchSwipeHandlers) {
            var previousSwipe = viewport._weddingTouchSwipeHandlers;
            viewport.removeEventListener(
                'pointerdown',
                previousSwipe.pointerDown,
                true
            );
            viewport.removeEventListener(
                'pointerup',
                previousSwipe.pointerUp,
                true
            );
            viewport.removeEventListener(
                'pointercancel',
                previousSwipe.pointerCancel,
                true
            );
            viewport.removeEventListener(
                'click',
                previousSwipe.click,
                true
            );
            delete viewport._weddingTouchSwipeHandlers;
        }

        /*
         * compact 상태의 viewport 자체는 CSS에 의해 34rem으로 줄어든다.
         * 그 폭을 다시 측정하면 넓은 PC에서도 영원히 compact로 남으므로,
         * 폭 제약을 받지 않는 바깥 transition root를 관찰한다.
         */
        if (observeResponsiveBook) {
            var layoutHost = viewport.parentElement || viewport;
            var lastCompact = null;
            var notify = function () {
                var compact =
                    layoutHost.getBoundingClientRect().width <= 719;
                if (compact === lastCompact) return;
                lastCompact = compact;
                dotnetRef.invokeMethodAsync(
                    'OnPageTurnViewportModeChanged',
                    compact
                ).catch(function () { });
            };

            var observer = new ResizeObserver(notify);
            observer.observe(layoutHost);
            viewport._weddingPageTurnObserver = observer;
            notify();
        }

        /*
         * 세로 wheel은 카드/책 내부 스크롤에 그대로 맡기고 실제 가로
         * 트랙패드 제스처 또는 Shift+wheel만 페이지 이동으로 소비한다.
         * momentum 이벤트가 한 번의 손짓으로 여러 장을 넘기지 않도록
         * 브라우저 쪽에서 transition 시간보다 길게 잠근다.
         */
        var accumulatedWheelX = 0;
        var wheelBlockedUntil = 0;
        var wheelHandler = function (event) {
            var horizontalDelta =
                Math.abs(event.deltaX) > Math.abs(event.deltaY) * 1.05
                    ? event.deltaX
                    : event.shiftKey
                        ? event.deltaY
                        : 0;
            if (Math.abs(horizontalDelta) < 1) return;

            event.preventDefault();
            if (Date.now() < wheelBlockedUntil) return;

            accumulatedWheelX += horizontalDelta;
            if (Math.abs(accumulatedWheelX) < 48) return;

            var direction = accumulatedWheelX > 0 ? 1 : -1;
            accumulatedWheelX = 0;
            wheelBlockedUntil = Date.now() + 900;
            dotnetRef.invokeMethodAsync(
                'OnPageTurnHorizontalWheel',
                direction
            ).catch(function () { });
        };

        viewport.addEventListener('wheel', wheelHandler, { passive: false });
        viewport._weddingHorizontalWheelHandler = wheelHandler;

        /*
         * 모바일에서는 페이지 내부의 button/section이 pointer 이벤트
         * bubbling을 막을 수 있다. capture 단계에서 손가락 시작/끝을
         * 관찰해 실제 수평 스와이프만 페이지 이동으로 소비한다.
         * 입력창·지도·영상·히어로 위치 편집은 고유 제스처를 보존한다.
         */
        var touchStart = null;
        var touchSwipeBlockedUntil = 0;
        var suppressClickUntil = 0;
        var shouldIgnoreTouchSwipe = function (target) {
            return target instanceof Element
                && target.closest(
                    'input,textarea,select,video,[contenteditable="true"],'
                    + '.leaflet-container,.w-design-draggable'
                ) !== null;
        };
        var pointerDown = function (event) {
            if (event.pointerType !== 'touch'
                || shouldIgnoreTouchSwipe(event.target)) {
                touchStart = null;
                return;
            }

            touchStart = {
                id: event.pointerId,
                x: event.clientX,
                y: event.clientY
            };
        };
        var pointerUp = function (event) {
            if (!touchStart
                || event.pointerType !== 'touch'
                || event.pointerId !== touchStart.id) {
                return;
            }

            var deltaX = event.clientX - touchStart.x;
            var deltaY = event.clientY - touchStart.y;
            touchStart = null;
            if (Math.abs(deltaX) < 48
                || Math.abs(deltaX) <= Math.abs(deltaY) * 1.2
                || Date.now() < touchSwipeBlockedUntil) {
                return;
            }

            event.preventDefault();
            event.stopPropagation();
            event.stopImmediatePropagation();
            touchSwipeBlockedUntil = Date.now() + 850;
            suppressClickUntil = Date.now() + 600;
            dotnetRef.invokeMethodAsync(
                'OnPageTurnHorizontalWheel',
                deltaX < 0 ? 1 : -1
            ).catch(function () { });
        };
        var pointerCancel = function (event) {
            if (touchStart && event.pointerId === touchStart.id) {
                touchStart = null;
            }
        };
        var click = function (event) {
            if (Date.now() >= suppressClickUntil) return;
            event.preventDefault();
            event.stopPropagation();
            event.stopImmediatePropagation();
        };

        viewport.addEventListener('pointerdown', pointerDown, {
            capture: true,
            passive: true
        });
        viewport.addEventListener('pointerup', pointerUp, {
            capture: true,
            passive: false
        });
        viewport.addEventListener('pointercancel', pointerCancel, {
            capture: true,
            passive: true
        });
        viewport.addEventListener('click', click, {
            capture: true,
            passive: false
        });
        viewport._weddingTouchSwipeHandlers = {
            pointerDown: pointerDown,
            pointerUp: pointerUp,
            pointerCancel: pointerCancel,
            click: click
        };
    },

    disposePageTurnViewport: function (viewport) {
        if (!viewport) return;
        if (viewport._weddingPageTurnObserver) {
            viewport._weddingPageTurnObserver.disconnect();
            delete viewport._weddingPageTurnObserver;
        }
        if (viewport._weddingHorizontalWheelHandler) {
            viewport.removeEventListener(
                'wheel',
                viewport._weddingHorizontalWheelHandler,
                false
            );
            delete viewport._weddingHorizontalWheelHandler;
        }
        if (viewport._weddingTouchSwipeHandlers) {
            var swipe = viewport._weddingTouchSwipeHandlers;
            viewport.removeEventListener(
                'pointerdown',
                swipe.pointerDown,
                true
            );
            viewport.removeEventListener(
                'pointerup',
                swipe.pointerUp,
                true
            );
            viewport.removeEventListener(
                'pointercancel',
                swipe.pointerCancel,
                true
            );
            viewport.removeEventListener(
                'click',
                swipe.click,
                true
            );
            delete viewport._weddingTouchSwipeHandlers;
        }
    },

    getPageTurnNavigationStep: function (viewport) {
        if (!viewport) return 1;
        var layoutHost = viewport.parentElement || viewport;
        return layoutHost.getBoundingClientRect().width <= 719 ? 1 : 2;
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

    openSafePhone: function (phone) {
        var normalized = String(phone || '').replace(/[^0-9+]/g, '').slice(0, 24);
        if (normalized) window.location.href = 'tel:' + normalized;
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
            var lat = Number.parseFloat(el.dataset.lat);
            var lng = Number.parseFloat(el.dataset.lng);
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
            var saved = Number.parseInt(localStorage.getItem(STORAGE_KEY), 10);
            if (!Number.isNaN(saved) && saved >= MIN) {
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
            startWidth = Number.parseInt(current, 10) || 420;
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
                var w = Number.parseInt(current, 10);
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
            try { f.contentWindow.location.reload(); } catch (e) {
                var src = f.getAttribute('src');
                if (src) f.setAttribute('src', src);
            }
        }
    },

    initAdminPreviewDesignBridge: function (dotnetRef) {
        window.__wAdminPreviewDesignBridgeRef = dotnetRef;
        if (window.__wAdminPreviewDesignBridgeBound) return;
        window.__wAdminPreviewDesignBridgeBound = true;
        window.addEventListener('message', function (event) {
            if (event.origin !== window.location.origin) return;
            var previewFrame = document.getElementById('w-preview-iframe');
            if (!previewFrame || event.source !== previewFrame.contentWindow) return;
            var data = event.data || {};
            if (data.type === 'wedding-admin-preview-ready') {
                window.weddingInterop.sendAdminPreviewAccountDraft();
                return;
            }
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

    updateAdminPreviewAccountDraft: function (accounts) {
        var values = Array.isArray(accounts) ? accounts : [];
        function bounded(value, maxLength) {
            return String(value || '').slice(0, maxLength);
        }
        window.__wAdminPreviewAccountDraft = values.slice(0, 8).map(function (account) {
            account = account || {};
            return {
                label: bounded(account.label, 80),
                name: bounded(account.name, 120),
                phone: bounded(account.phone, 32),
                bankName: bounded(account.bankName, 80),
                account: bounded(account.account, 100),
                accountHolder: bounded(account.accountHolder, 120),
                kakaoPayUrl: bounded(account.kakaoPayUrl, 2048)
            };
        });
        window.weddingInterop.sendAdminPreviewAccountDraft();
    },

    sendAdminPreviewAccountDraft: function () {
        var previewFrame = document.getElementById('w-preview-iframe');
        if (!previewFrame || !previewFrame.contentWindow) return;
        if (!Array.isArray(window.__wAdminPreviewAccountDraft)) return;
        previewFrame.contentWindow.postMessage({
            type: 'wedding-admin-preview-accounts',
            accounts: window.__wAdminPreviewAccountDraft
        }, window.location.origin);
    },

    initAdminPreviewDraftReceiver: function (dotnetRef) {
        window.__wAdminPreviewDraftReceiverRef = dotnetRef;
        if (!window.__wAdminPreviewDraftReceiverBound) {
            window.__wAdminPreviewDraftReceiverBound = true;
            window.addEventListener('message', function (event) {
                if (event.origin !== window.location.origin
                    || event.source !== window.parent) return;
                var data = event.data || {};
                if (data.type !== 'wedding-admin-preview-accounts') return;
                var ref = window.__wAdminPreviewDraftReceiverRef;
                if (!ref) return;
                ref.invokeMethodAsync(
                    'OnAdminPreviewAccountsChanged',
                    Array.isArray(data.accounts) ? data.accounts : []
                ).catch(function () { });
            });
        }

        if (window.parent && window.parent !== window) {
            window.parent.postMessage({
                type: 'wedding-admin-preview-ready'
            }, window.location.origin);
        }
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
                    }, window.location.origin);
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
     * 관리자 히어로 크롭 편집기.
     * 원본 이미지 위의 선택 사각형은 PC/폰 목표 화면 비율을 유지하며
     * 그리기, 이동, 네 모서리 크기 조절을 지원한다.
     */
    initHeroCropEditor: function (dotnetRef) {
        var stage = document.querySelector('[data-hero-crop-editor-stage]');
        if (!stage || stage.dataset.cropEditorBound) return;
        var image = stage.querySelector('img');
        var selection = stage.querySelector('[data-crop-selection]');
        if (!image || !selection) return;
        stage.dataset.cropEditorBound = '1';

        var aspect = Math.max(0.1, Number(stage.dataset.aspect) || (16 / 9));
        var crop = {
            x: Number(stage.dataset.cropX) || 0,
            y: Number(stage.dataset.cropY) || 0,
            width: Number(stage.dataset.cropWidth) || 100,
            height: Number(stage.dataset.cropHeight) || 100
        };
        var operation = null;
        var pointerId = null;
        var minSize = 28;

        function imageBounds() {
            var stageRect = stage.getBoundingClientRect();
            var imageRect = image.getBoundingClientRect();
            return {
                left: imageRect.left - stageRect.left,
                top: imageRect.top - stageRect.top,
                width: imageRect.width,
                height: imageRect.height
            };
        }

        function clampRect(rect, bounds) {
            rect.width = Math.max(minSize, Math.min(bounds.width, rect.width));
            rect.height = rect.width / aspect;
            if (rect.height > bounds.height) {
                rect.height = bounds.height;
                rect.width = rect.height * aspect;
            }
            rect.x = Math.max(0, Math.min(bounds.width - rect.width, rect.x));
            rect.y = Math.max(0, Math.min(bounds.height - rect.height, rect.y));
            return rect;
        }

        function conformInitialRect(bounds) {
            var rect = {
                x: bounds.width * crop.x / 100,
                y: bounds.height * crop.y / 100,
                width: bounds.width * crop.width / 100,
                height: bounds.height * crop.height / 100
            };
            var centerX = rect.x + rect.width / 2;
            var centerY = rect.y + rect.height / 2;
            if (rect.width / Math.max(1, rect.height) > aspect) {
                rect.width = rect.height * aspect;
            } else {
                rect.height = rect.width / aspect;
            }
            rect.x = centerX - rect.width / 2;
            rect.y = centerY - rect.height / 2;
            return clampRect(rect, bounds);
        }

        function renderRect(rect) {
            var bounds = imageBounds();
            selection.style.left = (bounds.left + rect.x) + 'px';
            selection.style.top = (bounds.top + rect.y) + 'px';
            selection.style.width = rect.width + 'px';
            selection.style.height = rect.height + 'px';
        }

        function rectFromSelection() {
            var bounds = imageBounds();
            var rect = selection.getBoundingClientRect();
            var stageRect = stage.getBoundingClientRect();
            return {
                x: rect.left - stageRect.left - bounds.left,
                y: rect.top - stageRect.top - bounds.top,
                width: rect.width,
                height: rect.height
            };
        }

        function localPoint(event, bounds) {
            var stageRect = stage.getBoundingClientRect();
            return {
                x: Math.max(0, Math.min(bounds.width, event.clientX - stageRect.left - bounds.left)),
                y: Math.max(0, Math.min(bounds.height, event.clientY - stageRect.top - bounds.top))
            };
        }

        function rectFromAnchor(anchor, point, signX, signY, bounds) {
            var width = Math.max(minSize, Math.abs(point.x - anchor.x));
            var height = Math.max(minSize, Math.abs(point.y - anchor.y));
            if (width / height > aspect) height = width / aspect;
            else width = height * aspect;

            var maxWidth = signX > 0 ? bounds.width - anchor.x : anchor.x;
            var maxHeight = signY > 0 ? bounds.height - anchor.y : anchor.y;
            var scale = Math.min(1, maxWidth / width, maxHeight / height);
            width *= Math.max(0.01, scale);
            height *= Math.max(0.01, scale);
            if (width < minSize || height < minSize) {
                width = Math.min(maxWidth, Math.max(minSize, minSize * aspect));
                height = width / aspect;
                if (height > maxHeight) {
                    height = maxHeight;
                    width = height * aspect;
                }
            }

            return clampRect({
                x: signX > 0 ? anchor.x : anchor.x - width,
                y: signY > 0 ? anchor.y : anchor.y - height,
                width: width,
                height: height
            }, bounds);
        }

        function notify(rect) {
            var bounds = imageBounds();
            if (!bounds.width || !bounds.height) return;
            crop = {
                x: Math.max(0, Math.min(100, rect.x / bounds.width * 100)),
                y: Math.max(0, Math.min(100, rect.y / bounds.height * 100)),
                width: Math.max(0, Math.min(100, rect.width / bounds.width * 100)),
                height: Math.max(0, Math.min(100, rect.height / bounds.height * 100))
            };
            dotnetRef.invokeMethodAsync(
                'OnHeroCropDraftChanged',
                stage.dataset.viewport || 'mobile',
                crop.x,
                crop.y,
                crop.width,
                crop.height
            ).catch(function () { });
        }

        function onPointerDown(event) {
            if (event.button !== undefined && event.button !== 0) return;
            var bounds = imageBounds();
            if (!bounds.width || !bounds.height) return;
            var point = localPoint(event, bounds);
            var handle = event.target && event.target.dataset
                ? event.target.dataset.cropHandle
                : null;
            var current = rectFromSelection();

            if (handle) {
                var east = handle.indexOf('e') >= 0;
                var south = handle.indexOf('s') >= 0;
                operation = {
                    type: 'resize',
                    anchor: {
                        x: east ? current.x : current.x + current.width,
                        y: south ? current.y : current.y + current.height
                    },
                    signX: east ? 1 : -1,
                    signY: south ? 1 : -1
                };
            } else if (event.target === selection || selection.contains(event.target)) {
                operation = {
                    type: 'move',
                    start: point,
                    rect: current
                };
            } else {
                operation = {
                    type: 'draw',
                    anchor: point,
                    signX: point.x <= bounds.width / 2 ? 1 : -1,
                    signY: point.y <= bounds.height / 2 ? 1 : -1
                };
                renderRect(rectFromAnchor(
                    point,
                    { x: point.x + operation.signX * minSize * aspect, y: point.y + operation.signY * minSize },
                    operation.signX,
                    operation.signY,
                    bounds));
            }

            pointerId = event.pointerId;
            if (stage.setPointerCapture && pointerId !== undefined) {
                try { stage.setPointerCapture(pointerId); } catch (_) { }
            }
            event.preventDefault();
        }

        function onPointerMove(event) {
            if (!operation) return;
            var bounds = imageBounds();
            var point = localPoint(event, bounds);
            var next;
            if (operation.type === 'move') {
                next = clampRect({
                    x: operation.rect.x + point.x - operation.start.x,
                    y: operation.rect.y + point.y - operation.start.y,
                    width: operation.rect.width,
                    height: operation.rect.height
                }, bounds);
            } else {
                next = rectFromAnchor(
                    operation.anchor,
                    point,
                    operation.signX,
                    operation.signY,
                    bounds);
            }
            renderRect(next);
            event.preventDefault();
        }

        function onPointerUp(event) {
            if (!operation) return;
            operation = null;
            var rect = clampRect(rectFromSelection(), imageBounds());
            renderRect(rect);
            notify(rect);
            if (stage.releasePointerCapture && pointerId !== null) {
                try { stage.releasePointerCapture(pointerId); } catch (_) { }
            }
            pointerId = null;
            event && event.preventDefault && event.preventDefault();
        }

        function initialize() {
            var bounds = imageBounds();
            if (!bounds.width || !bounds.height) return;
            var initial = conformInitialRect(bounds);
            renderRect(initial);
            notify(initial);
        }

        stage.addEventListener('pointerdown', onPointerDown);
        stage.addEventListener('pointermove', onPointerMove);
        stage.addEventListener('pointerup', onPointerUp);
        stage.addEventListener('pointercancel', onPointerUp);
        if (image.complete) window.requestAnimationFrame(initialize);
        else image.addEventListener('load', initialize, { once: true });
    },

    /**
     * 공개 히어로에 저장된 정규화 크롭 사각형을 적용한다.
     * 선택 영역을 하나의 가상 이미지로 보고 현재 레이아웃 영역에 cover 하므로
     * 레이아웃 비율이 조금 달라도 사용자가 고른 영역 밖으로 벗어나지 않는다.
     */
    initHeroCropTargets: function () {
        function numberVar(style, name, fallback) {
            var value = Number.parseFloat(style.getPropertyValue(name));
            return Number.isFinite(value) ? value : fallback;
        }

        function currentViewport() {
            return document.documentElement.clientWidth <= 640 ? 'mobile' : 'desktop';
        }

        function cropState(target) {
            var style = getComputedStyle(target);
            var viewport = currentViewport();
            return {
                enabled: numberVar(style, '--w-hero-image-crop-' + viewport + '-enabled', 0) >= 0.5,
                x: numberVar(style, '--w-hero-image-crop-' + viewport + '-x', 0),
                y: numberVar(style, '--w-hero-image-crop-' + viewport + '-y', 0),
                width: numberVar(style, '--w-hero-image-crop-' + viewport + '-width', 100),
                height: numberVar(style, '--w-hero-image-crop-' + viewport + '-height', 100)
            };
        }

        function containerFor(target) {
            return target.closest('[data-hero-crop-container]') || target.parentElement;
        }

        function geometry(target, naturalWidth, naturalHeight) {
            var container = containerFor(target);
            if (!container || !naturalWidth || !naturalHeight) return null;
            var width = container.clientWidth;
            var height = container.clientHeight;
            if (!width || !height) return null;
            var crop = cropState(target);
            if (!crop.enabled) return { crop: crop, container: container, full: true };

            var cropWidth = naturalWidth * Math.max(0.05, crop.width / 100);
            var cropHeight = naturalHeight * Math.max(0.05, crop.height / 100);
            var cropX = naturalWidth * Math.max(0, crop.x / 100);
            var cropY = naturalHeight * Math.max(0, crop.y / 100);
            var scale = Math.max(width / cropWidth, height / cropHeight);
            return {
                crop: crop,
                container: container,
                full: false,
                width: naturalWidth * scale,
                height: naturalHeight * scale,
                left: -cropX * scale + (width - cropWidth * scale) / 2,
                top: -cropY * scale + (height - cropHeight * scale) / 2
            };
        }

        function resetImage(target) {
            ['position', 'inset', 'left', 'top', 'right', 'bottom', 'width', 'height',
             'maxWidth', 'maxHeight', 'objectFit', 'objectPosition', 'transform']
                .forEach(function (name) { target.style[name] = ''; });
        }

        function applyImage(target) {
            if (!target.complete || !target.naturalWidth) return;
            var result = geometry(target, target.naturalWidth, target.naturalHeight);
            if (!result) return;
            if (result.full) {
                resetImage(target);
                return;
            }

            target.style.position = 'absolute';
            target.style.inset = 'auto';
            target.style.left = result.left + 'px';
            target.style.top = result.top + 'px';
            target.style.right = 'auto';
            target.style.bottom = 'auto';
            target.style.width = result.width + 'px';
            target.style.height = result.height + 'px';
            target.style.maxWidth = 'none';
            target.style.maxHeight = 'none';
            target.style.objectFit = 'fill';
            target.style.objectPosition = '0 0';
            target.style.transform = 'none';
        }

        function applyBackground(target) {
            var source = target.dataset.heroCropSource;
            if (!source) return;
            var loader = target._wHeroCropLoader;
            if (!loader || target._wHeroCropSource !== source) {
                loader = new Image();
                loader.src = source;
                target._wHeroCropLoader = loader;
                target._wHeroCropSource = source;
                loader.addEventListener('load', function () { applyBackground(target); });
            }
            if (!loader.complete || !loader.naturalWidth) return;
            var result = geometry(target, loader.naturalWidth, loader.naturalHeight);
            if (!result) return;
            if (result.full) {
                target.style.backgroundSize = '';
                target.style.backgroundPosition = '';
                return;
            }
            target.style.backgroundSize = result.width + 'px ' + result.height + 'px';
            target.style.backgroundPosition = result.left + 'px ' + result.top + 'px';
            target.style.backgroundRepeat = 'no-repeat';
        }

        function applyAll() {
            document.querySelectorAll('[data-hero-crop-image]').forEach(function (target) {
                if (!target.dataset.heroCropLoadBound) {
                    target.dataset.heroCropLoadBound = '1';
                    target.addEventListener('load', function () { applyImage(target); });
                }
                applyImage(target);
            });
            document.querySelectorAll('[data-hero-crop-background]').forEach(applyBackground);
        }

        window.__wHeroCropApplyAll = applyAll;
        if (!window.__wHeroCropResizeBound) {
            window.__wHeroCropResizeBound = true;
            var resizeTimer = 0;
            window.addEventListener('resize', function () {
                clearTimeout(resizeTimer);
                resizeTimer = setTimeout(function () {
                    window.__wHeroCropApplyAll && window.__wHeroCropApplyAll();
                }, 80);
            });
        }
        applyAll();
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
                        x: Number.parseFloat(fab.style.left) || 0,
                        y: Number.parseFloat(fab.style.top) || 0
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
