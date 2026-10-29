# Changelog / История изменений

## Windows 1.2.4 — candidate / проверочная версия

RU:
- Исправлен пропуск TextPattern при пустом ValuePattern: читаются оба представления одного активного поля, выбирается непустое. При конфликте непустых представлений замена отклоняется.
- Повторное чтение остаётся на выбранном источнике, в том числе после записи. Пустой ValuePattern больше не скрывает существующий текст от проверки COM-вставки.
- Добавлено резервное чтение MSAA в точке внутри границ проверенного поля, с проверкой фокуса, состояния, владельца и совпадения всего кода. Поиск по другим полям не выполняется.
- Диагностика показывает длины ValuePattern/TextPattern и причины отказа каждого резервного способа; коды, имена полей и координаты в отчёт не включаются.
- Пройдены 185 автоматических проверок, включая 32 новые проверки пустого ValuePattern, конфликтов, защиты COM и выбранного источника чтения. Установка и удаление проверены отдельно.
- Проверка с физическим сканером в Ozon пока не подтверждена; общий канал склада остаётся на 1.2.2.

EN:
- Fixed skipped TextPattern reads when ValuePattern is empty. Read both representations of the same focused field and select the nonempty one; conflicting nonempty representations reject replacement.
- Keep the selected reader pinned before and after writing. An empty ValuePattern no longer hides existing text from the COM insertion guard.
- Added an MSAA fallback at a point within the verified field's bounds, checking focus, eligibility, ownership, and the full captured payload. Other fields are not searched.
- Diagnostics shows ValuePattern/TextPattern lengths and every fallback-reader rejection reason without code contents, field names, or coordinates.
- Passed 185 automated assertions, including 32 new empty-ValuePattern, conflict, COM protection, and reader-pinning checks. Installation and uninstallation were checked separately.
- Physical scanner operation in live Ozon remains unverified; the warehouse feed stays on 1.2.2.

## Windows 1.2.3 — candidate / проверочная версия

RU:
- Shift, Ctrl, Alt и Caps Lock учитываются по порядку событий; состояние не запрашивается асинхронно внутри обработчика клавиш.
- Корректный код из активного поля сохраняется при полном соответствии физическим клавишам скана. Исправляются только разделители; регистр и символы поля не меняются наугад.
- Добавлена запись значения целиком через UIA с проверкой результата; переключение ENG/Caps Lock выполняется после подтверждённой записи.
- Поддержаны текстовые события программных сканеров; собственный ввод программы не захватывается повторно.
- Щелчок или прокрутка отменяют незавершённую замену. Добавлены ожидание готовности поля и резервное чтение через MSAA.
- На вкладке «Диагностика» показываются длины и результаты сравнения без самого кода; отчёт не сохраняется на диск и не отправляется в интернет.
- Пройдены 153 автоматические проверки, установка/удаление и тест прежнего несовпадения в настоящем окне Windows. Физический сканер и реальный Ozon ещё требуют проверки.
- Установщик распространяется отдельно; общий канал обновлений остаётся на 1.2.2 до проверки реального товара.

EN:
- Track Shift, Ctrl, Alt, and Caps Lock in keyboard event order instead of querying asynchronous state inside the keyboard callback.
- Preserve a valid active-field code when its entire payload corresponds to the same physical scanner keys. Repair separators without guessing field characters or case.
- Added whole-value UIA writes with result verification; request English input and Caps Lock off after a verified write.
- Accept software-scanner text events while excluding the application's own injected input.
- Cancel a pending replacement on a click or scroll. Added field-readiness retries and an MSAA alternative reader.
- Diagnostics reports lengths and comparison results without code contents; it is neither written to disk nor sent over the network.
- Passed 153 automated assertions, installation/uninstallation checks, and the previous mismatch test in a real Windows window. Physical scanner and live Ozon validation remain required.
- Distribute this installer separately; the shared update feed remains on 1.2.2 until a real item has been validated.

## Windows 1.2.2 — validation build / сборка для проверки

RU:
- Полностью полученный код больше не ждёт 650 мс; остаётся короткая пауза отпускания клавиши.
- Подготовка раскладки и Caps Lock перенесена за пределы обработчика клавиш и выполняется после скана.
- Убраны повторное чтение имени HID на каждом символе, опрос WMI каждые пять секунд и обновление неизменившегося списка.
- Подключение/отключение отслеживается событиями, резервная проверка выполняется раз в минуту.
- Не открываются посторонние COM-порты; без входящих данных скорость не переключается.
- Точное совпадение всего кода допускает замену поля целиком без зависимости от сообщения браузера о курсоре.
- UIA используется первым; MSAA включается только при необходимости. Во время одной замены способ чтения не меняется.
- Физический сканер и реальный Ozon требуют проверки; сквозной тест в браузере не подтверждён.

EN:
- Complete scans no longer wait 650 ms; only a short key-release delay remains.
- Layout and Caps Lock preparation runs after the scan, outside the keyboard callback.
- Removed per-character HID name queries, five-second WMI polling, and unchanged-list redraws.
- Device events trigger discovery, with a one-minute fallback refresh.
- Unrelated COM ports remain unopened, and idle ports do not switch baud rates.
- An exact whole-code match supports full-field replacement without depending on browser caret reporting.
- UIA is preferred; MSAA is used only when needed. Verification keeps the same reader throughout a replacement.
- Physical scanner and live Ozon validation are still required; end-to-end browser verification is unconfirmed.

## Windows 1.2.1 — validation build / сборка для проверки

RU:
- Добавлен доступ к активному полю через MSAA для браузеров, не предоставляющих поле через UIA.
- Добавлена запись значения целиком через доступность Chromium.
- Успех отображается только после чтения и проверки результата. Неизменённый исходный код больше не считается успехом.
- Добавлены ожидание готовности доступности и безопасный тест с искусственным кодом в браузере.
- Документация GitHub и описания выпусков оформлены на русском и английском.
- Сквозная проверка в браузере и проверка на физическом АТОЛ остаются неподтверждёнными.

EN:
- Added MSAA access to the focused field when the browser does not expose it through UIA.
- Added whole-value writes through Chromium accessibility.
- Reports success only after reading and verifying the result. An unchanged raw code is no longer treated as success.
- Added an accessibility-readiness retry and a synthetic-code browser test.
- Made GitHub documentation and release descriptions bilingual in Russian and English.
- End-to-end browser verification and physical ATOL scanner validation remain unconfirmed.

## Windows 1.2.0

RU: Автоматический поиск USB/HID и COM, подбор скорости COM, переподключение, подготовка клавиатуры и обновление через GitHub. Позже получены сообщения о проблемах замены в Chrome и Яндекс Браузере.

EN: Added USB/HID and COM discovery, COM baud detection, reconnection, keyboard preparation, and GitHub updates. Field-replacement failures were subsequently reported in Chrome and Yandex Browser.

## Windows 1.1.0

RU: Установщик Windows, ярлыки на рабочем столе и в «Пуск», стандартное удаление, автор by PEI.

EN: Windows installer, Desktop and Start menu shortcuts, standard uninstallation, and by PEI attribution.

## Browser extension 1.2.2 / Расширение 1.2.2

RU: Добавлен значок красного сканера.

EN: Added a red scanner icon.
