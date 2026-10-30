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

RU: В выпуске 1.2.9 пройдены 607 автоматических проверок: 80 ядра, 81 функций, 32 выбора источника поля и 414 разделителя Alt+NumPad и формирования кадра. Проверены установка и удаление текущей версии, ярлыки и издатель by PEI. 30 октября 2026 года владелец подтвердил полный обычный USB/HID-скан, автоматическую замену и принятие кода Ozon в Chrome. Работа на складе и в Яндекс Браузере повторно не проверялась.

EN: Release 1.2.9 passed 607 automated assertions: 80 core, 81 feature, 32 field-source, and 414 Alt+NumPad separator and frame-assembly checks. Current-version installation and uninstallation, shortcuts, and by PEI publisher checks passed. On October 30, 2026 the owner confirmed a complete ordinary USB/HID scan, automatic replacement, and Ozon acceptance in Chrome. Warehouse and Yandex Browser operation have not been revalidated.

RU: Прямой Native COM UIA прочитал 83 символа настоящего активного поля Chrome. Ранние отчёты Captured: 83 были созданы кнопкой искусственного теста и содержали другой GTIN. Такие отчёты подтверждают только путь TEST; они не доказывают поступление данных от сканера. Для обычного сканирования кнопки теста не используются.

EN: Direct Native COM UIA read 83 characters from the real focused Chrome field. Earlier Captured: 83 reports were produced by the synthetic test button and contained a different GTIN. Those reports establish only the TEST path, not physical scanner input. Ordinary scanning does not use test buttons.

RU: В 1.2.7 аппаратный USB/HID-поток переводится из Raw Input как единственного основного источника. LL-обработчик только считает события и проверяет управляющие клавиши по текущему кадру; второй перевод тех же клавиш исключён. Прямое чтение фокуса через Native COM сохранено. Перед записью обязательно строгое соответствие всей полезной части кода физическим клавишам скана; локальный эксперимент 1.2.6 с выбором текста через нормализацию заменён этой проверкой.

EN: Version 1.2.7 decodes physical USB/HID input from Raw Input as the single primary source. The LL hook only counts events and peeks at control keys against the current frame; the same keys are not decoded twice. Direct Native COM focus reading is retained. Writing requires strict physical-key correlation across the full payload; this replaces the local 1.2.6 experiment that selected normalized field text.

RU: В 1.2.8 обычный физический скан дал RAW/LL-события, но не завершённый пакет: максимальный кадр достигал 31 символа, границы первого GS. В 1.2.9 исправлен сброс при Alt+NumPad29. Проверка 30 октября 2026 года подтвердила физическую передачу Alt+29 и полный кадр: Max=85, packets=2, last packet=85, Alt29=2. Последний отчёт имеет источник USB/HID, Captured=85, Typed=83, Keyboard repair=True; исходное поле длиной 83 точно совпало с предсказанным вводом, а Readback длиной 85 имеет корректный формат и полное соответствие полезной части скану. Сброс префикса длиной 1 относится к незавершённому короткому кандидату, а не к потере этих полных пакетов. Коды и их символы не сохранены. Владелец отдельно подтвердил: «Код озон принял, автозамена работает». Фактическое состояние индикаторов языка и Caps Lock после записи отдельно не проверялось.

EN: In 1.2.8 ordinary physical scanning produced RAW/LL events but no completed packet; the maximum frame reached 31 characters, the first GS boundary. Version 1.2.9 fixes the Alt+NumPad29 reset. The October 30, 2026 test confirmed physical Alt+29 transmission and complete frames: Max=85, packets=2, last packet=85, Alt29=2. The latest report has USB/HID origin, Captured=85, Typed=83, Keyboard repair=True; the initial 83-character field exactly matched predicted input, and the 85-character readback has a valid format and full physical-payload correlation. A one-character prefix reset concerns a short incomplete candidate rather than loss of those complete packets. No code or payload characters were stored. The owner separately confirmed that Ozon accepted the code and automatic replacement works. The actual language and Caps Lock indicators after writing were not separately checked.

RU: Alt+NumPad29 принимается только на известной границе GS и только в том же окне. Проверяются промежутки между событиями и общий предел 2 секунды; Num Lock может быть выключен. Обработка отменяется при мыши, выключении, смене режима или ошибке. Диагностика считает принятые Alt+29 и сохраняет причину самого длинного потерянного кадра, исключая штатную передачу готового пакета. Автоматические проверки охватывают допустимые и отклоняемые Alt-коды, скан-коды цифровой клавиатуры, отпускания, сбросы и временные границы.

EN: Alt+NumPad29 is accepted only at a known GS boundary and within the same window. Inter-event gaps and a two-second overall limit are checked; Num Lock may be off. Mouse input, disabling capture, mode changes, and exceptions cancel the sequence. Diagnostics counts accepted Alt+29 sequences and preserves the largest lost frame's reason, excluding normal completed-packet emission. Automated assertions cover accepted and rejected Alt codes, physical numpad scan codes, releases, resets, and timing boundaries.

RU: В 1.2.9 браузер получает замену через клавиатурные события Windows после проверок активного поля, его текста и поколения ввода. Это устраняет зависимость записи в управляемую веб-форму от SetValue, который может не обновить её модель. Native COM UIA и резервные средства доступности остаются средствами чтения и проверки. Поле самой программы WinForms может использовать SetValue. Проверка чтением подтверждает записанный текст, но принятие кода подтверждает только ответ Ozon.

