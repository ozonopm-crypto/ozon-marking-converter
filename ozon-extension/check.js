"use strict";
const testField = document.getElementById("marking");
const testStatus = document.getElementById("test-status");
const demoBase = "010474334896754821TEST123456789";
const demoTail = "A".repeat(43) + "=";
const demoExpected = demoBase + String.raw`\u001d91TEST\u001d92` + demoTail;
function reportTest() {
  if (!testField.value) { testStatus.textContent = "Ожидаю код маркировки товара."; return; }
  const checked = EipOzonMarking.normalize(testField.value);
  testStatus.textContent = checked.ok && checked.code === testField.value ? "Преобразование выполнено: формат разделителей подготовлен." : checked.reason || "Дождитесь завершения сканирования.";
}
testField.addEventListener("input", reportTest);
document.getElementById("simulate").addEventListener("click", () => {
  testField.value = demoBase; testField.focus();
  testField.setSelectionRange(testField.value.length, testField.value.length);
  testField.dispatchEvent(new KeyboardEvent("keydown", { key: "F8", bubbles: true, cancelable: true }));
  testField.value += "91TEST";
  testField.setSelectionRange(testField.value.length, testField.value.length);
  testField.dispatchEvent(new KeyboardEvent("keydown", { key: "F8", bubbles: true, cancelable: true }));
  testField.value += "92" + demoTail;
  testField.dispatchEvent(new InputEvent("input", { bubbles: true, data: null, inputType: "insertText" }));
  testStatus.textContent = testField.value === demoExpected ? "Тест пройден: F8 преобразуется в нужные разделители. Теперь проверьте реальный скан." : "Тест не прошёл. Убедитесь, что расширение включено, и повторите проверку.";
});
document.getElementById("clear-test").addEventListener("click", () => {
  testField.value = ""; testField.dispatchEvent(new InputEvent("input", { bubbles: true })); testField.focus(); reportTest();
});
