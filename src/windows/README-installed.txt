MARKING CODE CONVERTER FOR OZON / ПРЕОБРАЗОВАТЕЛЬ КОДА ДЛЯ OZON — WINDOWS
by PEI

RU: Проверочная версия 1.2.3. Исправлены учёт Shift и замена разделителей
с сохранением символов проверенного поля. Пройдены 153 автоматические
проверки и тест прежнего несовпадения в настоящем окне Windows.
Реальный сканер и Ozon требуют проверки на рабочем месте.
Общий канал обновлений остаётся на 1.2.2 до проверки реального товара.
EN: Candidate 1.2.3 fixes Shift tracking and preserves verified field
characters when repairing separators. Passed 153 automated assertions
and the previous mismatch test in a real Windows window. Physical scanner
and live Ozon validation are required. The shared feed remains on 1.2.2
until a real item has been validated.

УСТАНОВКА
Запустите установщик. Будут созданы ярлыки на рабочем столе и в меню
«Пуск», а также запись удаления в приложениях Windows.
Автозапуск при входе в Windows выбран по умолчанию. Его можно отключить.
Старая программа должна быть завершена кнопкой «Завершить работу».
Крестик лишь скрывает окно в трей.

Windows 10/11 x64, .NET Framework 4.8. Установка для текущего пользователя:
%LOCALAPPDATA%\Programs\OzonMarkingConverter
Права администратора не требуются. Цифровой подписи у сборки пока нет.

РАБОТА
1. Подключите базу сканера. Отключите прежнее браузерное расширение
   преобразователя, чтобы два обработчика не мешали друг другу.
2. При первой установке обработка включена. Если раньше вручную нажали
   «Пауза», нажмите «Включить обработку».
3. Очистите поле маркировки в Ozon, нажмите на него и сканируйте товар.
4. Проверьте ответ Ozon. Подтверждает товар сотрудник.

ВЫБИРАТЬ ПОРТ, ИНТЕРФЕЙС И СКОРОСТЬ НЕ НУЖНО.
USB/HID отслеживается автоматически. COM открывается только для
распознанного или ранее подтверждённого сканера. Остальные порты
не открываются. Устройства обновляются по событиям подключения/
отключения и дополнительно раз в минуту. Без данных скорость не меняется.

COM: после некорректных входящих данных проверяются 9600, 115200, 38400, 19200, 57600,
4800, 2400 и 1200 бод, формат 8N1 без управления потоком.
При первом определении может понадобиться несколько сканирований,
пока подбирается скорость. После корректного кода скорость запоминается.
Сканируйте по одному товару и ждите результата в поле.

Для USB после подтверждённой записи поддерживаемого кода запрашивается
английская раскладка ENG и выключается Caps Lock. Во время самого скана
раскладка не переключается. Данные сканера
разбираются по английской раскладке независимо от языка Windows.
Уже набранные символы проверяются перед заменой. Обычный ввод текста,
не похожий на начало маркировки, не переключает язык.
COM передаёт данные независимо от раскладки; после записи также
запрашивается ENG и выключается Caps Lock.

Bluetooth поддерживается через USB-базу или после сопряжения в Windows
как клавиатура / COM. Программа не выполняет сопряжение за Windows.
Устройства с собственным протоколом, без клавиатурного или COM-интерфейса,
требуют поддержки производителя. Драйверы программа не устанавливает.

ФОН И ВОССТАНОВЛЕНИЕ
Крестик скрывает окно в трей рядом с часами. Двойной щелчок открывает окно.
После закрытия браузера обработка продолжает работать.
После сна Windows восстанавливается обработчик, поиск устройств идёт
автоматически. Ручная пауза и полный выход остаются доступны.
Последнее состояние обработки сохраняется.

ОБНОВЛЕНИЯ
Первую версию с этим механизмом нужно установить один раз.
Далее программа проверяет GitHub после запуска и каждые 6 часов.
При новой версии появляется уведомление и кнопка «Обновить» на вкладке
«Обновления». После нажатия программа сама скачает установщик, проверит
размер и SHA256, завершит обработку и запустит установку с сохранением
настроек. После обновления программа запускается в трее.
При недоступном интернете обработка сканера продолжает работать.
Для обновления используются только файлы из репозитория:
https://github.com/ozonopm-crypto/ozon-marking-converter
Коды товаров, данные заказов и введённый текст в GitHub не отправляются.

ПРОВЕРКА И ОГРАНИЧЕНИЯ
На вкладке «Проверка кода» есть искусственный пример и тест автозамены.
«Тест несовпадения» проверяет сохранение символов поля при несовпадении
регистра захваченного текста. На вкладке «Диагностика» показываются
длины и результаты сравнения. Сам код в отчёт не включён; отчёт остаётся
только в памяти до следующей проверки или закрытия программы.
Перед массовым использованием проверьте один реальный товар в Ozon.
Физический АТОЛ SB3100 BT и работу на складе нужно проверить на месте.

Поддерживается длинный формат: GTIN 14 цифр, серийный номер 13 знаков,
ключ 4 знака, проверочная часть 44 знака с '=' в конце.
Исправляются разделители. Регистр и пропущенные символы не угадываются.
Это не проверка подлинности или статуса в «Честном ЗНАКе».
История кодов не ведётся, буфер обмена не читается и не меняется.

