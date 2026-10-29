# Development and verification / Разработка и проверки

## Windows build / Сборка Windows

RU: C# 5, .NET Framework 4.8, WinForms и Inno Setup 6/7. Нужен Windows с компилятором .NET Framework и Inno Setup. Платные библиотеки и сервер приложения не нужны.

EN: Uses C# 5, .NET Framework 4.8, WinForms, and Inno Setup 6/7. Requires Windows with the .NET Framework compiler and Inno Setup. No paid libraries or application server are required.

    ./src/windows/build-installer.ps1
    ./tests/windows/Run.ps1

RU: Установщик появляется в outputs. Для проверки установки используйте TestMode: ярлыки и регистрация приложения изолированы от рабочей установки.

EN: The installer is written to outputs. Use TestMode for installation checks: shortcuts and registration are isolated from the working installation.

    ./src/windows/build-installer.ps1 -TestMode
    ./tests/windows/InstallerSmoke.ps1

## Regression checks / Проверки регрессий

RU: В 1.2.4 пройдены 185 проверок: 80 ядра, 73 функций и 32 выбора источника поля. Новый искусственный случай воспроизводит пустой ValuePattern при TextPattern длиной 83, сохранение выбранного источника при повторном чтении, конфликт представлений и защиту COM от перезаписи текста, появившегося перед записью. Новая сборка не проверена в живом Ozon: управление Chrome остановилось на определении URL.

EN: Version 1.2.4 passed 185 assertions: 80 core, 73 feature, and 32 field-source checks. New synthetic cases reproduce an empty ValuePattern with an 83-character TextPattern, pinned-source readback, source conflicts, and COM protection when text appears before writing. This build has not been verified in live Ozon: Chrome automation stopped at URL detection.

RU: Читаются только представления активного поля. MSAA(point) использует физические экранные координаты внутри границ проверенного UIA-поля, затем проверяет фокус, состояние и известного владельца. Поиск других полей и чтение их текста не выполняются. Каждый отказ отражается в диагностике без значений, имён и координат.

EN: Read only representations of the focused field. MSAA(point) uses physical screen coordinates within the verified UIA field's bounds, then checks focus, state, and known ownership. It does not search or read unrelated fields. Diagnostics records each failure without values, names, or coordinates.

[Microsoft Edit pattern requirements / Требования Microsoft к Edit](https://learn.microsoft.com/en-us/windows/win32/winauto/uiauto-supporteditcontroltype) · [Chromium TextPattern documentation / Документация Chromium](https://chromium.googlesource.com/chromium/src/+/HEAD/docs/accessibility/browser/uiautomation.md)

RU: Проверяются разделители, контрольная цифра GTIN, границы скана, смена окна, COM-пакеты, подбор скорости и обновления. Новые проверки подтверждают, что неизменённый исходный код, обрезанный результат и потеря префикса не считаются успешной заменой.

EN: Checks cover separators, GTIN check digits, scan boundaries, window changes, COM framing, baud detection, and updates. New tests reject an unchanged raw code, truncated result, and lost prefix as successful replacements.

RU: Эти проверки не доказывают совместимость с актуальным полем Ozon или физическим сканером. Тест установки не проверяет браузер. SHA256 не является цифровой подписью Windows.

EN: These checks do not prove compatibility with the current Ozon field or a physical scanner. Installation tests do not validate browser behavior. SHA256 is not a Windows digital signature.

RU: Для 1.2.3 пройдены 80 проверок ядра и 73 проверки функций, установка/удаление и настоящий тест Windows «Тест несовпадения»: видимый искусственный код с заглавными буквами сохранён при захваченном строчном варианте, оба разделителя записаны и перечитаны. В реальном Chrome/Ozon проверка не закончена: управление браузером остановилось на определении URL. Сам тестовый код не относится к товару.

EN: For 1.2.3, 80 core and 73 feature assertions passed, along with installation/uninstallation and the real Windows mismatch test: an uppercase synthetic field value was preserved despite a lowercase captured representation, and both separators were written and read back. Live Chrome/Ozon verification remains incomplete because browser automation stopped at URL detection. The synthetic code does not identify a product.

RU: --verify-scanner запускает отдельную диагностическую копию: физические клавиши обрабатываются только в разрешённом тестовом поле самой программы, COM не открывается, автозапуск и обновления отключены. Перед физическим тестом очистите поле. Используйте отдельную папку --settings-dir. Этот режим не заменяет проверку в Ozon.

EN: --verify-scanner runs a separate diagnostic instance: physical keys are captured only in the application's permitted test field; COM, startup changes, and updates are disabled. Clear the field before a physical scan. Use a separate --settings-dir folder. This mode does not replace live Ozon validation.

## Browser fixture / Проверка браузера

RU: Откройте tests/windows/browser-check.html в отдельной вкладке. В программе включите обработку, откройте «Проверка кода», нажмите «Тест в браузере» и за пять секунд перейдите в поле с искусственным примером. Ожидаются PASS / УСПЕХ и события input/change на странице. Этот пример не является кодом товара.

EN: Open tests/windows/browser-check.html in a separate tab. Enable processing, open the code-check tab, click the browser-test button, and focus the synthetic example within five seconds. Expect PASS / УСПЕХ and input/change events on the page. The example is not a product code.

RU: Параметр --verify-browser запускает изолированную проверочную копию без обработки физических сканов и без открытия COM-портов. Используйте отдельную папку --settings-dir. Этот режим не нужен сотрудникам склада.

EN: --verify-browser starts an isolated verification instance without capturing physical scans or opening COM ports. Supply a separate --settings-dir folder. Warehouse staff do not need this mode.

## Release / Выпуск

RU: Увеличивайте версию и пишите двуязычное описание. windows-updates.json относится только к Windows; updates.json — к расширению. Не заменяйте старые архивы и установщики.

EN: Increase the version and write bilingual notes. windows-updates.json is for Windows only; updates.json is for the extension. Do not replace old archives or installers.

RU: Проверочная 1.2.4 размещается в ветке windows-scanner-fix-1.2.4. Не копируйте сгенерированный outputs/publish/windows-updates.json в main до проверки реального товара. После подтверждения публикуйте тот же проверенный установщик и его SHA256 в общем канале.

EN: Candidate 1.2.4 is hosted on windows-scanner-fix-1.2.4. Do not copy the generated outputs/publish/windows-updates.json into main before a real-item check. After validation, publish the same tested installer and its SHA256 to the shared channel.
