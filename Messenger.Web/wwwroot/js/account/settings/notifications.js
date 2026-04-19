let pushToggle = null;
let currentSubscription = null;

async function loadPushSettings() {
    try {
        const settings = await apiFetch('/push/settings');

        pushToggle = document.getElementById('push-toggle');
        const slider = document.getElementById('push-toggle-slider');
        const typesContainer = document.getElementById('notification-types');

        const isEnabled = !!settings.pushEnabled;

        if (pushToggle) {
            pushToggle.checked = isEnabled;
        }

        if (slider) {
            if (isEnabled) {
                slider.classList.remove('!bg-gray-400', 'cursor-not-allowed', 'opacity-60');
            } else {
                slider.classList.add('!bg-gray-400', 'cursor-not-allowed', 'opacity-60');
            }
        }

        if (typesContainer) {
            typesContainer.classList.toggle('hidden', !isEnabled);
        }

        document.getElementById('notifyMessages').checked = !!settings.notifyMessages;
        document.getElementById('notifyGroup').checked = !!settings.notifyGroupChats;
        document.getElementById('notifyMentions').checked = !!settings.notifyMentions;

    } catch (e) {
        console.error('Не удалось загрузить настройки push:', e);
        showToast('Не удалось загрузить настройки уведомлений', 'error');
    }
}

async function subscribeToPush() {
    try {
        if (Notification.permission === 'default') {
            const permission = await Notification.requestPermission();
            if (permission !== 'granted') {
                if (pushToggle) pushToggle.checked = false;
                showToast('Разрешение на уведомления отклонено', 'error');
                return;
            }
        }

        const registration = await navigator.serviceWorker.ready;

        const keyRes = await fetch(`${API_URL}/push/vapid-public-key`);
        const vapidPublicKey = await keyRes.text();

        let subscription = await registration.pushManager.getSubscription();
        if (!subscription) {
            subscription = await registration.pushManager.subscribe({
                userVisibleOnly: true,
                applicationServerKey: urlBase64ToUint8Array(vapidPublicKey)
            });
        }

        const token = getAuthToken();
        const subJson = subscription.toJSON();

        const res = await fetch(`${API_URL}/push/subscribe`, {
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

        if (res.ok) {
            currentSubscription = subscription;
            showToast('Push-уведомления включены', 'success');
        } else {
            throw new Error('Не удалось сохранить подписку на сервере');
        }
    } catch (err) {
        console.error(err);
        if (pushToggle) pushToggle.checked = false;
        showToast('Не удалось включить push-уведомления', 'error');
    }
}

async function unsubscribeFromPush() {
    try {
        if (!currentSubscription) {
            const registration = await navigator.serviceWorker.ready;
            currentSubscription = await registration.pushManager.getSubscription();
        }

        if (currentSubscription) {
            await currentSubscription.unsubscribe();

            const token = getAuthToken();

            await fetch(`${API_URL}/push/unsubscribe`, {
                method: 'DELETE',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': token
                },
                body: JSON.stringify(currentSubscription.endpoint)
            });

            currentSubscription = null;
            showToast('Push-уведомления отключены', 'info');
        }
    } catch (err) {
        console.error(err);
        if (pushToggle) pushToggle.checked = true;
        showToast('Не удалось отключить push-уведомления', 'error');
    }
}

async function togglePushNotifications() {
    if (!pushToggle) return;

    const slider = document.getElementById('push-toggle-slider');
    const typesContainer = document.getElementById('notification-types');
    const isEnabled = pushToggle.checked;

    if (isEnabled) {
        slider.classList.remove('!bg-gray-400', 'cursor-not-allowed', 'opacity-60');
        await subscribeToPush();
        if (typesContainer) typesContainer.classList.remove('hidden');
    } else {
        slider.classList.add('!bg-gray-400', 'cursor-not-allowed', 'opacity-60');
        await unsubscribeFromPush();
        if (typesContainer) typesContainer.classList.add('hidden');
    }

    await saveNotificationSettings();
}

async function saveNotificationSettings() {
    const dto = {
        pushEnabled: document.getElementById('push-toggle').checked,
        notifyMessages: document.getElementById('notifyMessages').checked,
        notifyGroupChats: document.getElementById('notifyGroup').checked,
        notifyMentions: document.getElementById('notifyMentions').checked
    };

    try {
        await apiFetch('/push/settings', {
            method: 'POST',
            body: JSON.stringify(dto)
        });
    } catch (err) {
        console.error('Ошибка сохранения настроек:', err);
        showToast('Не удалось сохранить настройки уведомлений', 'error');
    }
}

function urlBase64ToUint8Array(base64String) {
    const padding = '='.repeat((4 - base64String.length % 4) % 4);
    const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');
    const rawData = window.atob(base64);
    const outputArray = new Uint8Array(rawData.length);

    for (let i = 0; i < rawData.length; ++i) {
        outputArray[i] = rawData.charCodeAt(i);
    }
    return outputArray;
}

async function checkPushStatus() {
    pushToggle = document.getElementById('push-toggle');
    if (!pushToggle) return;

    if (!('PushManager' in window) || !('serviceWorker' in navigator)) {
        pushToggle.disabled = true;
        const slider = document.getElementById('push-toggle-slider');
        if (slider) slider.classList.add('!bg-gray-400');
        return;
    }

    try {
        const registration = await navigator.serviceWorker.ready;
        currentSubscription = await registration.pushManager.getSubscription();

        if (Notification.permission === 'denied') {
            pushToggle.disabled = true;
        }
    } catch (e) {
        console.error('Ошибка проверки статуса push:', e);
    }
}

window.subscribeToPush = subscribeToPush;
window.unsubscribeFromPush = unsubscribeFromPush;