Поддерживаются доступные для чтения активные текстовые поля Яндекс
Браузера, Chrome, Edge и Opera. Адрес сайта не проверяется — выбирайте
поле маркировки. Браузер должен работать без повышения прав.
COM вставляет код только в пустое поле. Для USB проверяется весь код,
а не только GTIN. При ENG и выключенном Caps Lock корректный текст
поля может быть источником, если каждый символ полезной части
соответствует той же физической клавише скана. Регистр поля сохраняется.
Также поддерживается точное совпадение считанного суффикса. Положение курсора
учитывается только при замене суффикса, если в поле есть другой префикс.
Через MSAA выполняется только проверенная замена поля целиком.
После записи результат перечитывается и проверяется.
Если Windows не позволяет проверить поле, программа отказывается
от изменения. При смене поля или вводе другого текста повторите скан.

УДАЛЕНИЕ
Завершите программу и удалите её в «Параметры Windows → Приложения».
Ярлыки, программа и её запись автозапуска будут удалены.
Настройки в %LOCALAPPDATA%\OzonMarkingConverter сохраняются.

ENGLISH INSTRUCTIONS
by PEI

INSTALLATION
Run the installer. It creates Desktop and Start menu shortcuts and a
standard Windows uninstallation entry. Startup at Windows sign-in is
selected by default and can be disabled. Exit the old application before
updating; its X button only hides it in the tray.
Requires Windows 10/11 x64 and .NET Framework 4.8. Installation is per user:
%LOCALAPPDATA%\Programs\OzonMarkingConverter
Administrator rights are not required. The build is not digitally signed.

USE
Disable the old browser converter extension. Connect the scanner/base.
A fresh installation starts with processing enabled. A previously saved
manual pause remains paused; click Enable processing to resume.
Clear and focus Ozon's marking-code field, scan one item, and check Ozon's
response. Confirm the item manually.

AUTOMATIC CONNECTION
No port, interface, or baud selection is needed for recognized scanners.
USB/HID input is monitored automatically. Only scanner-named or previously
confirmed COM devices are opened; unrelated ports remain unopened.
Device events trigger discovery, with a one-minute fallback refresh.
Idle COM ports do not change baud rates.
COM rates: 9600, 115200, 38400, 19200, 57600, 4800, 2400, 1200 baud.
Format: 8N1 without flow control. Baud changes require invalid incoming
data. An unidentified COM scanner may need its vendor driver. Initial detection may require several
scans. Successful parameters are remembered. Scan one item at a time.

The application requests English input and switches Caps Lock off after
a supported code has been written and verified. The layout is not changed mid-scan.
Scanner keys are decoded using the
English layout; the actual typed suffix is checked before replacing it.
Ordinary text does not switch the layout. COM data does not depend on it.
Bluetooth works through a base or a keyboard/COM device already paired
in Windows. The application does not pair devices or install drivers.
Proprietary device protocols require vendor support.

BACKGROUND OPERATION
X hides the window in the tray. Double-click the tray icon to reopen it.
Closing the browser does not stop processing. The keyboard handler
is renewed after resume and between scans; devices are rediscovered
automatically. Pause and Exit remain available. Processing state is saved.

UPDATES
Install 1.2.0 or newer manually once if upgrading from 1.1.0. Afterwards,
GitHub is checked at startup and every six hours. When an update appears,
open Updates and click Update. The application downloads the installer,
verifies its size and SHA256, exits, runs setup, and restarts in the tray.
Preferences are preserved. Offline processing continues if GitHub is
unavailable. Codes, order data, and typed text are not sent to GitHub.
Source: https://github.com/ozonopm-crypto/ozon-marking-converter

VERIFICATION AND LIMITS
The code-check tab provides a synthetic example and a Windows replacement
test. The browser-test button applies only to the synthetic example;
see tests/windows/browser-check.html in the source repository.
The mismatch test checks preservation of field characters when captured
letter case differs. Diagnostics shows lengths and comparison results
without code contents. Its report exists only in memory until the next
check or application exit.
Test one real item before warehouse-wide use. Physical ATOL SB3100 BT
and the live Ozon field require validation at the workplace.

Supported format: GTIN 14 digits, serial 13 characters, key 4 characters,
verification block 44 characters ending in '='. Separators are prepared;
missing characters and letter case are not guessed. This is not a code
authenticity/status check. No code history is stored. The clipboard
is neither read nor changed.

Yandex Browser, Chrome, Edge and Opera must run without elevation.
The focused text field must expose its value through Windows accessibility.
The website URL is not checked. COM requires an empty field. USB checks
the entire code, not just its GTIN. With English input and Caps Lock off,
a valid field value may be used if every payload character corresponds
to the same physical scanner key; field letter case is preserved.
Exact typed-suffix matching is also supported. Caret position is required only
for suffix replacement with a separate prefix. MSAA supports only
verified whole-field replacements.
The result is read back after writing. Inaccessible fields, focus changes,
and unverified values produce a failure message. A successful field write
does not mean Ozon accepted the code. Check the field before rescanning.

UNINSTALL
Exit the application, then uninstall it in Windows Settings > Apps.
Program files, shortcuts, and its startup entry are removed. Preferences
in %LOCALAPPDATA%\OzonMarkingConverter remain for reinstallation.

by PEI
