# Changelog / История изменений

RU: Последний выпуск Windows и общий канал обновлений — 1.2.9. 30 октября 2026 года владелец подтвердил обычный USB/HID-скан, автоматическую замену и принятие кода Ozon в Chrome. Исторические записи ниже описывают отдельные этапы до этого подтверждения.

EN: The latest Windows release and shared update feed are 1.2.9. On October 30, 2026 the owner confirmed ordinary USB/HID scanning, automatic replacement, and Ozon acceptance in Chrome. Earlier entries describe stages before that confirmation.

## Windows 1.2.9 — October 30, 2026 / 30 октября 2026

RU:

- Добавлен приём GS как Alt+NumPad29 только на известной границе разделителя маркировки, включая ведущие нули и цифровую клавиатуру при выключенном Num Lock.
- Последовательность сохраняет кадр при допустимых промежутках между событиями, проверяет то же окно и ограничена общим временем 2 секунды. Выключение обработки, мышь, смена режима и ошибка приёма отменяют незавершённый Alt-ввод.
- Диагностика сохраняет причину потери самого длинного кадра; обычная передача готового кадра не считается потерей. Добавлен счётчик принятых Alt+29.
- Запись в браузере выполняется клавиатурными событиями Windows после проверки фокуса, текста и поколения ввода, чтобы управляемая форма получала события ввода. UIA используется для чтения и проверки; собственное поле WinForms может использовать SetValue.
- Пройдены 607 автоматических проверок: 80 ядра, 81 функций, 32 источника поля и 414 Alt+NumPad и формирования кадра. Проверены установка и удаление версии 1.2.9.
- Проверка 30 октября 2026 года подтвердила физический USB/HID-ввод: два полных пакета по 85 символов, распознанные Alt+29, точное совпадение исходного поля с предсказанным вводом и корректный формат после проверенной записи. Владелец отдельно подтвердил принятие кода Ozon и работу автозамены в Chrome.
- Выпущен тот же проверенный установщик; общий канал обновлён до 1.2.9. Повторная проверка на складе, в Яндекс Браузере и отдельная проверка фактического переключения индикаторов языка/Caps Lock не проводились.

EN:

- Added GS reception as Alt+NumPad29 only at a known marking-code separator boundary, including leading zeros and numpad scan codes with Num Lock off.
- Preserve the frame while inter-event gaps remain valid, require the same window, and limit the entire sequence to two seconds. Disabling capture, mouse input, mode changes, and input exceptions cancel incomplete Alt sequences.
- Retain the largest lost frame's reset reason; normal completed-frame emission is not treated as a loss. Added an accepted Alt+29 counter.
- Browser writes use Windows keyboard events after focus, text, and input-generation checks so controlled forms receive input events. UIA reads and verifies; the application's own WinForms field may still use SetValue.
- Passed 607 automated assertions: 80 core, 81 feature, 32 field-source, and 414 Alt+NumPad and frame-assembly checks. Version 1.2.9 installation and uninstallation passed.
- The October 30, 2026 physical USB/HID test confirmed two complete 85-character packets, recognized Alt+29 sequences, an exact initial-field/predicted-input match, and a valid format after verified replacement. The owner separately confirmed Ozon acceptance and automatic replacement in Chrome.
- Released the same tested installer and updated the shared feed to 1.2.9. Warehouse and Yandex Browser revalidation, and a separate check of the actual language/Caps Lock indicators after writing, have not been performed.

## Windows 1.2.8 — hardware reception observed / получение аппаратного ввода подтверждено

RU: Добавлены синхронизация состояния модификаторов между сканами и сведения о сбросе кадра. При обычном сканировании счётчики показали аппаратные события RAW/LL и перевод символов; максимальная длина кандидата составила 31 символ, завершённых пакетов не было. Поздний отказ префикса длиной 1 перекрывал первоначальную причину сброса. Граница 31 соответствует первому служебному разделителю поддерживаемого формата; на том этапе конкретный способ его передачи ещё не был установлен. Сборка оставалась локальной.

