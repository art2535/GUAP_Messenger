const API_BASE_URL = 'https://localhost:7001/api';

let currentToken = null;

function getAuthToken() {
    let token = localStorage.getItem('token');

    if (!token) {
        const metaToken = document.querySelector('meta[name="access-token"]');
        if (metaToken) token = metaToken.content;
    }

    if (!token && typeof sessionToken !== 'undefined') {
        token = sessionToken;
    }

    if (token && !token.startsWith('Bearer ')) {
        token = 'Bearer ' + token;
    }

    currentToken = token;
    return token;
}

if ('serviceWorker' in navigator) {
    window.addEventListener('load', async () => {
        try {
            const registration = await navigator.serviceWorker.register('/service-worker.js', {
                scope: '/'
            });
            console.log('✅ Service Worker зарегистрирован с scope:', registration.scope);

            setTimeout(() => {
                initializePushNotifications(registration);
            }, 1500);

        } catch (err) {
            console.error('❌ Ошибка регистрации Service Worker:', err);
        }
    });
}

async function initializePushNotifications(registration) {
    const token = getAuthToken();
    if (!token) {
        console.warn('⚠️ Push: Токен авторизации не найден. Подписка отложена.');
        return;
    }

    try {
        const permission = Notification.permission;

        if (permission === 'denied') {
            console.warn('Push уведомления запрещены пользователем');
            return;
        }

        if (permission === 'default') {
            const result = await Notification.requestPermission();
            if (result !== 'granted') return;
        }

        const keyResponse = await fetch(`${API_BASE_URL}/push/vapid-public-key`, {
            method: 'GET',
            headers: {
                'Authorization': token
            }
        });

        if (!keyResponse.ok) throw new Error('Не удалось получить VAPID ключ');
        const publicKey = await keyResponse.text();

        let subscription = await registration.pushManager.getSubscription();

        if (!subscription) {
            subscription = await registration.pushManager.subscribe({
                userVisibleOnly: true,
                applicationServerKey: urlBase64ToUint8Array(publicKey)
            });
            console.log('Новая push-подписка создана');
        } else {
            console.log('Существующая подписка найдена');
        }

        const subJson = subscription.toJSON();

        const response = await fetch(`${API_BASE_URL}/push/subscribe`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': token
            },
            body: JSON.stringify({
                endpoint: subscription.endpoint,
                p256dh: subJson.keys.p256dh,
                auth: subJson.keys.auth
            })
        });

        if (response.ok) {
            console.log('✅ Push-подписка успешно отправлена на сервер');
            localStorage.setItem('pushSubscribed', 'true');
        } else {
            const errorText = await response.text();
            console.error('❌ Ошибка при отправке подписки:', errorText);
        }

    } catch (err) {
        console.error('❌ Ошибка при инициализации Push-уведомлений:', err);
    }
}

function urlBase64ToUint8Array(base64String) {
    const padding = '='.repeat((4 - base64String.length % 4) % 4);
    const base64 = (base64String + padding)
        .replace(/-/g, '+')
        .replace(/_/g, '/');

    const rawData = window.atob(base64);
    const outputArray = new Uint8Array(rawData.length);

    for (let i = 0; i < rawData.length; ++i) {
        outputArray[i] = rawData.charCodeAt(i);
    }
    return outputArray;
}

navigator.serviceWorker.addEventListener('message', event => {
    if (event.data && event.data.type === 'OPEN_CHAT') {
        const chatId = event.data.chatId;
        if (chatId) {
            console.log(`Service Worker запросил открыть чат: ${chatId}`);
            openChatById(chatId);
        }
    }
});

async function openChatById(chatId) {
    const chatItem = document.querySelector(`.chat-item[data-chat-id="${chatId}"]`);
    if (chatItem) {
        chatItem.click();
    } else {
        await loadChats();
        const newItem = document.querySelector(`.chat-item[data-chat-id="${chatId}"]`);
        if (newItem)
            newItem.click();
    }
}