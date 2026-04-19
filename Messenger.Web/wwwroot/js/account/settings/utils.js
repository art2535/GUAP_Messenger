const API_URL = 'https://localhost:7001/api';

function getJwtToken() {
    const meta = document.querySelector('meta[name="access-token"]');
    return meta ? meta.content : '';
}

function getAuthToken() {
    let token = document.querySelector('meta[name="access-token"]')?.content?.trim();

    if (!token) {
        token = localStorage.getItem('token')?.trim();
    }

    if (!token) {
        console.warn("Токен авторизации не найден!");
        return null;
    }

    if (!token.startsWith('Bearer ')) {
        token = 'Bearer ' + token;
    }

    return token;
}

async function apiFetch(endpoint, options = {}) {
    const token = getAuthToken();
    if (!token) {
        showToast('Токен авторизации отсутствует', 'error');
        return;
    }

    const headers = {
        'Authorization': token,
        'Content-Type': 'application/json',
        ...options.headers
    };

    const response = await fetch(`${API_URL}${endpoint}`, { ...options, headers });

    if (!response.ok) {
        if (response.status === 401) {
            showToast('Сессия истекла. Пожалуйста, войдите заново.', 'error');
            return null;
        }

        const errorText = await response.text();
        throw new Error(errorText || `Ошибка: ${response.statusText}`);
    }

    return response.json();
}

function showToast(message, type = 'info', duration = 4000) {
    let toastContainer = document.getElementById('toast-container');

    if (!toastContainer) {
        toastContainer = document.createElement('div');
        toastContainer.id = 'toast-container';
        toastContainer.className = 'fixed top-4 right-4 z-[9999] flex flex-col gap-3';
        document.body.appendChild(toastContainer);
    }

    const toast = document.createElement('div');
    toast.className = `px-6 py-4 rounded-xl shadow-2xl text-white font-medium min-w-80 max-w-sm 
                       transform translate-x-full opacity-0 transition-all duration-500 ease-out 
                       flex items-center gap-3`;

    const colors = {
        success: 'bg-green-600',
        error: 'bg-red-600',
        info: 'bg-blue-600',
        warning: 'bg-orange-600'
    };

    toast.classList.add(colors[type] || 'bg-blue-600');

    toast.innerHTML = `
        <div class="flex-1">${message}</div>
        <button class="ml-4 text-white/70 hover:text-white">
            <i data-feather="x" class="w-5 h-5"></i>
        </button>
    `;

    toastContainer.insertBefore(toast, toastContainer.firstChild);
    feather.replace();

    requestAnimationFrame(() => {
        toast.classList.remove('translate-x-full', 'opacity-0');
    });

    const removeToast = () => {
        toast.classList.add('translate-x-full', 'opacity-0');
        toast.addEventListener('transitionend', () => toast.remove());
    };

    const timeoutId = setTimeout(removeToast, duration);

    toast.querySelector('button').onclick = () => {
        clearTimeout(timeoutId);
        removeToast();
    };
}

let confirmCallback = null;

function openConfirm(title, message) {
    document.getElementById('confirmTitle').textContent = title;
    document.getElementById('confirmMessage').textContent = message;
    document.getElementById('confirmModal').classList.add('active');

    return new Promise(resolve => confirmCallback = resolve);
}

function closeConfirm(result = false) {
    document.getElementById('confirmModal').classList.remove('active');
    if (confirmCallback) {
        confirmCallback(result);
        confirmCallback = null;
    }
}