let selectedUserIds = [];
let lastQuery = '';
let searchTimeout = null;
let chatNameChanged = false;
let removedParticipantIds = [];
let addedParticipantIds = [];

function toggleChatNameField() {
    const isGroup = document.querySelector('input[name="chat-type"]:checked').value === 'group';
    document.getElementById('group-name-container').classList.toggle('hidden', !isGroup);
}

document.getElementById('create-chat-btn').onclick = () => {
    selectedUserIds = [];
    document.getElementById('selected-users').innerHTML = '';
    document.getElementById('chat-name').value = '';
    document.getElementById('user-search').value = '';
    document.querySelector('input[value="private"]').checked = true;
    toggleChatNameField();

    document.getElementById('create-chat-modal').classList.add('show');
    lastQuery = '';
    document.getElementById('user-search-results').classList.remove('show');
    setTimeout(() => document.getElementById('user-search').focus(), 150);
};

document.querySelectorAll('input[name="chat-type"]').forEach(r =>
    r.addEventListener('change', toggleChatNameField)
);

document.getElementById('close-modal').onclick =
    document.getElementById('cancel-create').onclick = () => {
        document.getElementById('create-chat-modal').classList.remove('show');
    };

async function searchUsers() {
    const query = document.getElementById('user-search').value.trim();
    const results = document.getElementById('user-search-results');

    if (query.length < 2) {
        results.classList.remove('show');
        results.innerHTML = '';
        lastQuery = '';
        return;
    }
    if (query === lastQuery) return;
    lastQuery = query;

    results.innerHTML = `
        <div class="p-4 text-center text-gray-500">
            <i data-feather="loader" class="w-5 h-5 animate-spin inline"></i> Поиск...
        </div>
    `;
    results.classList.add('show');
    feather.replace();

    try {
        const res = await fetch(`${API_BASE}/users/search?query=${encodeURIComponent(query)}`, {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        const json = await res.json();
        results.innerHTML = '';

        if (!json.data || json.data.length === 0) {
            results.innerHTML = '<div class="p-4 text-gray-500 text-sm">Ничего не найдено</div>';
            feather.replace();
            return;
        }

        const availableUsers = json.data
            .filter(u => !selectedUserIds.includes(u.id))
            .slice(0, 8);

        if (availableUsers.length === 0) {
            results.innerHTML = '<div class="p-4 text-gray-500 text-sm">Все уже добавлены</div>';
            feather.replace();
            return;
        }

        availableUsers.forEach(user => {
            const div = document.createElement('div');
            div.className = 'p-3 hover:bg-gray-50 cursor-pointer flex items-center gap-3 transition rounded-lg';
            div.innerHTML = `
                <img src="${user.avatar || 'https://novgorodskij-r49.gosweb.gosuslugi.ru/netcat_files/9/260/user_test.png'}" 
                     class="w-10 h-10 rounded-full object-cover">
                <span class="font-medium">${user.name || 'Без имени'}</span>
            `;
            div.onclick = () => {
                if (selectedUserIds.includes(user.id)) return;

                const isPrivate = document.querySelector('input[name="chat-type"]:checked').value === 'private';
                if (isPrivate && selectedUserIds.length >= 1) {
                    showToast('В приватном чате может быть только один участник', 'error');
                    return;
                }

                selectedUserIds.push(user.id);

                const tag = document.createElement('div');
                tag.className = 'inline-flex items-center gap-2 px-3 py-2 rounded-full bg-blue-100 text-blue-800 text-sm font-medium';
                tag.innerHTML = `
                    ${user.name || 'Пользователь'}
                    <button type="button" class="ml-2 hover:bg-blue-200 rounded-full p-0.5 transition">
                        <i data-feather="x" class="w-4 h-4"></i>
                    </button>
                `;
                tag.querySelector('button').onclick = (e) => {
                    e.stopPropagation();
                    selectedUserIds = selectedUserIds.filter(id => id !== user.id);
                    tag.remove();
                };

                document.getElementById('selected-users').appendChild(tag);
                feather.replace();

                document.getElementById('user-search').value = '';
                results.classList.remove('show');
                lastQuery = '';
            };
            results.appendChild(div);
        });
        feather.replace();
    } catch (err) {
        console.error('Ошибка поиска:', err);
        results.innerHTML = '<div class="p-4 text-red-500 text-sm">Ошибка связи</div>';
        feather.replace();
    }
}

document.getElementById('user-search').addEventListener('input', () => {
    clearTimeout(searchTimeout);
    searchTimeout = setTimeout(searchUsers, 300);
});

document.getElementById('confirm-create').onclick = async () => {
    const type = document.querySelector('input[name="chat-type"]:checked').value;
    const name = type === 'group' ? document.getElementById('chat-name').value.trim() : null;

    if (type === 'group' && !name) return showToast('Введите название группы', 'error');
    if (selectedUserIds.length === 0) return showToast('Выберите участников', 'error');
    if (type === 'private' && selectedUserIds.length !== 1)
        return showToast('В приватном чате — один участник', 'error');

    try {
        const res = await fetch(`${API_BASE}/chats/create-chat`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ name, type, userIds: selectedUserIds })
        });

        const json = await res.json();
        if (!res.ok || !json.isSuccess) throw new Error(json.error || 'Ошибка создания чата');

        document.getElementById('create-chat-modal').classList.remove('show');
        showToast('Чат успешно создан!', 'success');
        await loadChats();
    } catch (err) {
        showToast('Ошибка: ' + err.message, 'error');
    }
};

