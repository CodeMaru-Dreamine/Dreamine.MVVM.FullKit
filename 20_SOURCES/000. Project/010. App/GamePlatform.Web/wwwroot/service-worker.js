const CACHE_NAME = "maru-expedition-shell-v1";
const OFFLINE_ASSETS = [
    "/offline.html",
    "/icons/maru-expedition-192.png",
    "/icons/maru-expedition-512.png"
];

self.addEventListener("install", event => {
    event.waitUntil(caches.open(CACHE_NAME).then(cache => cache.addAll(OFFLINE_ASSETS)));
    self.skipWaiting();
});

self.addEventListener("activate", event => {
    event.waitUntil(
        caches.keys()
            .then(keys => Promise.all(keys.filter(key => key !== CACHE_NAME).map(key => caches.delete(key))))
            .then(() => self.clients.claim())
    );
});

self.addEventListener("fetch", event => {
    if (event.request.mode !== "navigate") return;
    event.respondWith(fetch(event.request).catch(() => caches.match("/offline.html")));
});