EN: Added modifier-state synchronization between scans and frame-reset diagnostics. Ordinary scanning produced RAW/LL hardware events and decoded characters; the maximum candidate length was 31, with no completed packets. A later one-character prefix rejection overwrote the original reset reason. Position 31 is the first separator boundary in the supported format; at that stage the device's separator transmission method was still unconfirmed. This build remained local.

## Windows 1.2.7 — local validation, unpublished / локальная проверка, не опубликована

RU:
- Физический USB/HID-ввод переводится через Raw Input как единственный основной источник; исключён двойной перевод того же потока через Raw Input и LL.
- Низкоуровневый LL-обработчик оставлен лёгким: счётчик событий и проверка управляющих клавиш по текущему кадру без повторного перевода символов.
- Сохранено прямое чтение активного поля через Native COM UIA; восстановлена строгая проверка всей полезной части кода на соответствие физическим клавишам скана.
- Диагностика показывает версию, источник TEST / USB/HID и счётчики RAW, LL, decoded, frame без самого кода.
- Ранее показанные Captured: 83 относились к искусственному тесту с другим GTIN. Чтение 83 символов настоящего поля Chrome через Native COM подтверждено отдельно; аппаратный скан ещё требует проверки.
- Пройдены 193 автоматические проверки: 80 ядра, 81 функций и 32 источника поля; установка и удаление проверены. Версия уже установлена на компьютере владельца.
- Обычное сканирование выполняется без кнопок теста. Принятие реального скана в Ozon и работа на складе пока не подтверждены; публичного выпуска и изменения общего канала нет.

EN:
- Decode physical USB/HID input through Raw Input as the single primary source, avoiding duplicate decoding through Raw Input and LL.
- Keep the LL hook lightweight: event counting and control-key peeking against the current frame without decoding characters again.
- Retain direct Native COM UIA focus reading and restore strict physical-key correlation for the entire payload.
- Diagnostics shows version, TEST / USB/HID origin, and RAW, LL, decoded, frame counters without code contents.
- Earlier Captured: 83 reports came from a synthetic test with a different GTIN. Native COM separately read 83 characters from the real Chrome field; physical scanner input still requires validation.
- Passed 193 automated assertions: 80 core, 81 feature, and 32 field-source checks; installation and uninstallation passed. This version is already installed on the owner's computer.
- Ordinary scanning uses no test buttons. Live Ozon acceptance and warehouse operation remain unconfirmed; there is no public release or shared-feed change.

## Windows 1.2.6 — local experiment, unpublished / локальный эксперимент, не опубликована

RU: Локально проверялся выбор текста поля через нормализацию. Публичного выпуска не было. В 1.2.7 восстановлено строгое соответствие всей полезной части кода физическим клавишам скана; результаты искусственных тестов не считаются проверкой сканера.

EN: A local experiment selected field text through normalization. It was not publicly released. Version 1.2.7 restores strict physical-key correlation for the entire payload; synthetic test results do not establish scanner operation.

## Windows 1.2.5 — local Native COM reading / локальное чтение Native COM

RU: Добавлено прямое чтение фокуса через Native COM UIA вместо зависимости только от управляемого прокси UIA/MSAA. Из настоящего поля Chrome прочитаны 83 символа. Это подтвердило доступ к полю; проверка обычного физического скана и принятия кода Ozon оставалась открытой. Общий канал обновлений не изменялся.

EN: Added direct Native COM UIA focus reading instead of relying solely on the managed UIA/MSAA proxy. It read 83 characters from the real Chrome field. Field access was established; an ordinary physical scan and Ozon acceptance still required validation. The shared update feed was unchanged.

## Windows 1.2.4 — candidate / проверочная версия

RU: Историческая опубликованная проверочная версия. Последующая диагностика показала, что её управляемый UIA/MSAA-прокси возвращал неверное содержимое поля. Прямой Native COM был добавлен в 1.2.5, отдельный аппаратный путь Raw Input — в 1.2.7. Обычный физический скан с принятием кода Ozon подтверждён позднее в 1.2.9.

EN: Historical public candidate. Later diagnostics showed its managed UIA/MSAA proxy returning incorrect field content. Direct Native COM was added in 1.2.5 and a separate Raw Input hardware path in 1.2.7. An ordinary physical scan with Ozon acceptance was subsequently confirmed in 1.2.9.

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
