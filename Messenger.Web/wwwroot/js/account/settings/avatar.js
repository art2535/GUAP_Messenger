let currentAvatarElement;
let hasOriginalAvatar = false;

document.addEventListener('DOMContentLoaded', () => {
    currentAvatarElement = document.getElementById('current-avatar');
    hasOriginalAvatar = '@hasAvatar'.toLowerCase() === 'true';

    const avatarInput = document.getElementById('avatarInput');
    const previewContainer = document.getElementById('avatar-preview-container');
    const previewImg = document.getElementById('avatar-preview');
    const cancelPreviewBtn = document.getElementById('cancel-preview');
    const deleteAvatarTrigger = document.getElementById('delete-avatar-trigger');
    const deleteAvatarFlag = document.getElementById('delete-avatar-flag');

    avatarInput.addEventListener('change', function (e) {
        const file = e.target.files[0];
        if (!file) return hidePreview();

        if (!file.type.startsWith('image/') || file.size > 2 * 1024 * 1024) {
            showToast('Выберите изображение до 2 МБ', 'error');
            this.value = '';
            return;
        }

        const reader = new FileReader();
        reader.onload = (ev) => {
            previewImg.src = ev.target.result;
            previewContainer.classList.remove('hidden');
            cancelPreviewBtn.classList.remove('hidden');
            deleteAvatarTrigger.classList.add('hidden');
        };
        reader.readAsDataURL(file);
    });

    window.hidePreview = function () {
        previewContainer.classList.add('hidden');
        cancelPreviewBtn.classList.add('hidden');
        if (hasOriginalAvatar) {
            deleteAvatarTrigger.classList.remove('hidden');
        }
    };

    if (cancelPreviewBtn) {
        cancelPreviewBtn.onclick = () => {
            avatarInput.value = '';
            hidePreview();
        };
    }

    deleteAvatarTrigger.onclick = async () => {
        if (!confirm('Удалить аватар?')) return;

        deleteAvatarFlag.value = 'true';
        updateAvatarDisplay(null);
        hidePreview();
        showToast('Аватар будет удалён после сохранения', 'info');
    };
});

function updateAvatarDisplay(avatarUrl) {
    if (avatarUrl && avatarUrl.trim() !== '') {
        let imgElement = currentAvatarElement;

        if (imgElement.tagName !== 'IMG') {
            imgElement = document.createElement('img');
            imgElement.id = 'current-avatar';
            imgElement.className = 'w-40 h-40 rounded-full object-cover border-4 border-gray-200 shadow-xl';
            currentAvatarElement.replaceWith(imgElement);
            currentAvatarElement = imgElement;
        }

        currentAvatarElement.src = `${avatarUrl}?t=${new Date().getTime()}`;
        currentAvatarElement.style.display = 'block';

    } else {
        let fallbackDiv = currentAvatarElement;

        if (fallbackDiv.tagName === 'IMG') {
            fallbackDiv = document.createElement('div');
            fallbackDiv.id = 'current-avatar';
            fallbackDiv.className = 'guap-gradient w-40 h-40 rounded-full flex items-center justify-center shadow-xl';
            currentAvatarElement.replaceWith(fallbackDiv);
            currentAvatarElement = fallbackDiv;
        }

        fallbackDiv.innerHTML = `<i data-feather="user" class="w-20 h-20 text-white"></i>`;

        feather.replace();
    }
}