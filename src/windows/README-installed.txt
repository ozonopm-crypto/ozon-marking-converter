MARKING CODE CONVERTER FOR OZON / ПРЕОБРАЗОВАТЕЛЬ КОДА ДЛЯ OZON — WINDOWS
by PEI

RU: Сборка 1.2.1 требует проверки на рабочем месте. Добавлен доступ
через MSAA и проверка результата записи. Сквозной тест в браузере
в среде разработки не подтверждён.
EN: Version 1.2.1 requires workplace validation. It adds MSAA access and
post-write verification. End-to-end browser verification is unconfirmed.

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
USB/HID отслеживается автоматически. Доступные COM-порты прослушиваются
автоматически; программа не отправляет в них команды. Занятый порт
повторно проверяется каждые 3 секунды. Список устройств обновляется
примерно каждые 5 секунд.

COM: автоматически перебираются 9600, 115200, 38400, 19200, 57600,
4800, 2400 и 1200 бод, формат 8N1 без управления потоком.
При первом определении может понадобиться несколько сканирований,
пока подбирается скорость. После корректного кода скорость запоминается.
Сканируйте по одному товару и ждите результата в поле.

Для USB в режиме клавиатуры при распознавании начала кода запрашивается
английская раскладка ENG и выключается Caps Lock. Данные сканера
разбираются по английской раскладке независимо от языка Windows.
Уже набранные символы проверяются перед заменой. Обычный ввод текста,
не похожий на начало маркировки, не переключает язык.
COM передаёт данные независимо от раскладки; при обработке также
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
COM вставляет код только в пустое поле. Для USB проверяется точное
совпадение считанного текста в конце поля и положение курсора.
Через MSAA весь текст поля должен совпадать со считанным кодом.
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
No port, interface, or baud selection is required. USB/HID input is
monitored automatically. Available COM ports are opened for reading,
without sending commands. Busy ports are retried every three seconds;
device discovery runs approximately every five seconds.
COM rates: 9600, 115200, 38400, 19200, 57600, 4800, 2400, 1200 baud.
Format: 8N1 without flow control. Initial detection may require several
scans. Successful parameters are remembered. Scan one item at a time.

The application requests English input and switches Caps Lock off when
a marking-code prefix is recognized. Scanner keys are decoded using the
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
Test one real item before warehouse-wide use. Physical ATOL SB3100 BT
and the live Ozon field require validation at the workplace.

Supported format: GTIN 14 digits, serial 13 characters, key 4 characters,
verification block 44 characters ending in '='. Separators are prepared;
missing characters and letter case are not guessed. This is not a code
authenticity/status check. No code history is stored. The clipboard
is neither read nor changed.

Yandex Browser, Chrome, Edge and Opera must run without elevation.
The focused text field must expose its value through Windows accessibility.
The website URL is not checked. COM requires an empty field. USB requires
a matching typed suffix and verified caret position. With MSAA, the whole
existing value must match the scan, allowing a whole-field replacement.
The result is read back after writing. Inaccessible fields, focus changes,
and unverified values produce a failure message. A successful field write
does not mean Ozon accepted the code. Check the field before rescanning.

UNINSTALL
Exit the application, then uninstall it in Windows Settings > Apps.
Program files, shortcuts, and its startup entry are removed. Preferences
in %LOCALAPPDATA%\OzonMarkingConverter remain for reinstallation.

by PEI
