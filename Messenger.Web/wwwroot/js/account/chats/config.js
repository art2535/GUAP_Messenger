const API_BASE = 'https://localhost:7001/api';
const HUB_URL = 'https://localhost:7001/hubs/chat';

let token = localStorage.getItem('token') || '';
let me = localStorage.getItem('userId') || '';

if (!token) {
    console.warn("Токен не найден — редирект на логин");
    window.location.href = "/Authorization/Authorization";
}

console.log("✅ Токен загружен из localStorage:", token.substring(0, 25) + "...");
console.log("👤 me (userId):", me || "не установлен (будет обновлён позже)");