// The previous Kaboome demo was a PWA with a service worker at this path. This
// replacement clears its caches, unregisters itself and reloads open tabs, so
// returning visitors get the current site instead of the cached old app.
self.addEventListener("install", () => self.skipWaiting());
self.addEventListener("activate", (event) => {
  event.waitUntil(
    (async () => {
      for (const key of await caches.keys()) await caches.delete(key);
      await self.registration.unregister();
      for (const client of await self.clients.matchAll({ type: "window" })) client.navigate(client.url);
    })(),
  );
});
