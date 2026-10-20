(function () {
  "use strict";
  const isDemo = location.protocol === "chrome-extension:" && location.pathname.endsWith("/check.html");
  if (location.hostname !== "seller.ozon.ru" && !isDemo) return;
  const api = globalThis.EipOzonMarking;
  if (!api || globalThis.__eipOzonMarkingAttached) return;
  globalThis.__eipOzonMarkingAttached = true;
  const version = typeof chrome !== "undefined" && chrome.runtime && chrome.runtime.getManifest ? chrome.runtime.getManifest().version : "1.2.1";
  let enabled = !(typeof chrome !== "undefined" && chrome.storage);
  let forceMode = true;
  let panelHidden = false;
  let selecting = false;
  let lastField = null;
  let updating = false;
  let statusHost = null;
  let statusText = null;
  let releaseBox = null;
  let releaseState = null;
  let convertedCount = 0;
  let refreshTimer = null;
  let mutationTimer = null;
  const pendingFields = new WeakMap();
  const attempts = new WeakMap();
  const manualFields = new WeakSet();
  const handledFields = new WeakSet();
  const markingLabel = /(?:код\s*маркировки|коды\s*маркировки|маркировочн|\bdatamatrix\b|честн(?:ый|ого)\s*[«"']?\s*знак|(?:^|[^а-яё])киз(?:$|[^а-яё]))/i;

  function editable(node) {
    if (!(node instanceof HTMLInputElement || node instanceof HTMLTextAreaElement)) return false;
    if (node.disabled || node.readOnly) return false;
    return !(node instanceof HTMLInputElement) || ["text", "search", "tel", "url", ""].includes(node.type);
  }
  function textOf(node) { return String(node && node.textContent || "").trim().slice(0, 150); }
  function isMarkingField(node) {
    if (!editable(node)) return false;
    if (manualFields.has(node) || (forceMode && api.looksLikeMarking(node.value))) return true;
    const labels = [node.getAttribute("aria-label"), node.getAttribute("placeholder"), node.getAttribute("title")];
    if (/marking|datamatrix|(?:^|[_-])(?:cis|kiz)(?:$|[_-])/i.test(String(node.id || "") + " " + String(node.name || ""))) return true;
    for (const label of Array.from(node.labels || [])) labels.push(textOf(label));
    for (const id of String(node.getAttribute("aria-labelledby") || "").split(/\s+/).filter(Boolean)) {
      labels.push(textOf(document.getElementById(id)));
    }
    let parent = node.parentElement;
    // Inspect only close, single-field wrappers to avoid enabling unrelated forms.
    for (let depth = 0; parent && depth < 6; depth++, parent = parent.parentElement) {
      const fields = parent.querySelectorAll("input:not([type=hidden]),textarea");
      if (fields.length !== 1) break;
      for (const child of Array.from(parent.children)) {
        if (child !== node && !child.contains(node) && textOf(child).length < 100) labels.push(textOf(child));
      }
    }
    return labels.some(label => markingLabel.test(String(label || "")));
  }
  function showStatus(message, error = false) {
    if (!document.body || panelHidden || !enabled) return;
    if (!statusHost || !statusHost.isConnected) {
      statusHost = document.createElement("div");
      statusHost.setAttribute("data-eip-ozon-status", "true");
      statusHost.style.cssText = "position:fixed;right:18px;bottom:18px;z-index:2147483646;max-width:calc(100vw - 36px);";
      const shadow = statusHost.attachShadow({ mode: "open" });
      const style = document.createElement("style");
      style.textContent = ":host{all:initial}section{box-sizing:border-box;width:340px;max-width:calc(100vw - 36px);padding:15px;background:#fff;color:#182335;border:1px solid #d4dce8;border-radius:12px;box-shadow:0 5px 24px #14203425;font:14px/1.45 system-ui,Arial}strong{display:block;margin-bottom:6px}p{margin:0;overflow-wrap:anywhere}p[data-error=true]{color:#a12a1c}.actions{display:flex;flex-wrap:wrap;gap:8px;margin-top:10px}button{padding:7px 10px;background:#f3f6fa;color:#182335;border:1px solid #ccd5e1;border-radius:7px;font:14px system-ui;cursor:pointer}button:focus-visible{outline:3px solid #7aaeff;outline-offset:2px}";
      const section = document.createElement("section");
      const title = document.createElement("strong"); title.textContent = "Преобразователь кода для Ozon";
      statusText = document.createElement("p"); statusText.setAttribute("role", "status"); statusText.setAttribute("aria-live", "polite");
      const actions = document.createElement("div"); actions.className = "actions";
      const clear = document.createElement("button"); clear.type = "button"; clear.textContent = "Стереть код";
      clear.addEventListener("click", () => {
        if (!lastField || !lastField.isConnected || !editable(lastField)) return;
        attempts.delete(lastField); handledFields.delete(lastField);
        setValue(lastField, ""); lastField.focus();
        showStatus("Поле очищено. Можно сканировать следующий товар.");
      });
      const hide = document.createElement("button"); hide.type = "button"; hide.textContent = "Скрыть";
      hide.addEventListener("click", () => { panelHidden = true; statusHost.remove(); statusHost = null; });
      releaseBox = document.createElement("p");
      releaseBox.style.cssText = "margin-top:12px;padding-top:10px;border-top:1px solid #d4dce8;";
      actions.append(clear, hide); section.append(title, statusText, actions, releaseBox); shadow.append(style, section); document.body.append(statusHost);
      renderRelease();
    }
    if (statusText.textContent !== message) statusText.textContent = message;
    if (statusText.dataset.error !== String(error)) statusText.dataset.error = String(error);
  }
  function renderRelease() {
    if (!releaseBox) return;
    releaseBox.replaceChildren(); releaseBox.hidden = true;
    if (!releaseState || !releaseState.available || !releaseState.version) return;
    let linkURL;
    try { linkURL = new URL(releaseState.downloadURL); if (linkURL.protocol !== "https:" || linkURL.username || linkURL.password) return; } catch (_) { return; }
    const label = document.createElement("strong"); label.textContent = "Доступна новая версия " + releaseState.version;
    const link = document.createElement("a"); link.textContent = "Скачать обновление";
    link.href = linkURL.href; link.target = "_blank"; link.rel = "noopener noreferrer";
    const note = document.createElement("p"); note.textContent = "После скачивания замените файлы в прежней папке, обновите расширение и вкладку Ozon.";
    releaseBox.append(label, link, note); releaseBox.hidden = false;
  }
  function stop(event) { event.preventDefault(); event.stopImmediatePropagation(); }
  function setValue(field, value) {
    const prototype = field instanceof HTMLTextAreaElement ? HTMLTextAreaElement.prototype : HTMLInputElement.prototype;
    const setter = Object.getOwnPropertyDescriptor(prototype, "value").set;
    const previous = updating;
    updating = true;
    try {
      setter.call(field, value);
      field.dispatchEvent(new InputEvent("input", { bubbles: true, composed: true, inputType: "insertReplacementText", data: null }));
    } finally { updating = previous; }
  }
  function fix(field, announceFailure = false) {
    if (!enabled || updating || !isMarkingField(field)) return false;
    lastField = field;
    const result = api.normalize(field.value);
    if (!result.ok) {
      handledFields.delete(field);
      if (announceFailure) showStatus(result.reason, !result.empty);
      return false;
    }
    if (!handledFields.has(field)) { convertedCount++; handledFields.add(field); }
    if (result.changed) {
      const previousAttempt = attempts.get(field);
      // Ozon may convert escaped separators to real GS in its own state.
      if (previousAttempt && previousAttempt.code === result.code && field.value.includes("\x1d")) return true;
      const count = previousAttempt && previousAttempt.code === result.code ? previousAttempt.count : 0;
      if (count >= 3) {
        if (announceFailure) showStatus("Ozon повторно изменяет введённый код. Обновите страницу и проверьте, что другое расширение не обрабатывает маркировку.", true);
        return false;
      }
      attempts.set(field, { code: result.code, count: count + 1 });
      const end = field.selectionStart === field.value.length;
      setValue(field, result.code);
      if (end) field.setSelectionRange(result.code.length, result.code.length);
    }
    showStatus("Разделители подготовлены. Проверьте результат проверки Ozon и подтвердите ввод вручную.");
    return true;
  }
  function insertMarker(field) {
    const start = field.selectionStart == null ? field.value.length : field.selectionStart;
    const end = field.selectionEnd == null ? start : field.selectionEnd;
    const value = field.value.slice(0, start) + api.marker + field.value.slice(end);
    setValue(field, value);
    const position = start + api.marker.length;
    field.setSelectionRange(position, position);
  }
  function schedule(field) {
    clearTimeout(pendingFields.get(field));
    pendingFields.set(field, setTimeout(() => {
      pendingFields.delete(field);
      if (field.isConnected) fix(field);
    }, 0));
  }
  window.addEventListener("keydown", event => {
    const field = event.target;
    if (!enabled || selecting || event.isComposing || !isMarkingField(field)) return;
    const substitute = event.key === "F8" || (event.ctrlKey && !event.altKey && !event.metaKey && (event.code === "BracketRight" || event.key === "]")) || (event.key === "Tab" && !event.shiftKey && !event.ctrlKey && !event.altKey);
    const cursorAtEnd = field.selectionStart === field.value.length && field.selectionEnd === field.value.length;
    if (substitute && cursorAtEnd && api.canInsertSeparator(field.value)) {
      stop(event); lastField = field; insertMarker(field);
    } else if (event.key === "Enter" && !event.shiftKey && !event.ctrlKey && !event.altKey && api.looksLikeMarking(field.value)) {
      stop(event); fix(field, true);
    }
  }, true);
  window.addEventListener("input", event => {
    if (updating || !enabled || !isMarkingField(event.target)) return;
    const field = event.target;
    handledFields.delete(field);
    attempts.delete(field);
    schedule(field);
    clearTimeout(refreshTimer);
    if (api.looksLikeMarking(field.value)) refreshTimer = setTimeout(() => {
      if (enabled && field.isConnected && document.activeElement === field) fix(field, true);
    }, 700);
  }, true);
  window.addEventListener("paste", event => {
    const field = event.target;
    if (!enabled || !editable(field) || !event.clipboardData) return;
    const text = event.clipboardData.getData("text/plain");
    const start = field.selectionStart == null ? field.value.length : field.selectionStart;
    const end = field.selectionEnd == null ? start : field.selectionEnd;
    const candidate = field.value.slice(0, start) + text + field.value.slice(end);
    if (!isMarkingField(field) && !(forceMode && api.looksLikeMarking(candidate))) return;
    const result = api.normalize(candidate);
    if (!result.ok) return;
    stop(event); handledFields.delete(field); attempts.delete(field); lastField = field;
    setValue(field, result.code); field.setSelectionRange(result.code.length, result.code.length); fix(field);
  }, true);
  window.addEventListener("focusin", event => {
    if (enabled && isMarkingField(event.target)) {
      lastField = event.target;
      showStatus("Поле маркировки распознано. Английская раскладка (ENG), Caps Lock выключен.");
    }
  }, true);
  window.addEventListener("change", event => {
    if (!updating && enabled && isMarkingField(event.target)) schedule(event.target);
  }, true);
  window.addEventListener("click", event => {
    if (!selecting || !enabled || !editable(event.target)) return;
    stop(event); selecting = false; manualFields.add(event.target); lastField = event.target;
    lastField.focus(); showStatus("Поле выбрано. Отсканируйте код товара. Для нового окна поле может потребоваться выбрать повторно.");
  }, true);
  window.addEventListener("keydown", event => {
    if (selecting && event.key === "Escape") { selecting = false; showStatus("Выбор поля отменён."); }
  }, true);

  function readyMessage() {
    return forceMode ? "Принудительный режим включён. Ожидаю код маркировки товара." : "Расширение включено. Откройте поле кода маркировки товара.";
  }
  function sweep() {
    if (!enabled || !document.body || document.hidden) return;
    if (!panelHidden && (!statusHost || !statusHost.isConnected)) showStatus(readyMessage());
    const fields = new Set([document.activeElement, lastField]);
    for (const field of Array.from(document.querySelectorAll("input,textarea")).slice(0, 200)) {
      if (editable(field) && api.looksLikeMarking(field.value)) fields.add(field);
    }
    for (const field of fields) if (field && field.isConnected && editable(field) && field.value) fix(field);
  }
  function ready() {
    sweep();
    if (typeof MutationObserver !== "undefined" && document.documentElement) {
      const observer = new MutationObserver(() => {
        clearTimeout(mutationTimer);
        mutationTimer = setTimeout(sweep, 80);
      });
      observer.observe(document.documentElement, { childList: true, subtree: true });
    }
  }
  if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", ready, { once: true });
  else ready();
  window.addEventListener("pageshow", sweep);
  window.addEventListener("focus", sweep);
  document.addEventListener("visibilitychange", sweep);
  setInterval(sweep, 800);

  if (typeof chrome !== "undefined" && chrome.storage) {
    chrome.storage.local.get({ eipMarkingEnabled: true, eipMarkingForce: true, eipReleaseState: null }, settings => {
      enabled = settings.eipMarkingEnabled !== false;
      forceMode = settings.eipMarkingForce !== false;
      releaseState = settings.eipReleaseState;
      if (enabled) sweep();
      else if (statusHost) { statusHost.remove(); statusHost = null; }
      renderRelease();
    });
    chrome.storage.onChanged.addListener((changes, area) => {
      if (area !== "local") return;
      if (changes.eipReleaseState) { releaseState = changes.eipReleaseState.newValue; renderRelease(); }
      if (!changes.eipMarkingEnabled && !changes.eipMarkingForce) return;
      if (changes.eipMarkingForce) forceMode = changes.eipMarkingForce.newValue !== false;
      if (changes.eipMarkingEnabled) enabled = changes.eipMarkingEnabled.newValue !== false;
      selecting = false;
      if (!enabled && statusHost) { statusHost.remove(); statusHost = null; }
      if (enabled) { panelHidden = false; showStatus(readyMessage()); sweep(); }
    });
  }
  if (typeof chrome !== "undefined" && chrome.runtime && chrome.runtime.onMessage) {
    chrome.runtime.onMessage.addListener((message, sender, respond) => {
      if (message.type === "eip-status") respond({ version, enabled, forceMode, convertedCount, selecting });
      else if (message.type === "eip-apply-now") {
        panelHidden = false;
        attempts.delete(document.activeElement);
        if (lastField) attempts.delete(lastField);
        sweep(); respond({ ok: enabled, version });
      }
      else if (message.type === "eip-select-field") {
        if (!enabled) { respond({ ok: false }); return; }
        selecting = true; panelHidden = false;
        showStatus("Нажмите на поле кода маркировки. Для отмены нажмите Esc."); respond({ ok: true });
      }
    });
  }
})();