function openChatInfo() {
    if (!currentChatId) {
        showToast('Сначала выберите чат', 'error');
        return;
    }
    const info = currentChatInfo;
    if (!info) return;

    document.getElementById('modal-chat-avatar').src = document.getElementById('chat-info-avatar').src;
    document.getElementById('modal-chat-name').value = document.getElementById('chat-title').textContent;
    document.getElementById('modal-chat-type').textContent = info.type === 'group' ? 'Групповой чат' : 'Личный чат';

    const list = document.getElementById('participants-list');
    list.innerHTML = '';

    info.participants.forEach(p => {
        const isYou = String(p.id || p.userId || "").trim() === String(me || '@Model.UserId' || "").trim();
        const userId = String(p.id || p.userId || "").trim();
        const status = userStatuses.get(userId);
        const isOnline = status?.isOnline === true;

        const div = document.createElement('div');
        div.className = 'flex items-center justify-between p-4 bg-gray-50 rounded-xl';
        div.dataset.userId = userId;
        div.innerHTML = `
            <div class="flex items-center gap-4">
                <div class="relative">
                    <img src="${p.avatar || 'https://novgorodskij-r49.gosweb.gosuslugi.ru/netcat_files/9/260/user_test.png'}" 
                         class="w-12 h-12 rounded-full object-cover">
                    ${info.type === 'group' ? `
                    <span class="chat-online-dot absolute bottom-0 right-0 w-4 h-4 bg-green-500 rounded-full border-2 border-white ${isOnline ? '' : 'hidden'}"></span>` : ''}
                </div>
                <div>
                    <p class="font-semibold">${p.fullName || p.name || 'Пользователь'}</p>
                    ${isYou ? '<p class="text-sm text-blue-600">Вы</p>' : ''}
                </div>
            </div>
            ${info.type === 'group' && !isYou ? `
            <button type="button" class="text-red-600 hover:bg-red-100 rounded-full p-2 transition"
                    onclick="removeParticipantFromChat('${userId}', this)">
                <i data-feather="x" class="w-5 h-5"></i>
            </button>` : ''}
        `;
        list.appendChild(div);
    });

    document.getElementById('participants-count').textContent = `(${info.participants.length})`;
    chatNameChanged = false;
    removedParticipantIds = [];
    addedParticipantIds = [];
    toggleSaveButton();

    document.getElementById('chat-info-modal').classList.add('show');
    feather.replace();
}

