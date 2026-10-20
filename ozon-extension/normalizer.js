(function (root) {
  "use strict";
  const marker = String.raw`\u001d`;
  const separator = String.raw`(?:\x1d|\t| +|\\u001[dD]|\[GS\]|<GS>|\{GS\}|#GS#|#29F|#29)?`;
  const complete = new RegExp(String.raw`^01(\d{14})21([!-~]{13})${separator}91([!-~]{4})${separator}92([A-Za-z0-9+/]{43}=)$`);
  const boundary = new RegExp(String.raw`^01\d{14}21[!-~]{13}(?:${separator}91[!-~]{4})?$`);
  function clean(raw) {
    const value = String(raw).replace(/^[\r\n]+|[\r\n]+$/g, "");
    return value.startsWith("]d2") ? value.slice(3) : value;
  }
  function normalize(raw) {
    const value = clean(raw);
    if (!value) return { ok: false, empty: true, reason: "Ожидаю код маркировки товара." };
    const parts = complete.exec(value);
    if (!parts) return { ok: false, reason: "Код не соответствует поддерживаемому формату. Проверьте английскую раскладку, Caps Lock и повторите сканирование." };
    const weighted = [...parts[1].slice(0, 13)].reduce((sum, n, i) => sum + Number(n) * (i % 2 === 0 ? 3 : 1), 0);
    if ((10 - weighted % 10) % 10 !== Number(parts[1][13])) {
      return { ok: false, reason: "Не совпадает контрольная цифра GTIN. Повторите сканирование с упаковки." };
    }
    const code = "01" + parts[1] + "21" + parts[2] + marker + "91" + parts[3] + marker + "92" + parts[4];
    return { ok: true, code, changed: code !== String(raw) };
  }
  const api = Object.freeze({ normalize, canInsertSeparator: raw => boundary.test(clean(raw)), looksLikeMarking: raw => /^(?:\]d2)?01\d{14}21/.test(String(raw)), marker });
  if (typeof module !== "undefined" && module.exports) module.exports = api;
  else Object.defineProperty(root, "EipOzonMarking", { value: api, configurable: true });
})(globalThis);
