async function startSignalR() {
    if (!token) {
        console.error("Нет токена для SignalR");
        updateConnectionStatus('Нет токена', 'red');
        return;
    }

    async function getFreshToken() {
        let freshToken = '@HttpContext.Session.GetString("ACCESS_TOKEN")' || '';
        if (!freshToken) {
            console.warn("Токен пропал из session — редирект");
            window.location.href = "/Authorization/Authorization";
            return null;
        }
        return freshToken;
    }

    connection = new signalR.HubConnectionBuilder()
        .withUrl(HUB_URL, {
            accessTokenFactory: () => token
        })
        .withAutomaticReconnect([0, 2000, 5000, 10000])
        .build();

    connection.on('ParticipantCountChanged', (data) => {
        if (!data) return;

        const chatId = String(data.chatId || data.ChatId || "").trim();
        const newCount = parseInt(data.count || data.Count || "0");

        if (!chatId) return;

        console.log(`[PARTICIPANT COUNT] Чат ${chatId} → ${newCount} участников`);

        if (currentChatId === chatId) {
            const subtitleEl = document.getElementById('chat-subtitle');
            if (subtitleEl && currentChatInfo?.type === 'group') {
                subtitleEl.textContent = `${newCount} участников`;
                console.log(`[PARTICIPANT COUNT] Обновлено в шапке чата: ${newCount} участников`);
            }

            if (document.getElementById('chat-info-modal').classList.contains('show')) {
                const countEl = document.getElementById('participants-count');
                if (countEl) countEl.textContent = `(${newCount})`;
            }

            if (currentChatInfo) {
                currentChatInfo.participantsCount = newCount;
            }
        }
    });

    connection.on("UserOnlineStatusChanged", (data) => {
        console.log(`[STATUS] ${data.userId} → ${data.isOnline ? 'ONLINE' : 'OFFLINE'}`, data);

        userStatuses.set(data.userId.toString(), {
            isOnline: !!data.isOnline,
            lastActivity: data.lastActivity
        });

        if (currentChatId) {
            updateCurrentChatSubtitle();
        }

        if (document.getElementById('chat-info-modal').classList.contains('show') && currentChatInfo?.type === 'group') {
            const userIdStr = data.userId.toString().trim();
            const isOnlineNow = !!data.isOnline;

            const participantEl = document.querySelector(`#participants-list [data-user-id="${userIdStr}"]`);
            if (participantEl) {
                const onlineDot = participantEl.querySelector('.chat-online-dot');
                if (onlineDot) {
                    onlineDot.classList.toggle('hidden', !isOnlineNow);
                }
            }
        }
    });

    connection.on("UserIsTyping", (data) => {
        if (!data?.chatId || !data?.userId) return;

        const typingUserId = String(data.userId).trim();
        const myId = String(me || '@Model.UserId' || "").trim();

        console.log(`[TYPING RECEIVED] chat=${data.chatId}, user=${typingUserId}, isTyping=${data.isTyping}, myId=${myId}`);

        if (typingUserId === myId) {
            console.log(`[TYPING] Игнорируем своё печатание`);
            return;
        }

        if (data.chatId === currentChatId) {
            const typingIndicator = document.getElementById('typing-indicator');
            const typingNameEl = document.getElementById('typing-name');
            const subtitle = document.getElementById('chat-subtitle');

            if (data.isTyping) {
                loadUserName(typingUserId).then(name => {
                    console.log(`[TYPING] Показываем: ${name} печатает...`);
                    if (typingNameEl) typingNameEl.textContent = name || "Собеседник";
                    if (typingIndicator) typingIndicator.classList.remove('hidden');
                    if (subtitle) subtitle.classList.add('hidden');
                });
            } else {
                if (typingIndicator) typingIndicator.classList.add('hidden');
                if (subtitle) subtitle.classList.remove('hidden');
            }
        }

        updateTypingInChatList(data.chatId, typingUserId, data.isTyping);
    });

    connection.on("ProfileUpdated", function (data) {
        if (data.userId !== "@Model.UserId") return;

        const avatarContainer = document.querySelector(".relative.w-10.h-10");
        if (!avatarContainer) return;

        const displayNameElement = avatarContainer.parentElement.querySelector("p.font-medium");
        if (displayNameElement) {
            displayNameElement.textContent = data.displayName || "Пользователь";
        }

        if (data.avatarUrl && data.avatarUrl.trim() !== "") {
            let img = document.getElementById("current-user-avatar");
            if (!img) {
                const placeholder = document.getElementById("current-user-avatar-placeholder");
                if (placeholder) placeholder.remove();

                img = document.createElement("img");
                img.id = "current-user-avatar";
                img.className = "w-10 h-10 rounded-full object-cover shadow-md";
                img.alt = "Аватар";
                avatarContainer.appendChild(img);
            }
            img.src = data.avatarUrl + "?t=" + new Date().getTime();
        } else {
            let placeholder = document.getElementById("current-user-avatar-placeholder");
            const img = document.getElementById("current-user-avatar");

            if (img) img.remove();

            if (!placeholder) {
                placeholder = document.createElement("div");
                placeholder.id = "current-user-avatar-placeholder";
                placeholder.className = "guap-gradient w-10 h-10 rounded-full flex items-center justify-center shadow-md";
                placeholder.innerHTML = '<i data-feather="user" class="w-5 h-5 text-white"></i>';
                avatarContainer.appendChild(placeholder);
                feather.replace();
            }
        }
    });

    connection.on("AvatarUpdated", function (data) {
        if (data.userId !== '@Model.UserId') return;

        const container = document.querySelector('.relative.w-10.h-10');
        if (!container) return;

        const currentImg = document.getElementById('current-user-avatar');
        const placeholder = document.getElementById('current-user-avatar-placeholder');

        if (data.avatarUrl && data.avatarUrl.trim() !== '') {
            if (currentImg) {
                currentImg.src = data.avatarUrl + '?t=' + new Date().getTime();
            } else {
                const img = document.createElement('img');
                img.id = 'current-user-avatar';
                img.className = 'w-10 h-10 rounded-full object-cover shadow-md';
                img.alt = 'Аватар';
                img.src = data.avatarUrl + '?t=' + new Date().getTime();
                container.innerHTML = '';
                container.appendChild(img);
            }
            if (placeholder) placeholder.remove();
        } else {
            container.innerHTML = `
                        <div id="current-user-avatar-placeholder"
                             class="guap-gradient w-10 h-10 rounded-full flex items-center justify-center shadow-md">
                            <i data-feather="user" class="w-5 h-5 text-white"></i>
                        </div>
                    `;
            feather.replace();
        }
    });

    connection.on("UserBlockStatusChanged", (data) => {
        console.log("[Chats] UserBlockStatusChanged →", data);

        const actor = (data.actorId || "").toLowerCase();
        const target = (data.targetId || "").toLowerCase();
        const isBlocked = !!data.isBlocked;
        const me = '@HttpContext.Session.GetString("USER_ID")'?.toLowerCase().trim() ||
            '@Model.UserId'.toLowerCase().trim();

        console.log("Текущий пользователь (me):", me);

        if (actor === me) {
            if (isBlocked) {
                myBlockedUsers.add(target);
            } else {
                myBlockedUsers.delete(target);
            }

            console.log("Обновлён myBlockedUsers:", Array.from(myBlockedUsers));

            if (currentChatId && currentChatInfo?.type === 'private') {
                console.log("Открыт приватный чат → проверяем партнёра");

                let partner = null;
                for (const p of currentChatInfo.participants || []) {
                    let pid = (p.id || p.userId || "").toString().trim().toLowerCase();
                    if (pid && pid !== me) {
                        partner = p;
                        console.log("Найден партнёр:", p, "pid =", pid);
                        break;
                    }
                }

                if (partner) {
                    const partnerId = (partner.id || partner.userId || "")
                        .toString().trim().toLowerCase();

                    console.log("partnerId:", partnerId, "target:", target);

                    if (partnerId === target) {
                        console.log("→ Это текущий собеседник!");
                        if (isBlocked) {
                            blockChatForMe(partner.name || "этот пользователь");
                        } else {
                            unblockChat();
                        }
                        refreshBlockStatus();
                    }
                }
            } else {
                console.log("Чат не открыт или не приватный → просто обновляем список");
            }

            loadMyBlockedList();
        }

        if (target === me && isBlocked) {
            showToast("Вас заблокировали", "warning");
        }
    });

    connection.on('MessageSendingStatus', (data) => {
        const msgId = data.MessageId || data.messageId;
        let status = (data.Status || data.status || '').toLowerCase();
        const reason = data.Reason || data.reason;

        console.log(`[Status] ${msgId} → ${status}`, data);

        let element = document.querySelector(`[data-mid="${msgId}"]`);
        if (!element) {
            for (const [tempId, info] of pendingMessages) {
                if (info.serverMessageId === msgId) {
                    element = document.querySelector(`[data-mid="${tempId}"]`);
                    console.log(`[Status] Найдено по pendingMessages: ${tempId} → ${msgId}`);
                    break;
                }
            }
        }

        if (element) {
            if (status === 'sent' || status === 'delivered') {
                updateMessageStatus(msgId, 'sent');
                console.log(`[Status] Обновлено на Sent для ${msgId}`);
            } else if (status === 'failed') {
                updateMessageStatus(msgId, 'failed', reason || 'Ошибка');
            }
        } else {
            console.warn(`[Status] Элемент не найден для ${msgId} (уже удалён ReceiveMessage?)`);
        }
    });

    connection.on('ReceiveMessage', (msg) => {
        console.log(`[ReceiveMessage] ${msg.messageId} в чате ${msg.chatId}`);

        if (msg.chatId !== currentChatId) return;

        const optimisticElements = document.querySelectorAll('[data-mid^="temp-"]');
        optimisticElements.forEach(el => {
            console.log(`[ReceiveMessage] Удаляем optimistic: ${el.dataset.mid}`);
            el.remove();
        });

        if (document.querySelector(`[data-mid="${msg.messageId}"]`)) {
            console.log(`[ReceiveMessage] Сообщение ${msg.messageId} уже показано`);
            return;
        }

        appendMessage(msg);

        if (String(msg.senderId) === String(me)) {
            clearInputAfterSend();
            console.log(`[ReceiveMessage] Поле ввода очищено`);
        }
    });

    connection.on('MessageDeliveryFailed', data => {
        if (data.chatId !== currentChatId) return;

        const tempEl = document.querySelector(`[data-mid="${data.messageId}"]`) ||
            document.querySelector(`[data-mid^="temp-"]`);

        if (tempEl) {
            const statusContainer = tempEl.querySelector('div[id^="status-"]') || tempEl.querySelector('.opacity-75');
            if (statusContainer) {
                statusContainer.innerHTML = `<span class="text-red-400 font-medium">Ошибка отправки</span>`;
            }
            tempEl.style.opacity = "0.6";
        }

        showToast('Сообщение не удалось доставить на сервер', 'error');
    });

    connection.on("YouBlocked", (blockedUserId) => {
        myBlockedUsers.add(blockedUserId);
        console.log("Я заблокировал:", blockedUserId);

        if (currentChatId && currentChatInfo?.type === 'private') {
            const recipient = currentChatInfo.participants.find(p => p.id === blockedUserId);
            if (recipient) {
                blockChatForMe(recipient.fullName || recipient.name || "этот контакт");
            }
        }
    });

    connection.on("YouUnblocked", (unblockedUserId) => {
        myBlockedUsers.delete(unblockedUserId);
        console.log("Я разблокировал:", unblockedUserId);

        if (currentChatId && currentChatInfo?.type === 'private') {
            const recipient = currentChatInfo.participants.find(p => p.id === unblockedUserId);
            if (recipient) {
                unblockChat();
            }
        }
    });

    connection.on('UserBlockedMe', (blockerId, blockTime) => {
        if (currentChatInfo?.type === 'private' &&
            currentChatInfo.participants.some(p => p.id === blockerId)) {

            const blocker = currentChatInfo.participants.find(p => p.id === blockerId);
            const name = blocker?.fullName || blocker?.name || "Этот пользователь";

            blockChat(name, blockTime);
        }
    });

    connection.on('UserUnblockedMe', (unblockerId) => {
        if (currentChatInfo?.type === 'private' &&
            currentChatInfo.participants.some(p => p.id === unblockerId)) {
            unblockChat();
        }
    });

    connection.on('ReceiveSearchResults', users => {
        userSearchResults.innerHTML = "";

        if (!users || users.length === 0) {
            userSearchResults.innerHTML = "<div class='p-2 text-muted'>Нет пользователей</div>";
            return;
        }

        users.forEach(user => {
            const userDiv = document.createElement("div");
            userDiv.className = "user-search-item p-2 border-bottom";
            userDiv.style.cursor = "pointer";
            userDiv.dataset.userId = user.id;

            userDiv.innerHTML = `
                        <div class="d-flex align-items-center">
                            <img src="/avatars/${user.avatarPath || 'default.png'}"
                                 class="rounded-circle me-2" width="32" height="32">
                            <span>${user.fullName}</span>
                        </div>
                    `;

            userDiv.onclick = () => {
                toggleUserSelection(user.id, user.fullName);
            };

            userSearchResults.appendChild(userDiv);
        });
    });

    connection.on('NewChat', chat => {
        const container = document.getElementById('chats-container');
        const existing = container.querySelector(`.chat-item[data-chat-id="${chat.chatId}"]`);

        if (!existing) {
            let displayName = chat.name || 'Новый чат';
            let displayAvatar = chat.avatar || 'https://novgorodskij-r49.gosweb.gosuslugi.ru/netcat_files/9/260/user_test.png';

            if (chat.type === 'private' || chat.isPrivate) {
                const currentUserId = '@Model.UserId'.toLowerCase();
                if (chat.participants && chat.participants.length > 0) {
                    const interlocutor = chat.participants.find(p =>
                        (p.id && p.id.toLowerCase() !== currentUserId) ||
                        (p.userId && p.userId.toLowerCase() !== currentUserId)
                    );

                    if (interlocutor) {
                        displayName = interlocutor.fullName || interlocutor.name || displayName;
                        displayAvatar = interlocutor.avatar || interlocutor.avatarPath || displayAvatar;
                    }
                }
            }

            const div = document.createElement('div');
            div.className = 'p-4 chat-item flex items-center space-x-3 cursor-pointer hover:bg-gray-100 transition rounded-lg mx-2';
            div.dataset.chatId = chat.chatId;
            div.innerHTML = `
                        <div class="relative">
                            <img src="${displayAvatar}" class="w-12 h-12 rounded-full object-cover">
                            <span class="absolute bottom-0 right-0 w-3 h-3 bg-green-500 rounded-full border-2 border-white"></span>
                        </div>
                        <div class="flex-1 min-w-0">
                            <h3 class="font-medium truncate">${displayName}</h3>
                            <p class="text-sm text-gray-500 truncate">Чат создан</p>
                        </div>
                    `;

            div.onclick = () => openChat(div);

            container.prepend(div);

            toggleEmptyState();

            cacheChatItems();
            filterChats();
            feather.replace();

            connection.invoke("JoinChat", chat.chatId).catch(err => console.error(err));
        }
    });

    connection.on('YouWereRemovedFromChat', (chatId) => {
        const item = document.querySelector(`.chat-item[data-chat-id="${chatId}"]`);
        if (item) item.remove();

        if (currentChatId === chatId) {
            currentChatId = null;
            currentChatInfo = null;
            messagesContainer.innerHTML = '<div class="flex items-center h-full text-gray-500">Вы были удалены из чата</div>';
            document.getElementById('chat-title').textContent = 'Выберите чат';
        }

        connection.invoke("LeaveChat", chatId).catch(err => console.error(err));

        showToast('Вы были удалены из чата', 'info');
    });

    connection.on('ParticipantRemoved', ({ chatId, userId }) => {
        if (chatId !== currentChatId) return;

        const el = document.querySelector(`#participants-list [data-user-id="${userId}"]`);
        if (el) el.remove();

        if (currentChatInfo) {
            currentChatInfo.participants = currentChatInfo.participants.filter(p => p.id !== userId);
            document.getElementById('participants-count').textContent = `(${currentChatInfo.participants.length})`;
        }

        showToast('Участник удалён', 'info');
    });

    connection.on('ParticipantAdded', ({ chatId, user }) => {
        if (chatId !== currentChatId) return;

        const addedName = user?.name || user?.fullName || user?.login || 'Новый участник';
        showToast(`${addedName} добавлен в чат`, 'success');

        if (document.getElementById('chat-info-modal').classList.contains('show')) {
            if (currentChatInfo && !currentChatInfo.participants.some(p => p.id === user.id)) {
                currentChatInfo.participants.push(user);

                const list = document.getElementById('participants-list');
                const div = document.createElement('div');
                div.className = 'flex items-center justify-between p-4 bg-gray-50 rounded-xl';
                div.dataset.userId = user.id;
                div.innerHTML = `
                            <div class="flex items-center gap-4">
                                <img src="${user.avatar || 'https://novgorodskij-r49.gosweb.gosuslugi.ru/netcat_files/9/260/user_test.png'}"
                                     class="w-12 h-12 rounded-full object-cover">
                                <div>
                                    <p class="font-semibold">${addedName}</p>
                                </div>
                            </div>
                            <button type="button" class="text-red-600 hover:bg-red-100 rounded-full p-2 transition"
                                    onclick="removeParticipantFromChat('${user.id}', this)">
                                <i data-feather="x" class="w-5 h-5"></i>
                            </button>
                        `;
                list.appendChild(div);
                feather.replace();

                document.getElementById('participants-count').textContent =
                    `(${currentChatInfo.participants.length})`;
            }
        }
    });

    connection.on('ChatUpdated', ({ chatId, name }) => {
        const item = document.querySelector(`.chat-item[data-chat-id="${chatId}"] h3`);
        if (item) item.textContent = name;

        if (currentChatId === chatId) {
            document.getElementById('chat-title').textContent = name;
        }

        showToast(`Название чата изменено на: ${name}`, 'info');
    });

    connection.on('ChatDeleted', (chatId) => {
        const chatElement = document.querySelector(`.chat-item[data-chat-id="${chatId}"]`);
        if (chatElement) {
            chatElement.remove();
            toggleEmptyState();
        }

        if (currentChatId === chatId) {
            currentChatId = null;
            currentChatInfo = null;
            messagesContainer.innerHTML = `
                        <div class="flex items-center justify-center h-full text-gray-500">
                            <p>Чат был удалён</p>
                        </div>`;
            document.getElementById('chat-title').textContent = 'Выберите чат';
        }

        cacheChatItems();
        filterChats();
    });

    connection.onreconnecting(() => updateConnectionStatus('Переподключение...', 'orange'));
    connection.onreconnected(() => { updateConnectionStatus('Онлайн', 'green'); rejoinCurrentChat(); });
    connection.onclose(() => { updateConnectionStatus('Отключено', 'red'); setTimeout(startSignalR, 5000); });

    try {
        await connection.start();
        console.log("SignalR успешно подключён");

        updateConnectionStatus('Онлайн', 'green');

        await loadMyBlockedList();

        setTimeout(async () => {
            console.log("Запуск joinAllMyChats...");
            await joinAllMyChats();

            if (currentChatId) {
                setTimeout(updateCurrentChatSubtitle, 300);
            }
        }, 1500);

    } catch (err) {
        console.error("Ошибка подключения SignalR:", err);
        updateConnectionStatus('Ошибка подключения', 'red');

        setTimeout(startSignalR, 5000);
    }
}