function removeParticipantFromChat(userId, buttonElement) {
    if (!confirm('Удалить участника из чата?')) return;
    buttonElement.closest('div').remove();
    if (!removedParticipantIds.includes(userId)) {
        removedParticipantIds.push(userId);
    }
    updateParticipantCount();
    toggleSaveButton();
}

function addParticipantToChat(userId, userName, userAvatar) {
    if (document.querySelector(`#participants-list [data-user-id="${userId}"]`)) {
        showToast('Этот пользователь уже в чате', 'error');
        return;
    }

    const list = document.getElementById('participants-list');
    const div = document.createElement('div');
    div.className = 'flex items-center justify-between p-4 bg-gray-50 rounded-xl';
    div.dataset.userId = userId;
    div.innerHTML = `
        <div class="flex items-center gap-4">
            <img src="${userAvatar || 'https://novgorodskij-r49.gosweb.gosuslugi.ru/netcat_files/9/260/user_test.png'}" 
                 class="w-12 h-12 rounded-full object-cover">
            <div>
                <p class="font-semibold">${userName}</p>
            </div>
        </div>
        ${currentChatInfo.type === 'group' ? `
        <button type="button" class="text-red-600 hover:bg-red-100 rounded-full p-2 transition"
                onclick="removeParticipantFromChat('${userId}', this)">
            <i data-feather="x" class="w-5 h-5"></i>
        </button>` : ''}
    `;
    list.appendChild(div);
    feather.replace();

    if (!addedParticipantIds.includes(userId)) {
        addedParticipantIds.push(userId);
    }
    updateParticipantCount();
    toggleSaveButton();
}

function updateParticipantCount() {
    const count = document.getElementById('participants-count');
    const base = currentChatInfo.participants.length;
    const newCount = base - removedParticipantIds.length + addedParticipantIds.length;
    count.textContent = `(${newCount})`;
}

function toggleSaveButton() {
    const saveBtn = document.getElementById('save-chat-info');
    const hasChanges = chatNameChanged || removedParticipantIds.length > 0 || addedParticipantIds.length > 0;
    saveBtn.classList.toggle('opacity-0', !hasChanges);
    saveBtn.classList.toggle('pointer-events-none', !hasChanges);
}

document.getElementById('save-chat-info').addEventListener('click', async () => {
    if (!currentChatId || !currentChatInfo) return;

    const newName = document.getElementById('modal-chat-name').value.trim();
    const isGroup = currentChatInfo.type === 'group';
    const tasks = [];

    if (isGroup && chatNameChanged && newName && newName !== currentChatInfo.name) {
        tasks.push(fetch(`${API_BASE}/chats/${currentChatId}`, {
            method: 'PUT',
            headers: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' },
            body: JSON.stringify({ Name: newName })
        }));
    }

    removedParticipantIds.forEach(userId => {
        tasks.push(fetch(`${API_BASE}/chats/${currentChatId}/${userId}`, {
            method: 'DELETE',
            headers: { 'Authorization': `Bearer ${token}` }
        }));
    });

    addedParticipantIds.forEach(userId => {
        tasks.push(fetch(`${API_BASE}/chats/${currentChatId}/${userId}/participant`, {
            method: 'POST',
            headers: { 'Authorization': `Bearer ${token}` }
        }));
    });

    if (tasks.length === 0) return;

    try {
        const responses = await Promise.all(tasks);
        const allSuccess = responses.every(r => r.ok);
        if (!allSuccess) throw new Error('Не все изменения сохранены');

        if (chatNameChanged && isGroup && newName) {
            document.getElementById('chat-title').textContent = newName;
            const chatItem = document.querySelector(`.chat-item[data-chat-id="${currentChatId}"] h3`);
            if (chatItem) chatItem.textContent = newName;
            currentChatInfo.name = newName;
        }

        currentChatInfo.participants = currentChatInfo.participants.filter(p =>
            !removedParticipantIds.includes(p.id)
        );

        document.getElementById('chat-info-modal').classList.remove('show');
        showToast('Изменения сохранены!', 'success');
        await loadChats();
    } catch (err) {
        showToast('Ошибка: ' + err.message, 'error');
    } finally {
        removedParticipantIds = [];
        addedParticipantIds = [];
        chatNameChanged = false;
    }
});

