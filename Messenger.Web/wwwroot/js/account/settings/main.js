let connection = null;

async function initSignalR() {
    const token = getJwtToken();
    if (!token) {
        console.error("Нет токена - SignalR не запускается");
        return;
    }

    connection = new signalR.HubConnectionBuilder()
        .withUrl('https://localhost:7001/hubs/chat', {
            accessTokenFactory: () => token
        })
        .withAutomaticReconnect()
        .configureLogging(signalR.LogLevel.Information)
        .build();

    connection.on('UserBlockStatusChanged', (data) => {
        const me = '@Model.CurrentUser?.UserId'.toLowerCase();
        if (data.targetId?.toLowerCase() === me) {
            showToast(
                data.isBlocked
                    ? "Вас добавили в чёрный список"
                    : "Вас убрали из чёрного списка",
                data.isBlocked ? "warning" : "info"
            );
        }
    });

    connection.on("ProfileUpdated", (data) => {
        if (data.userId.toLowerCase() === '@(Model.CurrentUser?.UserId ?? "")'.toLowerCase()) {
            updateAvatarDisplay(data.avatarUrl);
            hasOriginalAvatar = !!data.avatarUrl;

            const deleteBtn = document.getElementById('delete-avatar-trigger');
            if (deleteBtn) {
                deleteBtn.classList.toggle('hidden', !hasOriginalAvatar);
            }
        }
    });

    try {
        await connection.start();
        console.log("SignalR успешно подключён!");
    } catch (err) {
        console.error("SignalR НЕ подключился:", err);
        showToast("Не удалось подключиться к серверу в реальном времени", "warning");
    }
}

document.addEventListener('DOMContentLoaded', async () => {
    feather.replace();

    await initSignalR();

    await loadPushSettings();

    checkPushStatus();

    const pushToggleEl = document.getElementById('push-toggle');
    if (pushToggleEl) {
        pushToggleEl.addEventListener('change', togglePushNotifications);
    }

    document.querySelectorAll('#notification-types input[type="checkbox"]').forEach(cb => {
        cb.addEventListener('change', saveNotificationSettings);
    });

    const confirmYesBtn = document.getElementById('confirmYesBtn');
    if (confirmYesBtn) {
        confirmYesBtn.onclick = () => closeConfirm(true);
    }

    document.querySelectorAll('.modal-overlay').forEach(m => {
        m.onclick = (e) => {
            if (e.target === m) {
                m.classList.remove('active');
                if (m.id === 'confirmModal') {
                    closeConfirm(false);
                }
            }
        };
    });
});