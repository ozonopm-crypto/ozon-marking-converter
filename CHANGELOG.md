# Changelog / История изменений

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
