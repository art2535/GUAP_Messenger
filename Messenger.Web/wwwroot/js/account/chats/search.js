let searchDebounceTimer = null;
let currentSearchQuery = "";
let foundMessageElements = [];
let currentHighlightIndex = -1;

function getSearchNav() {
    return document.getElementById('search-navigation');
}

function toggleMessageSearch() {
    const searchBar = document.getElementById('message-search-bar');
    const searchInput = document.getElementById('message-search-input');
    if (searchBar.classList.contains('hidden')) {
        searchBar.classList.remove('hidden');
        searchBar.classList.add('flex');
        searchInput.focus();
    } else {
        clearMessageSearch(true);
    }
}

async function performMessageSearch(query) {
    query = (query || "").trim();
    currentSearchQuery = query;
    if (!query || !currentChatId) {
        resetHighlights();
        return;
    }

    try {
        const res = await fetch(`${API_BASE}/messages/${currentChatId}/search?query=${encodeURIComponent(query)}`, {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        if (res.ok) {
            const json = await res.json();
            processSearchResults(json.data || json || [], query);
            return;
        }
    } catch (e) {
        console.warn("Серверный поиск не удался", e);
    }

    const lowerQuery = query.toLowerCase();
    const results = originalMessages.filter(msg => {
        let text = msg.messageText || "";
        return text.toLowerCase().includes(lowerQuery);
    });
    processSearchResults(results, query);
}

function processSearchResults(results, query) {
    resetHighlights();
    if (results.length === 0) return;

    foundMessageElements = [];
    currentHighlightIndex = -1;

    results.forEach(resultMsg => {
        const elements = document.querySelectorAll(`[data-mid="${resultMsg.messageId}"]`);
        elements.forEach(el => {
            highlightMessage(el, query);
            foundMessageElements.push(el);
        });
    });

    if (foundMessageElements.length > 0) goToHighlight(0);
}

function highlightMessage(element, query) {
    const textEl = element.querySelector('p');
    if (!textEl) return;
    let html = textEl.innerHTML;
    const escapedQuery = query.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    const regex = new RegExp(`(${escapedQuery})`, 'gi');
    html = html.replace(regex, '<span class="bg-yellow-300 px-1.5 rounded">$1</span>');
    textEl.innerHTML = html;
}

function resetHighlights() {
    document.querySelectorAll('.bg-yellow-300').forEach(span => {
        const parent = span.parentElement;
        if (parent) parent.innerHTML = parent.innerHTML.replace(/<span class="bg-yellow-300 px-1.5 rounded">(.+?)<\/span>/gi, '$1');
    });
    foundMessageElements = [];
    currentHighlightIndex = -1;
    const nav = getSearchNav();
    if (nav) nav.classList.add('hidden');
    const counter = document.getElementById('search-counter');
    if (counter) counter.classList.add('hidden');
}

function goToHighlight(index) {
    if (index < 0 || index >= foundMessageElements.length) return;
    currentHighlightIndex = index;
    const target = foundMessageElements[index];
    if (target) target.scrollIntoView({ behavior: "smooth", block: "center" });
    updateNavigationUI();
}

function updateNavigationUI() {
    const nav = getSearchNav();
    if (!nav) return;
    const total = foundMessageElements.length;
    nav.classList.toggle('hidden', total <= 1);

    const counter = document.getElementById('search-counter');
    if (counter) {
        counter.classList.toggle('hidden', total <= 1);
        if (total > 0) {
            document.getElementById('current-count').textContent = currentHighlightIndex + 1;
            document.getElementById('total-count').textContent = total;
        }
    }
}

function navigateSearch(direction) {
    if (foundMessageElements.length === 0) return;
    let newIndex = currentHighlightIndex + direction;
    if (newIndex < 0) newIndex = foundMessageElements.length - 1;
    if (newIndex >= foundMessageElements.length) newIndex = 0;
    goToHighlight(newIndex);
}

function clearMessageSearch(hideBar = false) {
    clearTimeout(searchDebounceTimer);
    currentSearchQuery = "";
    const searchInput = document.getElementById('message-search-input');
    const searchBar = document.getElementById('message-search-bar');
    if (searchInput) searchInput.value = '';
    if (hideBar && searchBar) {
        searchBar.classList.add('hidden');
        searchBar.classList.remove('flex');
    }
    resetHighlights();
}