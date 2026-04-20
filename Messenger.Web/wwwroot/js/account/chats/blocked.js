let myBlockedUsers = new Set();

async function loadMyBlockedList() {
    try {
        const res = await fetchWithAuth(`${API_BASE}/users/blocked`);
        if (res?.ok) {
            const json = await res.json();
            if (json.isSuccess && Array.isArray(json.data)) {
                myBlockedUsers = new Set(json.data.map(u => u.id.toLowerCase()));
                console.log(`Загружен мой чёрный список: ${myBlockedUsers.size}`);
                refreshBlockStatus();
            }
        }
    } catch (err) {
        console.warn("Не удалось загрузить мой чёрный список", err);
    }
}

function refreshBlockStatus() {
    if (!currentChatId || !currentChatInfo) return;
    const blockedUi = document.getElementById('blocked-ui');
    const normalUi = document.getElementById('normal-input-ui');

    if (currentChatInfo.type !== 'private') {
        normalUi.classList.remove('hidden');
        blockedUi.classList.add('hidden');
        return;
    }

    const partner = currentChatInfo.participants.find(p =>
        (p.id || p.userId || "").toLowerCase() !== me
    );
    if (!partner) {
        normalUi.classList.remove('hidden');
        blockedUi.classList.add('hidden');
        return;
    }

    const partnerId = (partner.id || partner.userId || "").toLowerCase();
    const iBlockedHim = myBlockedUsers.has(partnerId);
    const heBlockedMe = currentChatInfo.isBlockedByPartner === true;

    if (iBlockedHim || heBlockedMe) {
        normalUi.classList.add('hidden');
        blockedUi.classList.remove('hidden');
        blockedUi.innerText = iBlockedHim
            ? "Вы заблокировали этого пользователя"
            : "Пользователь ограничил доступ";
    } else {
        normalUi.classList.remove('hidden');
        blockedUi.classList.add('hidden');
    }
}

function addBlockSystemMessage(blockerName, blockTime = null) {
    document.querySelectorAll('.system-block-message').forEach(el => el.remove());
    const div = document.createElement('div');
    div.className = 'mb-6 flex justify-center system-block-message';
    div.innerHTML = `
        <div class="bg-red-50 border border-red-200 text-red-800 px-6 py-4 rounded-2xl text-center max-w-sm">
            <i data-feather="lock" class="w-8 h-8 mx-auto mb-2"></i>
            <p class="font-semibold text-lg">${blockerName} добавил вас в чёрный список</p>
            ${blockTime ? `<p class="text-sm opacity-80 mt-1">${formatDateTime(blockTime)}</p>` : ''}
            <p class="text-sm mt-2 opacity-90">Вы не можете отправлять сообщения в этот чат</p>
        </div>
    `;
    messagesContainer.appendChild(div);
    feather.replace();
    scrollToBottom();
}

function blockChat(blockerName = "Этот пользователь", blockTime = null) {
    messageInput.disabled = true;
    messageInput.placeholder = "Вы в чёрном списке";
    messageInput.style.backgroundColor = "#f3f4f6";
    messageInput.style.color = "#6b7280";
    sendButton.disabled = true;
    sendButton.style.opacity = "0.4";
    fileInput.disabled = true;
    document.querySelector('label[for="file-input"]').style.opacity = "0.4";
    const subtitle = document.getElementById('chat-subtitle');
    if (subtitle) subtitle.textContent = 'Заблокировал вас';
    addBlockSystemMessage(blockerName, blockTime);
}

function blockedByPartner(blockerName = "Этот пользователь") {
    if (!messageInput || !sendButton) return;

    console.log("blockedByPartner → вас заблокировали:", blockerName);
    messageInput.disabled = true;
    messageInput.placeholder = "Отправка сообщений ограничена";
    messageInput.style.backgroundColor = "#fef2f2";
    messageInput.style.color = "#991b1b";
    sendButton.disabled = true;
    sendButton.style.opacity = "0.5";
    fileInput.disabled = true;
    document.querySelector('label[for="file-input"]').style.opacity = "0.5";

    document.querySelectorAll('.system-blocked-by-partner').forEach(el => el.remove());
    const div = document.createElement('div');
    div.className = 'mb-6 flex justify-center system-blocked-by-partner';
    div.innerHTML = `
        <div class="bg-red-50 border border-red-200 text-red-800 px-6 py-4 rounded-2xl text-center max-w-sm">
            <i data-feather="slash" class="w-8 h-8 mx-auto mb-2 text-red-600"></i>
            <p class="font-semibold text-lg">${blockerName} добавил вас в чёрный список</p>
            <p class="text-sm mt-2 opacity-90">Вы не можете отправлять сообщения в этот чат</p>
        </div>
    `;
    messagesContainer.appendChild(div);
    feather.replace();
    scrollToBottom();
}

function blockChatForMe(blockedName = "этот контакт") {
    if (!messageInput || !sendButton) return;

    messageInput.disabled = true;
    messageInput.placeholder = `Вы заблокировали ${blockedName}`;
    messageInput.style.backgroundColor = "#fef3f2";
    messageInput.style.color = "#991b1b";
    sendButton.disabled = true;
    sendButton.style.opacity = "0.5";
    fileInput.disabled = true;
    document.querySelector('label[for="file-input"]').style.opacity = "0.5";

    const subtitle = document.getElementById('chat-subtitle');
    if (subtitle) subtitle.textContent = 'Вы заблокировали этого пользователя';

    document.querySelectorAll('.system-myblock-message').forEach(el => el.remove());
    const div = document.createElement('div');
    div.className = 'mb-6 flex justify-center system-myblock-message';
    div.innerHTML = `
        <div class="bg-orange-50 border border-orange-200 text-orange-800 px-6 py-4 rounded-2xl text-center max-w-sm">
            <i data-feather="user-x" class="w-8 h-8 mx-auto mb-2"></i>
            <p class="font-semibold text-lg">Вы заблокировали ${blockedName}</p>
            <p class="text-sm mt-2 opacity-90">Отправка сообщений этому контакту невозможна</p>
        </div>
    `;
    messagesContainer.appendChild(div);
    feather.replace();
    scrollToBottom();
}

function unblockChat() {
    if (!messageInput || !sendButton) return;

    console.log("unblockChat → полная разблокировка");
    messageInput.disabled = false;
    messageInput.placeholder = "Напишите сообщение...";
    messageInput.style.backgroundColor = "";
    messageInput.style.color = "";
    sendButton.disabled = false;
    sendButton.style.opacity = "";
    fileInput.disabled = false;
    document.querySelector('label[for="file-input"]').style.opacity = "";

    document.querySelectorAll('.system-block-message, .system-myblock-message, .system-blocked-by-partner')
        .forEach(el => el.remove());

    const subtitle = document.getElementById('chat-subtitle');
    if (subtitle) subtitle.textContent = 'Активен недавно';

    scrollToBottom();
}

function applyBlockStatus() {
    if (!currentChatInfo || currentChatInfo.type !== 'private') {
        unblockChat();
        return;
    }
    const partner = currentChatInfo.participants.find(p =>
        (p.id || p.userId || "").toString().toLowerCase().trim() !== me
    );
    if (!partner) return;

    const partnerId = (partner.id || partner.userId || "").toString().toLowerCase().trim();
    const iBlockedHim = myBlockedUsers.has(partnerId);
    const heBlockedMe = !!currentChatInfo.isBlockedByPartner;

    if (iBlockedHim) blockChatForMe(partner.name || "этот пользователь");
    else if (heBlockedMe) blockedByPartner(partner.name || "этот пользователь");
    else unblockChat();
}