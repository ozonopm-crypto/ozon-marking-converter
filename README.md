# Marking Code Converter for Ozon / Преобразователь кода для Ozon

**by PEI · Русский / English**

RU: Программа Windows и отдельное расширение браузера для подготовки разделителей длинных кодов маркировки «Честный ЗНАК» в Ozon Seller. Подлинность и статус кода проверяет Ozon. Пропущенные символы не восстанавливаются, регистр не исправляется наугад.

EN: A Windows application and a separate browser extension that prepare separators in long Chestny ZNAK marking codes for Ozon Seller. Ozon validates the code and its status. Missing characters are not reconstructed, and letter case is not guessed.

## Windows 1.2.4 — candidate / проверочная версия

[Download Windows 1.2.4 candidate / Скачать проверочную версию Windows 1.2.4](https://raw.githubusercontent.com/ozonopm-crypto/ozon-marking-converter/windows-scanner-fix-1.2.4/downloads/OzonMarkingConverter-Setup-1.2.4.exe)

RU: В 1.2.3 получен отказ с длинами «скан 83, поле 0», хотя код виден в браузере. В 1.2.4 исправлен пропуск TextPattern при пустом ValuePattern того же поля. Если оба непустых источника противоречат друг другу, программа отказывается от замены. Добавлено резервное чтение MSAA в границах проверенного поля. Выбранный способ чтения сохраняется до проверки результата. **Пройдены 185 автоматических проверок. Физический АТОЛ и реальный Ozon ещё требуют проверки: управление Chrome остановилось на определении адреса страницы.**

EN: Version 1.2.3 reported a captured length of 83 and field length of zero despite visible browser text. Version 1.2.4 fixes skipped TextPattern reads when the same field exposes an empty ValuePattern. Conflicting nonempty sources reject replacement. Added an MSAA fallback within the verified focused field. The selected reader remains pinned through result verification. **185 automated assertions passed. Physical ATOL and live Ozon validation remain required: Chrome automation stopped at URL detection.**

RU: Эта версия доступна отдельно. Общий канал обновлений main/windows-updates.json остаётся на 1.2.2 до проверки одного реального товара. Завершите старую программу кнопкой «Завершить работу», закройте диагностическую копию и установите 1.2.4. Отключите прежнее расширение, очистите поле маркировки Ozon и отсканируйте один товар. Если замена отклонена, откройте вкладку «Диагностика» — теперь там указаны длины ValuePattern/TextPattern и причины отказа резервных способов чтения. Самого кода в отчёте нет.

EN: This candidate is distributed separately. The shared main/windows-updates.json feed remains on 1.2.2 until one real item has been validated. Exit the old application and diagnostic instance, then install 1.2.4. Disable the old extension, clear the Ozon marking-code field, and scan one item. If replacement is rejected, open Diagnostics: it now shows ValuePattern/TextPattern lengths and fallback-reader rejection reasons, without code contents.

### Installation and use / Установка и работа

RU: Windows 10/11 x64, .NET Framework 4.8. Установщик создаёт ярлыки на рабочем столе и в меню «Пуск». Автозапуск при входе в Windows выбран по умолчанию; при первой установке обработка включена. Крестик скрывает программу в трей. Для полного выхода используйте «Завершить работу». Обновление сохраняет настройки.

EN: Requires Windows 10/11 x64 and .NET Framework 4.8. The installer creates Desktop and Start menu shortcuts. Startup at Windows sign-in is selected by default, and processing starts enabled on a fresh installation. Closing the window hides the application in the tray. Use Exit to stop it completely. Updating preserves preferences.

RU: Отключите прежнее расширение преобразователя. Подключите сканер, очистите поле маркировки Ozon, нажмите на него и отсканируйте один товар. Проверьте ответ Ozon; товар подтверждает сотрудник.

EN: Disable the previous converter extension. Connect the scanner, clear and focus Ozon's marking-code field, and scan one item. Check Ozon's response; an employee confirms the item manually.

### Automatic connection / Автоматическое подключение

RU: USB/HID обнаруживается автоматически. COM открывается только для устройства с названием сканера или ранее подтверждённым идентификатором. Прочие порты не открываются. Скорость меняется только после поступления некорректных данных; без данных перебора нет. Формат 8N1 без управления потоком, набор скоростей 1200–115200 бод. Неопознанному COM-устройству может потребоваться драйвер производителя.

EN: USB/HID is detected automatically. A COM port is opened only for a scanner-named or previously confirmed device identity. Other ports remain unopened. Baud changes occur only after invalid incoming data; idle ports do not cycle. Supported rates are between 1200 and 115200 baud, using 8N1 without flow control. An unidentified COM device may require its vendor driver.

RU: После подтверждённой записи поддерживаемого кода запрашивается ENG и выключается Caps Lock. Во время скана раскладка не переключается; клавиши разбираются по английской раскладке. Bluetooth поддерживается через базу либо как уже сопряжённая в Windows клавиатура / COM. Драйверы, закрытые протоколы и сопряжение требуют средств производителя / Windows.

EN: After a supported code has been written and verified, the application requests English input and switches Caps Lock off. The layout is not switched mid-scan; scanner keys are decoded using English. Bluetooth works through a base or an already paired Windows keyboard/COM interface. Proprietary protocols, drivers, and pairing require vendor/Windows tools.

### Updates / Обновления

RU: Начиная с 1.2.0 программа проверяет GitHub после запуска и каждые 6 часов. Сотрудник нажимает «Обновить»; программа скачивает и проверяет установщик, закрывается, обновляется и запускается в трее. Переход с 1.1.0 требует одной ручной установки. У сборки пока нет цифровой подписи Windows.

EN: Since 1.2.0, the application checks GitHub after startup and every six hours. An employee clicks Update; the application downloads and verifies the installer, exits, updates, and restarts in the tray. Upgrading from 1.1.0 requires one manual installation. The build does not yet carry a Windows digital signature.

[Windows instructions / Инструкция Windows](src/windows/README-installed.txt) · [Publishing updates / Выпуск обновлений](windows/ВЫПУСК_ОБНОВЛЕНИЙ.txt) · [Release workflow / Форма выпуска](https://github.com/ozonopm-crypto/ozon-marking-converter/actions/workflows/windows-release.yml)

## Browser extension 1.2.2 / Расширение браузера 1.2.2

[Download extension ZIP / Скачать ZIP расширения](https://raw.githubusercontent.com/ozonopm-crypto/ozon-marking-converter/main/downloads/ozon-extension-1.2.2.zip)

RU: Отдельное расширение для Яндекс Браузера, Chrome, Edge и Opera на компьютере. Распакуйте ZIP в постоянную папку. На странице browser://extensions, chrome://extensions, edge://extensions или opera://extensions включите режим разработчика и загрузите папку ozon-extension с manifest.json. Разрешите доступ к seller.ozon.ru и обновите вкладки Ozon.

EN: A separate desktop extension for Yandex Browser, Chrome, Edge, and Opera. Extract the ZIP into a permanent folder. Open browser://extensions, chrome://extensions, edge://extensions, or opera://extensions, enable developer mode, and load the ozon-extension folder containing manifest.json. Allow access to seller.ozon.ru and reload Ozon tabs.

RU: **Для расширения нужна раскладка ENG и выключенный Caps Lock.** Яндекс Браузер может отключать распакованное расширение после перезапуска; оно не может включить себя самостоятельно. Эта ZIP-сборка уведомляет об обновлении, но её файлы заменяются вручную. Не используйте расширение одновременно с Windows-программой.

EN: **The extension requires English input and Caps Lock off.** Yandex Browser may disable unpacked extensions after a restart; an extension cannot re-enable itself. This ZIP distribution provides update notifications, but its files must be replaced manually. Do not run the extension alongside the Windows application.

[Extension instructions / Инструкция расширения](ozon-extension/ИНСТРУКЦИЯ.txt) · [Extension update channel / Канал обновлений расширения](update-channel/НАСТРОЙКА_ОБНОВЛЕНИЙ.txt)

## Format and privacy / Формат и данные

RU: Поддерживается длинный формат: GTIN 14 цифр, серийный номер 13 знаков, ключ 4 знака и проверочная часть 44 знака с '=' в конце. Неподдерживаемые форматы не исправляются. Коды обрабатываются локально, их история не сохраняется и в GitHub не отправляется. GitHub используется только для проверки версии и скачивания обновлений.

EN: The supported long format contains a 14-digit GTIN, a 13-character serial number, a four-character key, and a 44-character verification block ending in '='. Unsupported formats are not repaired. Codes are processed locally, are not logged, and are not sent to GitHub. GitHub is used only for version checks and update downloads.

RU: Программа изменяет только проверенное активное поле поддерживаемого браузера. COM заполняет только пустое поле. Для USB проверяется весь код, а не только GTIN. В ENG с выключенным Caps Lock корректный текст поля может быть источником при полном соответствии физическим клавишам скана; его регистр сохраняется. Через MSAA заменяется только поле целиком. Если поле недоступно, фокус изменился или результат не подтверждён, программа сообщает об отказе. Успех записи не означает, что Ozon принял код.

EN: The application changes only a verified focused field in a supported browser. COM input requires an empty field. USB correlation covers the entire payload, not just the GTIN. With English input and Caps Lock off, a valid field value may be used when every payload character corresponds to the same physical scanner key; its case is preserved. MSAA supports only whole-field replacement. Unavailable fields, focus changes, and unverified results produce a failure message. A successful field write does not mean Ozon accepted the code.

[Development and verification / Разработка и проверки](docs/DEVELOPMENT.md) · [Changelog / История изменений](CHANGELOG.md)