document.getElementById('modal-chat-name').addEventListener('input', () => {
    chatNameChanged = true;
    toggleSaveButton();
});

document.getElementById('delete-chat-btn')?.addEventListener('click', function () {
    if (!currentChatId) return;

    document.getElementById('chat-info-modal').classList.remove('show');

    const overlay = document.createElement('div');
    overlay.id = 'delete-confirm-overlay';
    overlay.className = 'fixed inset-0 bg-black bg-opacity-60 flex items-center justify-center z-50';
    overlay.innerHTML = `
        <div class="bg-white rounded-2xl p-8 max-w-md w-full mx-4 shadow-2xl">
            <div class="text-center mb-8">
                <div class="w-20 h-20 bg-red-100 rounded-full flex items-center justify-center mx-auto mb-5">
                    <i data-feather="trash-2" class="w-12 h-12 text-red-600"></i>
                </div>
                <h3 class="text-2xl font-bold text-gray-900">Удалить чат навсегда?</h3>
                <p class="text-gray-600 mt-4 leading-relaxed">
                    Все сообщения и файлы будут удалены.<br>
                    Это действие <strong>нельзя отменить</strong>.
                </p>
            </div>
            <div class="flex gap-4">
                <button id="cancel-delete" class="flex-1 px-6 py-3 bg-gray-200 hover:bg-gray-300 text-gray-800 rounded-xl font-medium transition">
                    Отмена
                </button>
                <button id="confirm-delete" class="flex-1 px-6 py-3 bg-red-600 hover:bg-red-700 text-white rounded-xl font-medium transition">
                    Удалить чат
                </button>
            </div>
        </div>
    `;

    document.body.appendChild(overlay);
    feather.replace();

    overlay.querySelector('#cancel-delete').onclick = () => overlay.remove();

    overlay.querySelector('#confirm-delete').onclick = async () => {
        try {
            const res = await fetch(`${API_BASE}/chats/${currentChatId}`, {
                method: 'DELETE',
                headers: { 'Authorization': `Bearer ${token}` }
            });

            if (!res.ok) throw new Error('Не удалось удалить чат');

            overlay.remove();
            document.querySelector(`.chat-item[data-chat-id="${currentChatId}"]`)?.remove();
            closeCurrentChat();
            showToast('Чат успешно удалён', 'success');
            await loadChats();
        } catch (err) {
            overlay.remove();
            showToast('Ошибка: ' + err.message, 'error');
        }
    };
});

document.addEventListener('keydown', function (e) {
    if (e.key !== 'Escape') return;

    const createModal = document.getElementById('create-chat-modal');
    const infoModal = document.getElementById('chat-info-modal');
    const lightboxEl = document.getElementById('lightbox');
    const deleteOverlay = document.getElementById('delete-confirm-overlay');

    if (deleteOverlay) {
        deleteOverlay.remove();
        return;
    }
    if (infoModal && infoModal.classList.contains('show')) {
        infoModal.classList.remove('show');
        removedParticipantIds = [];
        addedParticipantIds = [];
        chatNameChanged = false;
        return;
    }
    if (createModal && createModal.classList.contains('show')) {
        createModal.classList.remove('show');
        return;
    }
    if (lightboxEl && lightboxEl.classList.contains('show')) {
        lightboxEl.classList.remove('show');
        document.getElementById('lightbox-img').src = '';
        return;
    }
});