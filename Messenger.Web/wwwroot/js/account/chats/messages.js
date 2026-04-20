async function sendMessage() {
    if (!currentChatId || isSending) return;
    const text = messageInput.value.trim();
    if (!text && selectedFiles.length === 0) return;

    isSending = true;
    sendButton.disabled = true;
    const tempId = 'temp-' + Date.now();

    appendOptimisticMessage(text, selectedFiles, tempId);
    updateChatLastMessagePreview(currentChatId, text, selectedFiles);

    try {
        const formData = new FormData();
        if (text) formData.append('messageText', text);
        selectedFiles.forEach(file => formData.append('files', file));

        const res = await fetchWithAuth(`${API_BASE}/messages/${currentChatId}`, {
            method: 'POST',
            body: formData
        });

        if (!res?.ok) throw new Error(`HTTP ${res.status}`);
        const result = await res.json();
        const serverMessageId = result?.data?.messageId || result?.messageId;
        if (serverMessageId) {
            pendingMessages.set(tempId, { serverMessageId });
        }
        clearInputAfterSend();
    } catch (err) {
        console.error('Ошибка отправки:', err);
        updateMessageStatus(tempId, 'failed', err.message || 'Ошибка');
        showToast('Не удалось отправить сообщение', 'error');
    } finally {
        isSending = false;
        sendButton.disabled = false;
    }
}

function appendMessage(msg) {
    if (msg.messageId && document.querySelector(`[data-mid="${msg.messageId}"]`)) {
        return;
    }

    const isMyMessage = String(msg.senderId || msg.sender || "").trim() === String(me).trim();

    updateChatLastMessagePreview(
        msg.chatId,
        msg.messageText || '',
        msg.attachments || [],
        msg.sentAt || msg.sentTime,
        isMyMessage
    );

    removeEmptyStateIfNeeded();

    let displayText = msg.messageText || "";
    if (displayText && displayText.length > 20 && !displayText.includes(' ') && !displayText.includes('\n') &&
        /^[A-Za-z0-9+/=]+$/.test(displayText)) {
        displayText = displayText.trim() === "" ? "[Сообщение]" : displayText;
    }

    const isGroupChat = currentChatInfo?.type === 'group';

    const div = document.createElement('div');
    div.className = `mb-6 flex ${isMyMessage ? 'justify-end' : 'justify-start'}`;
    if (msg.messageId) div.dataset.mid = msg.messageId;

    const time = formatDateTime(msg.sentAt || msg.sentTime);

    let senderNameHtml = '';
    if (isGroupChat && !isMyMessage) {
        senderNameHtml = `<span class="sender-name font-medium text-gray-700 block mb-1">Загрузка...</span>`;
    }

    const bubbleClass = isMyMessage
        ? 'bg-blue-600 text-white'
        : 'bg-gray-100 text-gray-900';

    div.innerHTML = `
        <div class="flex flex-col ${isMyMessage ? 'items-end' : 'items-start'} max-w-xs lg:max-w-md">
            ${senderNameHtml}
            <div class="inline-block px-4 py-3 rounded-3xl shadow-sm ${bubbleClass}">
                ${displayText ? `<p class="break-words whitespace-pre-wrap leading-relaxed">${displayText.replace(/\n/g, '<br>')}</p>` : ''}
                ${renderAttachments(msg.attachments || [])}
            </div>
            <div class="text-xs text-gray-500 mt-1 ${isMyMessage ? 'text-right' : 'text-left'}">
                ${time}
                ${isGroupChat && isMyMessage ? ' · Вы' : ''}
            </div>
        </div>
    `;

    messagesContainer.appendChild(div);
    feather.replace();

    if (isGroupChat && !isMyMessage) {
        loadUserName(msg.senderId).then(name => {
            const nameEl = div.querySelector('.sender-name');
            if (nameEl) nameEl.textContent = name || "Участник";
        });
    }

    scrollToBottom();
}

