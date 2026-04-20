async function loadCurrentUser() {
    try {
        const res = await fetchWithAuth(`${API_BASE}/users/info`);
        if (!res || !res.ok) {
            console.warn("Не удалось получить /users/info");
            return;
        }

        const json = await res.json();

        let userId = null;

        if (json.data?.account?.id) {
            userId = json.data.account.id;
        }
        else if (json.data?.id) {
            userId = json.data.id;
        }
        else if (json.data?.userId) {
            userId = json.data.userId;
        }

        if (userId) {
            me = String(userId).trim();
            localStorage.setItem('userId', me);
            console.log(`✅ me успешно установлен: ${me}`);
        } else {
            console.warn("В ответе /users/info не найден ID пользователя", json);
        }
    } catch (err) {
        console.error("Не удалось загрузить информацию о текущем пользователе", err);
    }
}

async function loadAvatar() {
    try {
        const res = await fetchWithAuth(`${API_BASE}/users/info`);
        if (!res || !res.ok) return;
        const json = await res.json();
        if (json.data?.account?.avatar) {
            const avatarUrl = json.data.account.avatar;
            const img = document.getElementById("current-user-avatar");
            if (img) {
                img.src = avatarUrl + "?t=" + new Date().getTime();
            } else {
                const container = document.querySelector(".relative.w-10.h-10");
                if (container) {
                    const placeholder = document.getElementById("current-user-avatar-placeholder");
                    if (placeholder) placeholder.remove();
                    const newImg = document.createElement("img");
                    newImg.id = "current-user-avatar";
                    newImg.className = "w-10 h-10 rounded-full object-cover shadow-md";
                    newImg.alt = "Аватар";
                    newImg.src = avatarUrl + "?t=" + new Date().getTime();
                    container.appendChild(newImg);
                }
            }
        }
    } catch (err) {
        console.error("Ошибка загрузки аватара:", err);
    }
}

function cacheChatItems() {
    allChatItems = Array.from(document.querySelectorAll('.chat-item'));
}

function filterChats() {
    const query = document.getElementById('chat-search').value.trim().toLowerCase();
    document.getElementById('clear-search').classList.toggle('hidden', query === '');
    allChatItems.forEach(item => {
        const title = (item.querySelector('h3')?.textContent || '').toLowerCase();
        const lastMsg = (item.querySelector('p')?.textContent || '').toLowerCase();
        item.style.display = (title.includes(query) || lastMsg.includes(query)) ? '' : 'none';
    });
}

function toggleEmptyState() {
    const container = document.getElementById('chats-container');
    const emptyState = document.getElementById('empty-state');
    const chatCount = container.querySelectorAll('.chat-item').length;

    if (chatCount > 0) {
        emptyState.classList.add('hidden');
        emptyState.classList.remove('flex');
    } else {
        emptyState.classList.remove('hidden');
        emptyState.classList.add('flex');
        feather.replace();
    }
}

function updateConnectionStatus(text, color) {
    document.getElementById('status-dot').className = `w-3 h-3 rounded-full ${color === 'green' ? 'bg-green-500' : color === 'orange' ? 'bg-orange-500' : 'bg-red-500'}`;
    const tooltip = document.getElementById('status-tooltip');
    if (tooltip) tooltip.textContent = text;
}

function updateCurrentChatSubtitle() {
    if (!currentChatId || !currentChatInfo) {
        console.log("updateCurrentChatSubtitle: пропуск — нет currentChatId или currentChatInfo");
        return;
    }
    const subtitle = document.getElementById('chat-subtitle');
    const onlineDot = document.getElementById('chat-online-indicator');

    if (currentChatInfo.type !== 'private') {
        if (subtitle) subtitle.textContent = `${currentChatInfo.participants?.length || 0} участников`;
        if (onlineDot) onlineDot.classList.add('hidden');
        return;
    }

    const partner = currentChatInfo.participants?.find(p => {
        const pid = String(p.id || p.userId || "").toLowerCase().trim();
        return pid !== String(me).toLowerCase().trim();
    });

    if (!partner) return;

    const partnerId = String(partner.id || partner.userId || "").trim();
    const status = userStatuses.get(partnerId);

    if (onlineDot) {
        onlineDot.classList.toggle('hidden', !(status?.isOnline === true));
    }

    if (!subtitle) return;
    if (status?.isOnline === true) {
        subtitle.textContent = "в сети";
        subtitle.classList.add('text-green-600', 'font-medium');
    } else if (status?.lastActivity) {
        subtitle.textContent = `был(а) ${formatLastSeen(status.lastActivity)}`;
        subtitle.classList.remove('text-green-600', 'font-medium');
    } else {
        subtitle.textContent = "был(а) недавно";
    }
}

