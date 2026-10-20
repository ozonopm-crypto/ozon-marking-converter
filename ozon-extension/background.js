"use strict";
if (typeof importScripts === "function") importScripts("update-core.js", "updates.js");
function ensureOzonTab(tabId) {
  chrome.scripting.executeScript({
    target: { tabId, allFrames: true },
    files: ["normalizer.js", "content.js"]
  }, () => {
    if (!chrome.runtime.lastError) return;
    // A third-party frame must not prevent recovery of the main Ozon page.
    chrome.scripting.executeScript({ target: { tabId }, files: ["normalizer.js", "content.js"] }, () => { void chrome.runtime.lastError; });
  });
}
function restoreOpenTabs() {
  chrome.tabs.query({ url: ["https://seller.ozon.ru/*"] }, tabs => {
    if (chrome.runtime.lastError) return;
    for (const tab of tabs) if (typeof tab.id === "number") ensureOzonTab(tab.id);
  });
}
chrome.runtime.onInstalled.addListener(restoreOpenTabs);
chrome.runtime.onStartup.addListener(restoreOpenTabs);
chrome.tabs.onUpdated.addListener((tabId, change, tab) => {
  if (change.status !== "complete") return;
  try {
    if (new URL(tab.url).origin === "https://seller.ozon.ru") ensureOzonTab(tabId);
  } catch (_) {}
});
