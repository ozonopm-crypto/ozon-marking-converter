"use strict";
const updateURL = document.getElementById("update-url");
const updateStatus = document.getElementById("update-status");
const updateLink = document.getElementById("download-update");
const updateNotes = document.getElementById("update-notes");
function renderUpdate(state, configured) {
  updateLink.hidden = true; updateLink.removeAttribute("href"); updateNotes.textContent = "";
  if (!configured) { updateStatus.textContent = "Адрес обновлений ещё не задан. Автоматическая проверка пока не включена."; return; }
  if (state && state.available) {
    try {
      updateLink.href = EipUpdateCore.httpsUrl(state.downloadURL); updateLink.hidden = false;
      updateStatus.textContent = "Доступна новая версия " + state.version + ".";
      updateNotes.textContent = state.notes || "";
      return;
    } catch (_) {}
  }
  updateStatus.textContent = state && state.error ? "Проверка не удалась: " + state.error : state && state.checkedAt ? "Новых версий не найдено." : "Адрес задан. Можно проверить обновления.";
}
function loadUpdate() {
  chrome.storage.local.get({ eipUpdateURL: EipUpdateCore.defaultURL, eipReleaseState: null }, config => {
    updateURL.value = config.eipUpdateURL;
    renderUpdate(config.eipReleaseState, Boolean(config.eipUpdateURL));
  });
}
function checkUpdate() {
  updateStatus.textContent = "Проверяю обновления…";
  chrome.runtime.sendMessage({ type: "eip-check-update" }, response => {
    if (chrome.runtime.lastError) { updateStatus.textContent = "Не удалось запустить проверку. Закройте и откройте окно расширения."; return; }
    renderUpdate(response, response && response.configured !== false);
  });
}
document.getElementById("save-update").addEventListener("click", () => {
  const entered = updateURL.value.trim();
  if (!entered) {
    chrome.storage.local.set({ eipUpdateURL: "", eipReleaseState: null }, loadUpdate); return;
  }
  let address;
  try { address = EipUpdateCore.httpsUrl(entered); } catch (error) { updateStatus.textContent = error.message; return; }
  // The permission request stays inside the user's explicit save action.
  chrome.permissions.request({ origins: [new URL(address).origin + "/*"] }, granted => {
    if (chrome.runtime.lastError || !granted) { updateStatus.textContent = "Доступ к адресу обновлений не разрешён. Настройка не сохранена."; return; }
    chrome.storage.local.set({ eipUpdateURL: address, eipReleaseState: null }, () => { updateURL.value = address; checkUpdate(); });
  });
});
document.getElementById("check-update").addEventListener("click", checkUpdate);
chrome.storage.onChanged.addListener((changes, area) => {
  if (area === "local" && changes.eipReleaseState) {
    renderUpdate(changes.eipReleaseState.newValue, Boolean(updateURL.value.trim()));
  }
});
loadUpdate();
