let currentChatId = null;
let currentChatInfo = null;
let allChats = [];
let allChatItems = [];
let userStatuses = new Map();
let originalMessages = [];
let selectedFiles = [];
let pendingMessages = new Map();
let isSending = false;
let connection = null;

let messageInput = null;
let sendButton = null;
let fileInput = null;
let attachedFiles = null;
let messagesContainer = null;