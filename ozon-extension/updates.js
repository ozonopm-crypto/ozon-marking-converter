(function () {
  "use strict";
  const core = globalThis.EipUpdateCore;
  const alarmName = "eip-release-check";
  const interval = 6 * 60 * 60 * 1000;
  let pending = null;
  const settings = () => new Promise(resolve => chrome.storage.local.get({ eipUpdateURL: core.defaultURL, eipReleaseState: null }, resolve));
  const save = state => new Promise(resolve => chrome.storage.local.set({ eipReleaseState: state }, resolve));
  function badge(state) {
    const available = Boolean(state && state.available);
    chrome.action.setBadgeText({ text: available ? "NEW" : "" });
    if (available) chrome.action.setBadgeBackgroundColor({ color: "#175cd3" });
  }
  function hasPermission(url) {
    return new Promise(resolve => chrome.permissions.contains({ origins: [new URL(url).origin + "/*"] }, resolve));
  }
  async function perform(force) {
    const config = await settings();
    if (!config.eipUpdateURL) { badge(null); return { configured: false }; }
    const source = core.httpsUrl(config.eipUpdateURL);
    const previous = config.eipReleaseState && config.eipReleaseState.sourceURL === source ? config.eipReleaseState : null;
    if (!force && previous && Date.now() - previous.checkedAt < interval) { badge(previous); return previous; }
    let state;
    const controller = new AbortController();
    const timeout = setTimeout(() => controller.abort(), 12000);
    try {
      if (!await hasPermission(source)) throw new Error("Разрешите доступ к адресу обновлений в настройках расширения.");
      const response = await fetch(source, { method: "GET", cache: "no-store", credentials: "omit", referrerPolicy: "no-referrer", redirect: "error", signal: controller.signal });
      if (!response.ok) throw new Error("Сервер обновлений вернул HTTP " + response.status + ".");
      const text = await response.text();
      if (text.length > 32768) throw new Error("Файл обновлений слишком большой.");
      state = { ...core.release(JSON.parse(text), chrome.runtime.getManifest().version, source), checkedAt: Date.now(), error: "" };
    } catch (error) {
      state = { ...(previous || { available: false }), sourceURL: source, checkedAt: Date.now(), error: error && error.message ? error.message : "Не удалось проверить обновления." };
    } finally { clearTimeout(timeout); }
    const latest = await settings();
    if (latest.eipUpdateURL !== source) return { ignored: true };
    await save(state); badge(state); return state;
  }
  function check(force = false) {
    if (!pending) pending = perform(force).catch(error => ({ error: String(error.message || error) })).finally(() => { pending = null; });
    return pending;
  }
  async function initialize(force = false) {
    const config = await settings();
    if (!config.eipUpdateURL) { chrome.alarms.clear(alarmName); badge(null); return; }
    chrome.alarms.get(alarmName, alarm => {
      if (!alarm) chrome.alarms.create(alarmName, { delayInMinutes: 1, periodInMinutes: 360 });
    });
    // An installed update must immediately clear an obsolete notification.
    if (config.eipReleaseState && config.eipReleaseState.version && !core.newer(config.eipReleaseState.version, chrome.runtime.getManifest().version)) {
      await save({ ...config.eipReleaseState, available: false }); badge(null);
    } else badge(config.eipReleaseState);
    await check(force);
  }
  chrome.alarms.onAlarm.addListener(alarm => { if (alarm.name === alarmName) void check(false); });
  chrome.runtime.onStartup.addListener(() => { void initialize(true).catch(() => {}); });
  chrome.runtime.onInstalled.addListener(() => { void initialize(true).catch(() => {}); });
  chrome.storage.onChanged.addListener((changes, area) => {
    if (area === "local" && changes.eipUpdateURL) {
      void initialize(true).catch(() => {});
    }
  });
  chrome.runtime.onMessage.addListener((message, sender, respond) => {
    if (message.type !== "eip-check-update") return;
    check(true).then(respond); return true;
  });
  void initialize().catch(() => {});
})();
