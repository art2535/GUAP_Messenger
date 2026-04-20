function safeChatId(chatId) {
    if (!chatId || chatId === "null" || chatId === "undefined" || chatId.trim() === "") {
        console.error("Получен некорректный chatId:", chatId);
        return null;
    }
    return chatId;
}

function formatDateTime(dateValue) {
    if (!dateValue) return '—';
    let date;
    try {
        if (typeof dateValue === 'string') {
            let str = dateValue.trim();
            if (!str.endsWith('Z') && !str.includes('+') && !str.includes('-', 10)) str += 'Z';
            date = new Date(str);
        } else {
            date = new Date(dateValue);
        }
        if (isNaN(date.getTime())) {
            console.warn("Не удалось распарсить дату:", dateValue);
            return '—';
        }
        const day = String(date.getDate()).padStart(2, '0');
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const year = date.getFullYear();
        const hours = String(date.getHours()).padStart(2, '0');
        const minutes = String(date.getMinutes()).padStart(2, '0');
        return `${day}.${month}.${year} ${hours}:${minutes}`;
    } catch (e) {
        console.error("Ошибка форматирования даты:", dateValue, e);
        return '—';
    }
}

function formatLastSeen(isoString) {
    if (!isoString) return "давно";
    const date = new Date(isoString);
    const now = new Date();
    const diffMs = now - date;
    const diffMinutes = Math.floor(diffMs / 60000);

    if (diffMinutes < 360) {
        if (diffMinutes < 1) return "только что";
        if (diffMinutes < 60) return `${diffMinutes} мин назад`;
        return `${Math.floor(diffMinutes / 60)} ч назад`;
    }

    const day = String(date.getDate()).padStart(2, '0');
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const year = date.getFullYear();
    const hours = String(date.getHours()).padStart(2, '0');
    const minutes = String(date.getMinutes()).padStart(2, '0');

    return year === now.getFullYear()
        ? `${day}.${month} в ${hours}:${minutes}`
        : `${day}.${month}.${year} в ${hours}:${minutes}`;
}

function showToast(message, type = 'info', duration = 4000) {
    let toastContainer = document.getElementById('toast-container');
    if (!toastContainer) {
        toastContainer = document.createElement('div');
        toastContainer.id = 'toast-container';
        toastContainer.className = 'fixed top-4 right-4 z-50 flex flex-col gap-3';
        document.body.appendChild(toastContainer);
    }

    const toast = document.createElement('div');
    const colors = {
        success: 'bg-green-600',
        error: 'bg-red-600',
        info: 'bg-blue-600',
        warning: 'bg-orange-600'
    };
    toast.className = `px-6 py-4 rounded-xl shadow-2xl text-white font-medium min-w-80 max-w-sm transform translate-x-full opacity-0 transition-all duration-500 ease-out flex items-center gap-3 ${colors[type] || 'bg-blue-600'}`;

    toast.innerHTML = `
        <div class="flex-1">${message}</div>
        <button class="ml-4 text-white/70 hover:text-white">
            <i data-feather="x" class="w-5 h-5"></i>
        </button>
    `;

    toastContainer.insertBefore(toast, toastContainer.firstChild);

    requestAnimationFrame(() => {
        toast.classList.remove('translate-x-full', 'opacity-0');
    });

    const timeoutId = setTimeout(() => {
        toast.style.transform = 'translateX(120%)';
        toast.style.opacity = '0';
        toast.addEventListener('transitionend', () => toast.remove());
    }, duration);

    toast.querySelector('button').addEventListener('click', (e) => {
        e.stopPropagation();
        clearTimeout(timeoutId);
        toast.style.transform = 'translateX(120%)';
        toast.style.opacity = '0';
        toast.addEventListener('transitionend', () => toast.remove());
    });

    feather.replace();
}

function openLightbox(url) {
    const lightbox = document.getElementById('lightbox');
    const lightboxImg = document.getElementById('lightbox-img');
    lightboxImg.src = url;
    lightbox.classList.add('show');
}

function scrollToBottom() {
    const container = document.getElementById('messages-container');
    if (!container) return;
    container.scrollTop = container.scrollHeight;
    requestAnimationFrame(() => {
        container.scrollTop = container.scrollHeight;
    });
}

function decodeHtml(html) {
    const txt = document.createElement("textarea");
    txt.innerHTML = html;
    return txt.value;
}