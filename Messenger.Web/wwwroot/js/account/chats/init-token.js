(function () {
    const tokenMeta = document.getElementById('access-token-meta');

    if (tokenMeta && tokenMeta.content && tokenMeta.content.length > 10) {
        localStorage.setItem('token', tokenMeta.content.trim());
        console.log("✅ Токен успешно сохранён в localStorage из meta-тега");

        const userIdMeta = document.getElementById('user-id-meta');
        if (userIdMeta && userIdMeta.content) {
            localStorage.setItem('userId', userIdMeta.content.trim());
            console.log("✅ User ID сохранён:", userIdMeta.content.trim());
        }
    }
    else {
        console.warn("⚠️ Токен не найден или пустой в meta-теге");

        const sessionToken = '@HttpContext.Session.GetString("ACCESS_TOKEN")';
        if (sessionToken && sessionToken.length > 10) {
            localStorage.setItem('token', sessionToken);
            console.log("✅ Токен сохранён из Session (fallback)");
        }
    }
})();