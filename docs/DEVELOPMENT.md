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

RU: Проверяются разделители, контрольная цифра GTIN, границы скана, смена окна, COM-пакеты, подбор скорости и обновления. Новые проверки подтверждают, что неизменённый исходный код, обрезанный результат и потеря префикса не считаются успешной заменой.

EN: Checks cover separators, GTIN check digits, scan boundaries, window changes, COM framing, baud detection, and updates. New tests reject an unchanged raw code, truncated result, and lost prefix as successful replacements.

RU: Эти проверки не доказывают совместимость с актуальным полем Ozon или физическим сканером. Тест установки не проверяет браузер. SHA256 не является цифровой подписью Windows.

EN: These checks do not prove compatibility with the current Ozon field or a physical scanner. Installation tests do not validate browser behavior. SHA256 is not a Windows digital signature.

## Browser fixture / Проверка браузера

RU: Откройте tests/windows/browser-check.html в отдельной вкладке. В программе включите обработку, откройте «Проверка кода», нажмите «Тест в браузере» и за пять секунд перейдите в поле с искусственным примером. Ожидаются PASS / УСПЕХ и события input/change на странице. Этот пример не является кодом товара.

EN: Open tests/windows/browser-check.html in a separate tab. Enable processing, open the code-check tab, click the browser-test button, and focus the synthetic example within five seconds. Expect PASS / УСПЕХ and input/change events on the page. The example is not a product code.

RU: Параметр --verify-browser запускает изолированную проверочную копию без обработки физических сканов и без открытия COM-портов. Используйте отдельную папку --settings-dir. Этот режим не нужен сотрудникам склада.

EN: --verify-browser starts an isolated verification instance without capturing physical scans or opening COM ports. Supply a separate --settings-dir folder. Warehouse staff do not need this mode.

## Release / Выпуск

RU: Увеличивайте версию и пишите двуязычное описание. windows-updates.json относится только к Windows; updates.json — к расширению. Не заменяйте старые архивы и установщики.

EN: Increase the version and write bilingual notes. windows-updates.json is for Windows only; updates.json is for the extension. Do not replace old archives or installers.
