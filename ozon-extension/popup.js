"use strict";
const version = chrome.runtime.getManifest().version;
document.getElementById("installed-version").textContent = version;
const enabledBox = document.getElementById("enabled");
const forceBox = document.getElementById("force");
const selectButton = document.getElementById("select");
const applyButton = document.getElementById("apply");
const status = document.getElementById("status");
let activeId = null;
let canInject = false;
function injectHandler(allFrames, done) {
  const target = allFrames ? { tabId: activeId, allFrames: true } : { tabId: activeId };
  chrome.scripting.executeScript({ target, files: ["normalizer.js", "content.js"] }, () => {
    const error = chrome.runtime.lastError;
    if (error && allFrames) { injectHandler(false, done); return; }
    done(error);
  });
}
function display(response) {
  if (!response || response.version !== version) {
    selectButton.disabled = true; applyButton.disabled = true;
    status.textContent = "На вкладке осталась прежняя версия. Отключите старое расширение и обновите страницу Ozon клавишами Ctrl+R.";
    return;
  }
  selectButton.disabled = !enabledBox.checked; applyButton.disabled = !enabledBox.checked;
  status.textContent = enabledBox.checked ? (response.forceMode ? "Версия " + version + " подключена. Принудительная обработка включена." : "Версия " + version + " подключена. Обрабатываются распознанные поля маркировки.") : "Преобразование выключено.";
}
function connect(retry = true) {
  if (activeId == null) return;
  chrome.tabs.sendMessage(activeId, { type: "eip-status" }, response => {
    const error = chrome.runtime.lastError;
    if (!error && response) { display(response); return; }
    if (retry && canInject && chrome.scripting) {
      injectHandler(true, injectionError => {
        if (injectionError) {
          status.textContent = "Браузер не разрешил запуск на этой странице. Проверьте доступ расширения к seller.ozon.ru и обновите вкладку.";
          return;
        }
        connect(false);
      });
      return;
    }
    selectButton.disabled = true; applyButton.disabled = true;
    status.textContent = "Откройте seller.ozon.ru и обновите вкладку. Проверьте, что браузер разрешает расширению доступ к этому сайту.";
  });
}
function refresh() {
  selectButton.disabled = true; applyButton.disabled = true;
  chrome.tabs.query({ active: true, currentWindow: true }, tabs => {
    const error = chrome.runtime.lastError;
    if (error || !tabs.length) { status.textContent = "Откройте кабинет Ozon Seller."; return; }
    activeId = tabs[0].id;
    try { canInject = new URL(tabs[0].url).origin === "https://seller.ozon.ru"; } catch (_) { canInject = false; }
    connect();
  });
}
chrome.storage.local.get({ eipMarkingEnabled: true, eipMarkingForce: true }, settings => {
  enabledBox.checked = settings.eipMarkingEnabled !== false;
  forceBox.checked = settings.eipMarkingForce !== false;
  refresh();
});
enabledBox.addEventListener("change", () => {
  selectButton.disabled = true;
  chrome.storage.local.set({ eipMarkingEnabled: enabledBox.checked }, refresh);
});
forceBox.addEventListener("change", () => chrome.storage.local.set({ eipMarkingForce: forceBox.checked }, refresh));
selectButton.addEventListener("click", () => {
  if (activeId == null) return;
  chrome.tabs.sendMessage(activeId, { type: "eip-select-field" }, response => {
    const error = chrome.runtime.lastError;
    if (error || !response || !response.ok) { status.textContent = "Не удалось выбрать поле. Обновите страницу Ozon и попробуйте снова."; return; }
    window.close();
  });
});
document.getElementById("check").addEventListener("click", () => {
  chrome.tabs.create({ url: chrome.runtime.getURL("check.html") });
});
applyButton.addEventListener("click", () => {
  if (activeId == null) return;
  chrome.tabs.sendMessage(activeId, { type: "eip-apply-now" }, response => {
    if (chrome.runtime.lastError || !response || !response.ok) { refresh(); return; }
    status.textContent = "Проверка полей выполнена. Посмотрите сообщения преобразователя и Ozon на странице.";
  });
});
