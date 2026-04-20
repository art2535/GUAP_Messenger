async function fetchWithAuth(url, options = {}) {
    if (!token) {
        console.warn("Токен отсутствует");
        window.location.href = "/Authorization/Authorization";
        return null;
    }

    const headers = {
        'Authorization': `Bearer ${token}`,
        ...options.headers
    };

    try {
        const response = await fetch(url, { ...options, headers });

        if (response.status === 401 || response.status === 403) {
            console.warn('Токен недействителен — редирект на логин');
            window.location.href = "/Authorization/Authorization";
            return null;
        }

        return response;
    } catch (err) {
        console.error("Ошибка fetchWithAuth:", err);
        return null;
    }
}