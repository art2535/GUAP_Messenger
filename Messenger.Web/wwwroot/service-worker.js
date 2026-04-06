const CACHE_NAME = 'guap-messenger-v0.3.4';

const STATIC_ASSETS = [
    '/',
    '/Authorization/Authorization',
    '/Account/Chats',
    '/manifest.json',
    '/css/site.css',
    '/images/web-app-manifest-192x192.png',
    '/images/web-app-manifest-512x512.png'
];

self.addEventListener('install', event => {
    event.waitUntil(
        caches.open(CACHE_NAME)
            .then(cache => cache.addAll(STATIC_ASSETS))
            .then(() => self.skipWaiting())
    );
});

self.addEventListener('activate', event => {
    event.waitUntil(
        caches.keys().then(keys => {
            return Promise.all(keys.map(key => key !== CACHE_NAME ? caches.delete(key) : null));
        }).then(() => self.clients.claim())
    );
});

self.addEventListener('fetch', event => {
    const url = new URL(event.request.url);

    if (url.pathname.includes('/hub') || url.pathname.includes('/api/')) {
        return;
    }

    event.respondWith(
        caches.match(event.request).then(response => {
            return response || fetch(event.request).catch(err => {
                console.warn('Фоновый запрос не удался:', event.request.url);
                return new Response(null, { status: 404 });
            });
        })
    );
});

self.addEventListener('push', event => {
    let data = {
        title: 'GUAP Messenger',
        body: 'Новое сообщение',
        sender: 'Кто-то',
        chatId: null,
        url: '/Account/Chats'
    };

    if (event.data) {
        try {
            const parsed = event.data.json();
            data = { ...data, ...parsed };
        } catch (e) {
            data.body = event.data.text();
        }
    }

    const title = data.sender || data.title || 'Новое сообщение';
    const body = data.body || data.message || '';

    const options = {
        body: body,
        icon: '/images/web-app-manifest-192x192.png',
        badge: '/images/web-app-manifest-192x192.png',
        vibrate: [200, 100, 200],
        tag: data.chatId ? `chat-${data.chatId}` : 'default',
        renotify: true,
        data: {
            url: data.url || `/Account/Chats?chatId=${data.chatId}`,
            chatId: data.chatId
        }
    };

    event.waitUntil(
        self.registration.showNotification(title, options)
    );

    setTimeout(() => {
        self.registration.getNotifications({ tag: data.chatId ? `chat-${data.chatId}` : 'default' })
            .then(notifications => {
                notifications.forEach(notification => notification.close());
            });
    }, 5000);
});

self.addEventListener('notificationclick', event => {
    event.notification.close();

    const urlToOpen = event.notification.data?.url || '/Account/Chats';

    event.waitUntil(
        clients.matchAll({ type: 'window', includeUncontrolled: true }).then(clientList => {
            for (const client of clientList) {
                if (client.url.includes('/Account/Chats') && 'focus' in client) {
                    return client.focus();
                }
            }

            if (clients.openWindow) {
                return clients.openWindow(urlToOpen);
            }
        })
    );
});