document.addEventListener("DOMContentLoaded", async () => {
    console.log("🚀 GUAP Messenger — запуск");

    feather.replace();

    await loadCurrentUser();

    messageInput = document.getElementById('message-input');
    sendButton = document.getElementById('send-button');
    fileInput = document.getElementById('file-input');
    attachedFiles = document.getElementById('attached-files');
    messagesContainer = document.getElementById('messages-container');

    loadAvatar();
    loadChats();
    startSignalR();

    document.getElementById('empty-right-panel').classList.remove('hidden');
    document.getElementById('chat-header').classList.add('hidden');
    document.getElementById('messages-container').classList.add('hidden');
    document.getElementById('chat-input-area').classList.add('hidden');

    if (sendButton && messageInput) {
        sendButton.onclick = sendMessage;

        messageInput.addEventListener('keydown', e => {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                sendMessage();
            }
        });
    }

    if (fileInput && attachedFiles) {
        fileInput.onchange = function () {
            const MAX_SIZE_MB = 10;
            const MAX_SIZE_BYTES = MAX_SIZE_MB * 1024 * 1024;
            const validFiles = [];
            let hasError = false;

            for (let file of this.files) {
                if (file.size > MAX_SIZE_BYTES) {
                    showToast(`Файл "${file.name}" слишком большой. Максимум: ${MAX_SIZE_MB} МБ`, 'error');
                    hasError = true;
                    continue;
                }
                validFiles.push(file);
            }

            if (hasError && validFiles.length === 0) {
                this.value = '';
                selectedFiles = [];
                attachedFiles.innerHTML = '';
                return;
            }

            selectedFiles = validFiles;
            renderAttachedPreviews();

            if (hasError && validFiles.length > 0) {
                showToast(`Некоторые файлы превышают лимит 10 МБ`, 'warning');
            }
        };
    }

    const chatSearch = document.getElementById('chat-search');
    const clearSearch = document.getElementById('clear-search');
    if (chatSearch) chatSearch.addEventListener('input', filterChats);
    if (clearSearch) {
        clearSearch.addEventListener('click', () => {
            chatSearch.value = '';
            chatSearch.focus();
            filterChats();
        });
    }

    const observer = new MutationObserver(() => feather.replace());
    observer.observe(document.body, { childList: true, subtree: true });

    console.log("✅ Инициализация завершена");
});

document.addEventListener('keydown', function (e) {
    if (e.key !== 'Escape') return;

    const createModal = document.getElementById('create-chat-modal');
    const infoModal = document.getElementById('chat-info-modal');
    const lightbox = document.getElementById('lightbox');
    const deleteOverlay = document.getElementById('delete-confirm-overlay');

    if (deleteOverlay) {
        deleteOverlay.remove();
        return;
    }
    if (infoModal && infoModal.classList.contains('show')) {
        infoModal.classList.remove('show');
        return;
    }
    if (createModal && createModal.classList.contains('show')) {
        createModal.classList.remove('show');
        return;
    }
    if (lightbox && lightbox.classList.contains('show')) {
        lightbox.classList.remove('show');
        const lightboxImg = document.getElementById('lightbox-img');
        if (lightboxImg) lightboxImg.src = '';
        return;
    }

    if (currentChatId) {
        closeCurrentChat();
    }
});

const messageSearchInput = document.getElementById('message-search-input');
if (messageSearchInput) {
    messageSearchInput.addEventListener('input', (e) => {
        const value = e.target.value.trim();
        if (value === "") {
            resetHighlights();
        } else {
            clearTimeout(searchDebounceTimer);
            searchDebounceTimer = setTimeout(() => {
                performMessageSearch(value);
            }, 300);
        }
    });

    messageSearchInput.addEventListener('keydown', (e) => {
        if (e.key === 'Escape') {
            e.preventDefault();
            clearMessageSearch(true);
        }
    });
}