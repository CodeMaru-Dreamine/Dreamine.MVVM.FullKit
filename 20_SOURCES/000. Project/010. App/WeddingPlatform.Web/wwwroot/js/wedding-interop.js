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

    playMusic: function () {
        var a = document.querySelector('audio[loop]');
        if (a) a.play().catch(function () { });
    },

    pauseMusic: function () {
        var a = document.querySelector('audio[loop]');
        if (a) a.pause();
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
    }
};
