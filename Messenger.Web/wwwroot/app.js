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

navigator.serviceWorker.addEventListener('message', async function (event) {
    if (event.data && event.data.type === 'OPEN_SPECIFIC_CHAT') {
        const { chatId, notificationId } = event.data;

        console.log(`[Push Click] Получено из SW: chatId=${chatId}, notificationId=${notificationId}`);

        if (chatId) {
            setTimeout(async () => {
                await openChatById(chatId);
            }, 800);
        }

        if (notificationId) {
            setTimeout(() => {
                markNotificationAsRead(notificationId);
            }, 1500);
        }
    }
});

async function openChatById(chatId) {
    if (!chatId) return;

    console.log(`Попытка открыть чат: ${chatId}`);

    document.querySelectorAll('.chat-item').forEach(item => item.classList.remove('active'));

    let chatItem = document.querySelector(`.chat-item[data-chat-id="${chatId}"]`);

    if (chatItem) {
        console.log('Чат найден в DOM');
        chatItem.click();
        return;
    }

    console.log('Чат не найден, загружаем список...');
    await loadChats();

    chatItem = document.querySelector(`.chat-item[data-chat-id="${chatId}"]`);
    if (chatItem) {
        console.log('Чат найден после загрузки');
        chatItem.click();
    } else {
        console.warn(`Чат ${chatId} не найден даже после перезагрузки списка`);
        showToast('Не удалось открыть чат', 'warning');
    }
}

async function markNotificationAsRead(notificationId) {
    try {
        const token = getAuthToken();
        if (!token) {
            console.warn('markNotificationAsRead: токен отсутствует');
            return;
        }

        const response = await fetch(`${API_BASE_URL}/push/${notificationId}/read`, {
            method: 'POST',
            headers: {
                'Authorization': token,
                'Content-Type': 'application/json'
            }
        });

        if (response.ok) {
            console.log(`Уведомление ${notificationId} помечено как прочитанное`);
        } else {
            console.warn(`Ошибка отметки: ${response.status}`);
        }
    } catch (err) {
        console.error('Ошибка markNotificationAsRead:', err);
    }
}