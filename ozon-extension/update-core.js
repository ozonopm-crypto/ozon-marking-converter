(function (root) {
  "use strict";
  function httpsUrl(value) {
    const url = new URL(String(value));
    if (url.protocol !== "https:" || url.username || url.password || url.hash) throw new Error("Нужен прямой HTTPS-адрес без логина, пароля и фрагмента.");
    return url.href;
  }
  function parts(value) {
    if (typeof value !== "string" || !/^(?:0|[1-9]\d{0,4})(?:\.(?:0|[1-9]\d{0,4})){0,3}$/.test(value)) throw new Error("Неверный номер версии.");
    const result = value.split(".").map(Number);
    if (!result.some(Boolean) || result.some(n => n > 65535)) throw new Error("Неверный номер версии.");
    while (result.length < 4) result.push(0);
    return result;
  }
  function newer(candidate, current) {
    const a = parts(candidate), b = parts(current);
    for (let i = 0; i < 4; i++) { if (a[i] !== b[i]) return a[i] > b[i]; }
    return false;
  }
  function release(data, current, source) {
    if (!data || typeof data !== "object" || Array.isArray(data)) throw new Error("Неверный файл обновлений.");
    const version = String(data.version || ""); parts(version);
    return {
      available: newer(version, current), version,
      downloadURL: httpsUrl(data.download_url),
      notes: typeof data.notes === "string" ? data.notes.slice(0, 1200) : "",
      sourceURL: httpsUrl(source)
    };
  }
  const defaultURL = "https://raw.githubusercontent.com/ozonopm-crypto/ozon-marking-converter/main/updates.json";
  const api = Object.freeze({ httpsUrl, newer, release, defaultURL });
  if (typeof module !== "undefined" && module.exports) module.exports = api;
  else Object.defineProperty(root, "EipUpdateCore", { value: api, configurable: true });
})(globalThis);