function updateAllOnlineIndicators() {
    document.querySelectorAll('.chat-item').forEach(item => {
        const partnerId = item.dataset.partnerId;
        if (!partnerId) return;
        const status = userStatuses.get(partnerId);
        const onlineDot = item.querySelector('.chat-online-dot');
        if (onlineDot) {
            onlineDot.classList.toggle('hidden', !(status?.isOnline === true));
        }
    });
}

function updateTypingInChatList(chatId, userId, isTyping) {
    const chatItem = document.querySelector(`.chat-item[data-chat-id="${chatId}"]`);
    if (!chatItem) return;
    const typingBlock = chatItem.querySelector('.typing-in-list');
    const lastMsgBlock = chatItem.querySelector('.last-message-preview');
    if (!typingBlock || !lastMsgBlock) return;

    const myId = String(me || '@Model.UserId' || "").trim();
    const typingUserId = String(userId).trim();
    if (typingUserId === myId) {
        typingBlock.classList.add('hidden');
        lastMsgBlock.classList.remove('hidden');
        return;
    }

    if (isTyping) {
        loadUserName(typingUserId).then(name => {
            let nameEl = typingBlock.querySelector('.typing-name');
            if (!nameEl) {
                typingBlock.innerHTML = `<span class="typing-name"></span> печатает...`;
                nameEl = typingBlock.querySelector('.typing-name');
            }
            if (nameEl) nameEl.textContent = name || "Собеседник";
            typingBlock.classList.remove('hidden');
            lastMsgBlock.classList.add('hidden');
        });
    } else {
        typingBlock.classList.add('hidden');
        lastMsgBlock.classList.remove('hidden');
    }
}

function closeCurrentChat() {
    if (currentChatId && connection?.state === 'Connected') {
        leaveChat(currentChatId);
    }
    currentChatId = null;
    currentChatInfo = null;
    document.getElementById('chat-header').classList.add('hidden');
    document.getElementById('messages-container').classList.add('hidden');
    document.getElementById('chat-input-area').classList.add('hidden');
    document.getElementById('empty-right-panel').classList.remove('hidden');
    document.querySelectorAll('.chat-item').forEach(i => i.classList.remove('active'));
    document.getElementById('message-input').value = '';
    document.getElementById('attached-files').innerHTML = '';
    selectedFiles = [];
}

async function openChat(item) {
    const chatId = item.dataset.chatId;
    if (!chatId || currentChatId === chatId) return;

    if (currentChatId && connection?.state === 'Connected') {
        leaveChat(currentChatId);
    }

    currentChatId = chatId;
    currentChatInfo = null;

    document.querySelectorAll('.chat-item').forEach(i => i.classList.remove('active'));
    item.classList.add('active');

    document.getElementById('empty-right-panel').classList.add('hidden');
    document.getElementById('chat-header').classList.remove('hidden');
    document.getElementById('messages-container').classList.remove('hidden');
    document.getElementById('chat-input-area').classList.remove('hidden');

    const chatNameEl = item.querySelector('h3');
    document.getElementById('chat-title').textContent = chatNameEl ? chatNameEl.textContent : 'Чат';

    const avatarEl = item.querySelector('img');
    if (avatarEl) {
        document.getElementById('chat-info-avatar').src = avatarEl.src;
    }

    unblockChat();
    messageInput.value = '';
    attachedFiles.innerHTML = '';
    selectedFiles = [];

    messagesContainer.innerHTML = '<div class="flex items-center justify-center h-full text-gray-500">Загрузка сообщений...</div>';

    try {
        const res = await fetchWithAuth(`${API_BASE}/chats/${chatId}`);
        if (res?.ok) {
            const json = await res.json();
            if (json.isSuccess && json.data) {
                currentChatInfo = json.data;
                applyBlockStatus();
                refreshBlockStatus();

                if (currentChatInfo.type === 'private') {
                    const partner = currentChatInfo.participants?.find(p =>
                        String(p.id || p.userId || "").toLowerCase() !== String(me).toLowerCase()
                    );
                    if (partner) {
                        const partnerId = String(partner.id || partner.userId || "");
                        setTimeout(() => requestPartnerStatus(partnerId), 300);
                    }
                }
            }
        }
    } catch (e) {
        console.warn("Не удалось загрузить информацию о чате", e);
    }

    await loadMessages(chatId);

    joinChat(chatId);

    setTimeout(updateCurrentChatSubtitle, 100);
    setTimeout(updateCurrentChatSubtitle, 500);
}

async function requestPartnerStatus(partnerId) {
    if (!connection || connection.state !== 'Connected') return;
    try {
        await connection.invoke("RequestAndBroadcastUserStatus", partnerId);
    } catch (err) {
        console.warn("Не удалось запросить статус партнёра", err);
    }
}