EN: Version 1.2.9 writes to browsers through Windows keyboard events after focused-field, text, and input-generation checks. This avoids relying on SetValue to update a controlled web form's model. Native COM UIA and accessibility fallbacks remain responsible for reading and verification. The application's own WinForms field may use SetValue. Readback verifies the written text; only Ozon's response establishes code acceptance.

RU: Диагностика различает TEST и USB/HID, указывает версию, RAW, LL, decoded и frame. Эти счётчики помогают найти этап остановки: получение аппаратных событий, перевод символов или формирование кадра. Их наличие само по себе не подтверждает принятие кода Ozon. Значения кодов, реальные GTIN и снимки рабочих полей в репозиторий не добавляются.

EN: Diagnostics distinguishes TEST from USB/HID and reports version, RAW, LL, decoded, and frame. These counters locate a stopped stage: hardware event reception, character decoding, or frame assembly. Counters alone do not establish Ozon acceptance. Code values, real GTINs, and screenshots of working fields are not added to the repository.

### Earlier validation / Предыдущие проверки

RU: В 1.2.4 прошли 185 проверок: 80 ядра, 73 функций и 32 источника поля. Искусственный случай пустого ValuePattern и TextPattern длиной 83 проверял выбор источника, но последующая диагностика настоящего браузера показала прокси с неверным содержимым. Чтение настоящего поля было исправлено прямым Native COM в 1.2.5. Исторический тест 1.2.4 не считается аппаратной проверкой.

EN: Version 1.2.4 passed 185 assertions: 80 core, 73 feature, and 32 field-source checks. Its synthetic empty-ValuePattern/83-character-TextPattern case tested reader selection, but later live-browser diagnostics exposed a proxy with incorrect content. Direct Native COM in 1.2.5 corrected real-field reading. The historical 1.2.4 test does not establish hardware operation.

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

RU: tests/windows/browser-native-check.html содержит обычное поле и имитацию управляемого поля с моделью состояния. Для второго PASS требует совпадения DOM, модели и поступления input/change. Молчаливая запись только в DOM не проходит; при потере фокуса поле восстанавливается из модели. В файле также есть искусственные поля только для чтения и пароля для проверки отказа. Используются только искусственные данные, без сети и внешних библиотек.

EN: tests/windows/browser-native-check.html provides a vanilla field and a controlled-model simulation. Controlled PASS requires matching DOM, matching state, and an input/change event. A silent DOM-only write fails; blur restores the field from its model. Synthetic read-only and password fields test rejection. It uses only synthetic data, without network requests or external libraries.

RU: Синтаксис и логика этой страницы проверены отдельно: длины 83/95, отказ молчаливой записи, обновление модели по событиям, сброс и восстановление при blur. Это проверки самой тестовой страницы, а не подтверждённый автоматизированный прогон в настоящем браузере или физическим сканером.

EN: The fixture's syntax and logic were checked separately: 83/95 lengths, silent-write rejection, event-driven state updates, reset, and blur restoration. These validate the fixture itself; they are not a confirmed automated real-browser run or physical scanner test.

RU: Для ручной проверки искусственного примера откройте страницу, нажмите её «Сбросить и выбрать», затем в программе «Проверка кода» → «Искусственный пример» и за пять секунд верните фокус в выбранное поле. Источник отчёта будет TEST. Простой прежний пример tests/windows/browser-check.html тоже сохранён. Настоящий товар проверяется обычным сканированием в Ozon без тестовых кнопок.

EN: To check the synthetic example manually, open the page, click its Reset and focus button, then choose Code check → Synthetic example in the app and return focus to the prepared field within five seconds. The report origin will be TEST. The earlier simple tests/windows/browser-check.html fixture is also retained. A real item is checked by ordinary scanning in Ozon without test buttons.

RU: Параметр --verify-browser запускает изолированную проверочную копию без обработки физических сканов и без открытия COM-портов. Используйте отдельную папку --settings-dir. Этот режим не нужен сотрудникам склада.

EN: --verify-browser starts an isolated verification instance without capturing physical scans or opening COM ports. Supply a separate --settings-dir folder. Warehouse staff do not need this mode.

## Release / Выпуск

RU: Увеличивайте версию и пишите двуязычное описание. windows-updates.json относится только к Windows; updates.json — к расширению. Не заменяйте старые архивы и установщики.

EN: Increase the version and write bilingual notes. windows-updates.json is for Windows only; updates.json is for the extension. Do not replace old archives or installers.

RU: Выпуск 1.2.9 и main/windows-updates.json используют тот же установщик, который прошёл физическую проверку и принятие кода Ozon 30 октября 2026 года. SHA256: `6702965ccae62573ca92a2f37bc17c1de2bc2e79b9111ad00fb413ba0c56dde7`. Исходники и канал обновления публикуются вместе с этим файлом. После подтверждения не пересобирайте установщик без повторной проверки; изменения в документации репозитория не требуют замены проверенного файла.

EN: Release 1.2.9 and main/windows-updates.json use the same installer that passed physical scanning and Ozon acceptance on October 30, 2026. SHA256: `6702965ccae62573ca92a2f37bc17c1de2bc2e79b9111ad00fb413ba0c56dde7`. The corresponding sources and update feed are published with that file. Do not rebuild a validated installer without repeating validation; repository documentation changes do not require replacing the tested file.

[Download Windows 1.2.9 / Скачать Windows 1.2.9](https://raw.githubusercontent.com/ozonopm-crypto/ozon-marking-converter/main/downloads/OzonMarkingConverter-Setup-1.2.9.exe)