async function joinAllMyChats() {
    if (!connection) return;

    while (connection.state !== signalR.HubConnectionState.Connected) {
        console.log(`Ожидаем Connected... текущее состояние: ${connection.state}`);
        await new Promise(resolve => setTimeout(resolve, 300));
    }

    try {
        const res = await fetch(`${API_BASE}/chats`, {
            headers: { 'Authorization': `Bearer ${token}` }
        });

        if (!res.ok) return;

        const json = await res.json();
        console.log("Чаты для join:", json.data?.length || 0);

        if (json.data) {
            for (const chat of json.data) {
                const chatId = chat.chatId || chat.id;
                if (chatId) {
                    try {
                        await connection.invoke("JoinChat", chatId);
                        console.log(`✓ Joined chat ${chatId}`);
                    } catch (e) {
                        console.warn(`JoinChat failed for ${chatId}:`, e.message);
                    }
                }
            }
        }
    } catch (err) {
        console.error("joinAllMyChats error:", err);
    }
}

async function joinChat(id) {
    if (connection?.state === 'Connected')
        await connection.invoke('JoinChat', id).catch(() => { });
}

async function leaveChat(id) {
    if (connection?.state === 'Connected')
        await connection.invoke('LeaveChat', id).catch(() => { });
}

async function rejoinCurrentChat() {
    if (currentChatId)
        joinChat(currentChatId);
}