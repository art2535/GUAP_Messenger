(function () {
    const tokenMeta = document.getElementById('access-token-meta');
    if (tokenMeta && tokenMeta.content) {
        localStorage.setItem('token', tokenMeta.content);
        console.log("✅ Токен успешно сохранён в localStorage (из meta)");
    } else {
        console.warn("⚠️ Токен не найден в meta-теге");
    }
})();