function appendOptimisticMessage(text, files, tempId) {
    removeEmptyStateIfNeeded();

    const div = document.createElement('div');
    div.className = `mb-6 flex justify-end`;
    div.dataset.mid = tempId;

    let content = text
        ? `<p class="break-words whitespace-pre-wrap">${text.replace(/\n/g, '<br>')}</p>`
        : `<div class="opacity-70 text-sm">Отправка ${files.length} вложений...</div>`;

    div.innerHTML = `
        <div class="flex flex-col items-end max-w-xs lg:max-w-md">
            <div class="inline-block px-4 py-3 rounded-3xl bg-blue-600 text-white relative">
                ${content}
                <div id="status-${tempId}" class="message-status flex items-center gap-1.5 text-xs mt-1.5 opacity-75">
                    <i data-feather="loader" class="w-3 h-3 animate-spin"></i>
                    <span>Отправляется...</span>
                </div>
            </div>
        </div>
    `;

    messagesContainer.appendChild(div);
    feather.replace();
    scrollToBottom();

    setTimeout(() => {
        const stillExists = document.querySelector(`[data-mid="${tempId}"]`);
        if (stillExists) {
            const statusEl = stillExists.querySelector('.message-status');
            if (statusEl && statusEl.textContent.includes('Отправляется')) {
                updateMessageStatus(tempId, 'failed', 'Таймаут подтверждения');
            }
        }
    }, 15000);
}

function updateMessageStatus(id, status, reason = null) {
    let el = document.querySelector(`[data-mid="${id}"]`);
    if (!el) return;
    const container = el.querySelector('.message-status');
    if (!container) return;

    if (status === 'sent') {
        container.innerHTML = `<span class="text-blue-200">${formatDateTime(new Date())}</span>`;
    } else if (status === 'failed') {
        container.className = 'message-status flex items-center gap-1.5 text-xs mt-1.5 text-red-400 font-medium';
        container.innerHTML = `<i data-feather="alert-triangle" class="w-4 h-4"></i><span>${reason || 'Ошибка'}</span>`;
    }
    feather.replace();
}

function updateChatLastMessagePreview(chatId, messageText, attachments = [], sentAt = null, isMyMessage = false) {
    const chatItem = document.querySelector(`.chat-item[data-chat-id="${chatId}"]`);
    if (!chatItem) return;
    const lastMessageEl = chatItem.querySelector('.text-sm.text-gray-500');
    if (!lastMessageEl) return;

    let previewHTML = '';
    if (messageText && messageText.trim() !== '') {
        const shortText = messageText.trim().length > 45 ? messageText.trim().substring(0, 42) + '...' : messageText.trim();
        previewHTML = shortText;
    } else if (attachments && attachments.length > 0) {
        const imageCount = attachments.filter(a => a.fileType?.startsWith('image/')).length;
        previewHTML = imageCount > 0
            ? `<i data-feather="image" class="w-4 h-4 inline"></i> Фото`
            : `<i data-feather="paperclip" class="w-4 h-4 inline"></i> Вложение`;
    } else {
        previewHTML = 'Новое сообщение';
    }

    if (attachments && attachments.length > 0 && messageText && messageText.trim() !== '') {
        const shortText = messageText.trim().length > 35 ? messageText.trim().substring(0, 32) + '...' : messageText.trim();
        previewHTML = `<i data-feather="paperclip" class="w-4 h-4 inline"></i> Вложение: ${shortText}`;
    }

    lastMessageEl.innerHTML = previewHTML;
    const chatsContainer = document.getElementById('chats-container');
    if (chatsContainer) chatsContainer.prepend(chatItem);
    feather.replace();
}

function renderAttachments(attachments = []) {
    if (!attachments?.length) return '';
    return attachments.map(att => {
        const fileNameFromUrl = att.url.split('/').pop();
        const isImage = att.fileType?.startsWith('image/');
        const displayUrl = `/uploads/${fileNameFromUrl}`;
        const downloadUrl = `/uploads/${fileNameFromUrl}?download=1`;
        const size = att.sizeInBytes ? (att.sizeInBytes / 1024).toFixed(1) + ' КБ' : '';

        if (isImage) {
            return `
                <img src="${displayUrl}" class="image-preview mt-2 cursor-zoom-in" alt="${att.fileName}" onclick="openLightbox('${displayUrl}')">
            `;
        } else {
            return `
                <div class="file-attachment mt-2">
                    <i data-feather="file"></i>
                    <a href="${downloadUrl}" class="text-white-600 hover:underline font-medium">
                       ${att.fileName} ${size ? `<span class="text-xs opacity-70">(${size})</span>` : ''}
                    </a>
                </div>
            `;
        }
    }).join('');
}