async function loadChats() {
    const container = document.getElementById('chats-container');
    const loading = document.getElementById('chats-loading');
    const emptyState = document.getElementById('empty-state');

    try {
        const res = await fetchWithAuth(`${API_BASE}/chats`);
        if (!res) return;

        const json = await res.json();
        allChats = json.data || [];

        if (loading) loading.style.display = 'none';
        container.innerHTML = '';

        if (!json.isSuccess || !allChats.length) {
            toggleEmptyState();
            return;
        }

        allChats.forEach(chat => {
            const div = document.createElement('div');
            div.className = 'p-4 chat-item flex items-center space-x-3 cursor-pointer hover:bg-gray-100 transition rounded-lg mx-2';
            div.dataset.chatId = chat.chatId || chat.id;

            const isPrivate = chat.type === 'private' || chat.isPrivate;
            let displayName = chat.name || 'Без имени';
            let displayAvatar = chat.avatar || 'https://novgorodskij-r49.gosweb.gosuslugi.ru/netcat_files/9/260/user_test.png';
            let partnerId = null;

            if (isPrivate && chat.participants?.length) {
                const interlocutor = chat.participants.find(p =>
                    String(p.id || p.userId || "").toLowerCase() !== String(me).toLowerCase()
                );
                if (interlocutor) {
                    displayName = interlocutor.fullName || interlocutor.name || displayName;
                    displayAvatar = interlocutor.avatar || interlocutor.avatarPath || displayAvatar;
                    partnerId = interlocutor.id || interlocutor.userId;
                }
            }

            div.innerHTML = `
                <div class="relative flex-shrink-0">
                    <img src="${displayAvatar}" class="w-12 h-12 rounded-full object-cover">
                    <span class="chat-online-dot absolute bottom-0 right-0 w-3.5 h-3.5 bg-green-500 rounded-full border-2 border-white hidden"></span>
                </div>
                <div class="flex-1 min-w-0">
                    <h3 class="font-medium truncate">${displayName}</h3>
                    <div class="text-sm text-gray-500 truncate flex items-center gap-1.5 last-message-preview">
                        ${renderLastMessagePreview(chat.lastMessage)}
                    </div>
                    <div class="typing-in-list hidden text-xs text-blue-600 font-medium mt-0.5">
                        <span class="typing-name"></span> печатает...
                    </div>
                </div>
            `;

            if (partnerId) div.dataset.partnerId = partnerId;
            div.onclick = () => openChat(div);

            container.appendChild(div);
        });

        cacheChatItems();
        filterChats();
        feather.replace();
        setTimeout(updateAllOnlineIndicators, 400);

    } catch (err) {
        console.error("Ошибка загрузки чатов:", err);
    }
}

async function loadMessages(chatId) {
    const container = document.getElementById('messages-container');

    container.innerHTML = `
        <div class="flex items-center justify-center h-full text-gray-500">
            <i data-feather="loader" class="w-6 h-6 animate-spin mr-3"></i>
            Загрузка сообщений...
        </div>`;

    try {
        const res = await fetchWithAuth(`${API_BASE}/messages/${chatId}`);

        if (!res || !res.ok) {
            throw new Error(`HTTP ${res ? res.status : 'Network error'}`);
        }

        const json = await res.json();
        const messages = json.data || json || [];

        originalMessages = [...messages];

        container.innerHTML = '';

        if (messages.length === 0) {
            container.innerHTML = `
                <div class="flex-1 flex flex-col items-center justify-center text-gray-400 py-12">
                    <i data-feather="message-square" class="w-12 h-12 mb-4 opacity-30"></i>
                    <p class="text-lg">Сообщений пока нет</p>
                    <p class="text-sm mt-1">Напишите первое сообщение</p>
                </div>`;
        } else {
            messages.forEach(msg => appendMessage(msg));
        }

        setTimeout(scrollToBottom, 50);
        setTimeout(scrollToBottom, 150);

    } catch (err) {
        console.error('Ошибка загрузки сообщений:', err);

        container.innerHTML = `
            <div class="flex flex-col items-center justify-center h-full text-red-500 text-center px-4">
                <i data-feather="alert-triangle" class="w-12 h-12 mb-4"></i>
                <p class="font-medium">Не удалось загрузить сообщения</p>
                <p class="text-sm mt-2">Проверьте подключение или попробуйте обновить страницу</p>
            </div>`;
    }
}

function closeCurrentChat() {
    if (currentChatId && connection?.state === 'Connected') {
        leaveChat(currentChatId);
    }
    currentChatId = null;
    currentChatInfo = null;

    document.getElementById('chat-header').classList.add('hidden');
    document.getElementById('messages-container').classList.add('hidden');
    document.getElementById('chat-input-area').classList.add('hidden');
    document.getElementById('empty-right-panel').classList.remove('hidden');

    document.querySelectorAll('.chat-item').forEach(i => i.classList.remove('active'));

    messageInput.value = '';
    attachedFiles.innerHTML = '';
    selectedFiles = [];
}