function renderLastMessagePreview(lastMessageValue) {
    if (typeof lastMessageValue === "string") {
        const text = lastMessageValue.trim();
        if (text === "") return '<i data-feather="paperclip" class="w-4 h-4"></i> Вложение';
        return text.length > 38 ? text.substring(0, 35) + '...' : text;
    }
    if (typeof lastMessageValue === "object" && lastMessageValue !== null) {
        if (lastMessageValue.messageText && lastMessageValue.messageText.trim()) {
            const text = lastMessageValue.messageText.trim();
            return text.length > 38 ? text.substring(0, 35) + '...' : text;
        }
        if (lastMessageValue.attachments && lastMessageValue.attachments.length > 0) {
            const imgs = lastMessageValue.attachments.filter(a => a.fileType?.startsWith('image/')).length;
            const vids = lastMessageValue.attachments.filter(a => a.fileType?.startsWith('video/')).length;
            const files = lastMessageValue.attachments.length - imgs - vids;
            const parts = [];
            if (imgs > 0) parts.push(`<i data-feather="image" class="w-4 h-4"></i> Фото${imgs > 1 ? ' (' + imgs + ')' : ''}`);
            if (vids > 0) parts.push(`<i data-feather="video" class="w-4 h-4"></i> Видео${vids > 1 ? ' (' + vids + ')' : ''}`);
            if (files > 0) parts.push(`<i data-feather="paperclip" class="w-4 h-4"></i> Файл${files > 1 ? 'ов: ' + files : ''}`);
            return parts.join('&nbsp;&nbsp;') || '<i data-feather="paperclip"></i> Вложение';
        }
        return '<i data-feather="message-square" class="w-4 h-4"></i> Сообщение';
    }
    return '<span class="italic text-gray-400">Нет сообщений</span>';
}

function renderAttachedPreviews() {
    attachedFiles.innerHTML = '';
    selectedFiles.forEach((file, currentIndex) => {
        const wrapper = document.createElement('div');
        wrapper.className = 'preview-attachment relative inline-block';

        const removeBtn = document.createElement('button');
        removeBtn.type = 'button';
        removeBtn.innerHTML = '×';
        removeBtn.className = 'absolute -top-2 -right-2 w-8 h-8 bg-red-500 hover:bg-red-600 text-white rounded-full flex items-center justify-center text-xl font-bold shadow-lg z-10 transition';
        removeBtn.onclick = (e) => {
            e.stopPropagation();
            selectedFiles.splice(currentIndex, 1);
            renderAttachedPreviews();
        };

        if (file.type.startsWith('image/')) {
            const img = document.createElement('img');
            img.src = URL.createObjectURL(file);
            img.className = 'w-24 h-24 object-cover rounded-xl border-2 border-gray-200 shadow-md';
            wrapper.appendChild(img);
        } else {
            wrapper.innerHTML = `
                <div class="w-24 h-24 bg-gray-100 border-2 border-dashed border-gray-300 rounded-xl flex flex-col items-center justify-center text-gray-600">
                    <i data-feather="file-text" class="w-10 h-10 mb-1"></i>
                    <span class="text-xs px-2 text-center truncate w-full">${file.name.length > 15 ? file.name.slice(0, 12) + '...' : file.name}</span>
                </div>
            `;
            feather.replace();
        }
        wrapper.appendChild(removeBtn);
        attachedFiles.appendChild(wrapper);
    });
}

function clearInputAfterSend() {
    messageInput.value = '';
    attachedFiles.innerHTML = '';
    selectedFiles = [];
    fileInput.value = '';
}

function removeEmptyStateIfNeeded() {
    const emptyPlaceholder = messagesContainer.querySelector('.flex-1.flex.flex-col.items-center.justify-center.text-gray-400');
    if (emptyPlaceholder) emptyPlaceholder.remove();
}

function loadUserName(userId) {
    if (!userId) return Promise.resolve("Пользователь");

    const normalizedId = String(userId).trim();
    if (normalizedId !== String(me || '@Model.UserId' || "").trim()) {
        userNameCache.delete(normalizedId);
    }

    if (userNameCache.has(normalizedId)) {
        return Promise.resolve(userNameCache.get(normalizedId));
    }

    return fetch(`${API_BASE}/users/${normalizedId}/name`, {
        headers: {
            'Authorization': `Bearer ${token}`,
            'Cache-Control': 'no-cache'
        }
    })
        .then(res => res.ok ? res.json() : null)
        .then(json => {
            if (json?.isSuccess && json.data) {
                const name = json.data.trim();
                userNameCache.set(normalizedId, name);
                return name;
            }
            userNameCache.set(normalizedId, 'Пользователь');
            return 'Пользователь';
        })
        .catch(() => {
            userNameCache.set(normalizedId, 'Пользователь');
            return 'Пользователь';
        });
}