using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyTitle("Преобразователь кода для Ozon - Windows")]
[assembly: AssemblyDescription("Локальная подготовка разделителей кода маркировки")]
[assembly: AssemblyCompany("by PEI")]
[assembly: AssemblyCopyright("by PEI")]
[assembly: AssemblyVersion("1.2.3.0")]
[assembly: AssemblyFileVersion("1.2.3.0")]

namespace OzonMarkingDesktop
{
    internal sealed class Preferences
    {
        public bool Enabled = true;
        public int Gap = 500;
        public readonly Dictionary<string, int> Rates = new Dictionary<string, int>();
        private readonly string path;
        public Preferences(string directory)
        {
            path = Path.Combine(directory, "settings.ini");
            try
            {
                foreach (string line in File.ReadAllLines(path))
                {
                    string[] item = line.Split('=');
                    if (item.Length != 2) continue;
                    if (item[0] == "enabled") Enabled = item[1] == "true";
                    int number;
                    if (item[0] == "gap" && Int32.TryParse(item[1], out number)) Gap = Math.Max(80, Math.Min(500, number));
                    if (item[0].StartsWith("baud_") && Int32.TryParse(item[1], out number)) Rates[item[0]] = number;
                }
            }
            catch { }
        }
        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                List<string> lines = new List<string> { "enabled=" + (Enabled ? "true" : "false"), "gap=" + Gap.ToString() };
                foreach (KeyValuePair<string, int> item in Rates) lines.Add(item.Key + "=" + item.Value);
                File.WriteAllLines(path, lines.ToArray());
            }
            catch { }
        }
        public static bool StartupEnabled()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
                return key != null && key.GetValue("OzonMarkingConverter") != null;
        }
        public static void SetStartup(bool enabled)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
            {
                if (enabled) key.SetValue("OzonMarkingConverter", "\"" + Application.ExecutablePath + "\" --background", RegistryValueKind.String);
                else key.DeleteValue("OzonMarkingConverter", false);
            }
        }
    }

    internal sealed class MainWindow : Form
    {
        private readonly Preferences preferences;
        private readonly bool background;
        private readonly bool browserVerification;
        private readonly bool scannerVerification;
        private TextBox diagnostics;
        private ScannerHook scanner;
        private NotifyIcon tray;
        private Button toggle;
        private Label status;
        private Label countLabel;
        private TextBox testInput;
        private TextBox testOutput;
        private Label testStatus;
        private CheckBox testAuto;
        private CheckBox startup;
        private bool closing;
        private bool changingStartup;
        private int count;
        private ToolStripMenuItem trayToggle;
        private DeviceMonitor devices;
        private TextBox deviceList;
        private Label connection;
        private Label updateStatus;
        private Button updateButton;
        private UpdateRelease availableUpdate;
        private bool checkingUpdate, installingUpdate;
        private System.Windows.Forms.Timer updateTimer;
        private readonly Color ink = Color.FromArgb(23, 35, 55);
        private readonly Color muted = Color.FromArgb(82, 97, 119);
        private readonly Color blue = Color.FromArgb(23, 92, 211);
        public MainWindow(Preferences settings, bool startBackground, bool verifyBrowser = false, bool verifyScanner = false)
        {
            scannerVerification = verifyScanner;
            browserVerification = verifyBrowser;
            preferences = settings; background = startBackground;
            Text = "Преобразователь кода для Ozon — Windows" + (verifyBrowser ? " [Проверка браузера]" : verifyScanner ? " [Диагностика сканера]" : "");
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(780, 730);
            MinimumSize = new Size(740, 720);
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = new Font("Segoe UI", 10f);
            BackColor = Color.White;
            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scanner.ico");
            if (File.Exists(iconPath)) Icon = new Icon(iconPath);

            TableLayoutPanel root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(22), ColumnCount = 1, RowCount = 5 };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 73));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            Controls.Add(root);
            Panel header = new Panel { Dock = DockStyle.Fill };
            Label title = LabelAt("Преобразователь кода для Ozon", 0, 0, 710, 35, 20, true);
            Label subtitle = LabelAt("Windows 1.2.3 · by PEI · USB/HID, COM и Bluetooth через Windows", 1, 42, 710, 25, 10, false);
            if (scannerVerification) subtitle.Text = "Проверка исправления / Fix verification · by PEI";
            subtitle.ForeColor = muted; header.Controls.Add(title); header.Controls.Add(subtitle); root.Controls.Add(header, 0, 0);

            FlowLayoutPanel actions = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Padding = new Padding(0, 1, 0, 0) };
            toggle = ButtonOf("Включить обработку", 237, true);
            toggle.Click += delegate { SetEnabled(scanner != null && !scanner.Enabled); };
            Button hide = ButtonOf("Свернуть в трей", 190, false); hide.Click += delegate { Hide(); };
            Button quit = ButtonOf("Завершить работу", 200, false); quit.Click += delegate { ExitApp(); };
            actions.Controls.Add(toggle); actions.Controls.Add(hide); actions.Controls.Add(quit); root.Controls.Add(actions, 0, 1);

            status = new Label { Dock = DockStyle.Fill, Text = "Обработка на паузе. Нажмите «Включить обработку».", Padding = new Padding(13, 10, 13, 5), BackColor = Color.FromArgb(241, 245, 250), ForeColor = ink, AutoEllipsis = true, AccessibleName = "Состояние обработки" };
            root.Controls.Add(status, 0, 2);

            TabControl tabs = new TabControl { Dock = DockStyle.Fill, Padding = new Point(18, 8) };
            TabPage work = new TabPage("Работа") { BackColor = Color.White, Padding = new Padding(17) };
            TabPage check = new TabPage("Проверка кода") { BackColor = Color.White, Padding = new Padding(17) };
            TabPage options = new TabPage("Настройки") { BackColor = Color.White, Padding = new Padding(17) };
            tabs.TabPages.Add(work); tabs.TabPages.Add(check); tabs.TabPages.Add(options); root.Controls.Add(tabs, 0, 3);
            BuildWork(work); BuildTest(check); BuildOptions(options);
            TabPage hardware = new TabPage("Сканер") { BackColor = Color.White };
            TabPage updates = new TabPage("Обновления") { BackColor = Color.White };
            tabs.TabPages.Add(hardware); tabs.TabPages.Add(updates);
            BuildDevices(hardware); BuildUpdates(updates);
            TabPage diagnosticPage = new TabPage("Диагностика") { BackColor = Color.White, Padding = new Padding(17) };
            diagnostics = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Font = new Font("Consolas", 11), AccessibleName = "Отчёт диагностики", Text = "Отчёт появится после сканирования. Сам код в отчёт не включается." };
            diagnosticPage.Controls.Add(diagnostics); tabs.TabPages.Add(diagnosticPage);
            tabs.Layout += delegate { FitPages(tabs); };
            Shown += delegate { FitPages(tabs); };
            countLabel = new Label { Dock = DockStyle.Fill, ForeColor = muted, TextAlign = ContentAlignment.BottomLeft, Text = "Исправлено за этот запуск: 0. История кодов не сохраняется." };
            root.Controls.Add(countLabel, 0, 4);

            tray = new NotifyIcon { Icon = Icon, Text = "Преобразователь кода для Ozon", Visible = true };
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("Открыть", null, delegate { OpenWindow(); });
            trayToggle = new ToolStripMenuItem("Включить обработку"); trayToggle.Click += delegate { SetEnabled(scanner != null && !scanner.Enabled); }; menu.Items.Add(trayToggle);
            menu.Items.Add("Выход", null, delegate { ExitApp(); }); tray.ContextMenuStrip = menu;
            tray.DoubleClick += delegate { OpenWindow(); };
            FormClosing += delegate(object sender, FormClosingEventArgs e) { if (!closing && e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; Hide(); } };
            FormClosed += delegate { SystemEvents.PowerModeChanged -= PowerChanged; SystemEvents.SessionSwitch -= SessionChanged; if (updateTimer != null) updateTimer.Dispose(); if (devices != null) devices.Dispose(); if (scanner != null) scanner.Dispose(); tray.Visible = false; tray.Dispose(); };
            Activated += delegate { if (scanner != null) scanner.SyncKeyboard(); };
            Load += Loaded;
        }
        private Label LabelAt(string value, int x, int y, int width, int height, float size, bool bold)
        {
            return new Label { Text = value, Left = x, Top = y, Width = width, Height = height, Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular), ForeColor = ink };
        }
        private Button ButtonOf(string value, int width, bool primary)
        {
            Button button = new Button { Text = value, Width = width, Height = 40, Margin = new Padding(0, 0, 9, 0), FlatStyle = FlatStyle.Flat, BackColor = primary ? blue : Color.FromArgb(243, 246, 250), ForeColor = primary ? Color.White : ink };
            button.FlatAppearance.BorderColor = primary ? blue : Color.FromArgb(207, 217, 229); return button;
        }
        private void FitPages(TabControl tabs)
        {
            foreach (TabPage page in tabs.TabPages)
                foreach (Control control in page.Controls)
                    if (control.Width >= 500) control.Width = Math.Max(500, page.ClientSize.Width - 34);
        }
        private void BuildWork(TabPage page)
        {
            Label intro = LabelAt("1. Отключите прежнее расширение преобразователя в браузере.\r\n\r\n2. Включите обработку в этой программе.\r\n\r\n3. Откройте Ozon, очистите поле маркировки и нажмите на него.\r\n\r\n4. Отсканируйте товар. Затем проверьте ответ Ozon.", 17, 20, 680, 195, 11, false);
            intro.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; page.Controls.Add(intro);
            Label keyboard = LabelAt("ENG И CAPS LOCK ВЫКЛЮЧЕН — АВТОМАТИЧЕСКИ ПРИ СКАНИРОВАНИИ.", 17, 223, 680, 48, 11, true);
            keyboard.Anchor = intro.Anchor; page.Controls.Add(keyboard);
            Label details = LabelAt("Работает в обычных текстовых полях Яндекс Браузера, Chrome, Edge и Opera. Перед заменой программа проверяет активное поле и считанный код. Enter от сканера не подтверждает товар за сотрудника.\r\n\r\nКрестик скрывает окно. Программа продолжает работать рядом с часами Windows. Для остановки нажмите «Пауза» или «Завершить работу».", 17, 273, 680, 130, 10, false);
            details.Anchor = intro.Anchor; details.ForeColor = muted; page.Controls.Add(details);
        }
        private void BuildTest(TabPage page)
        {
            Label hint = LabelAt("Для проверки сканера включите обработку, нажмите на поле и сканируйте.\r\nКнопки ниже отдельно проверяют преобразование искусственного примера.", 17, 14, 680, 43, 10, false); hint.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; page.Controls.Add(hint);
            testInput = new TextBox { Left = 17, Top = 65, Width = 680, Height = 77, Multiline = true, AcceptsTab = false, ScrollBars = ScrollBars.Vertical, Font = new Font("Consolas", 11), AccessibleName = "Тестовый скан", Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            page.Controls.Add(testInput);
            testAuto = new CheckBox { Left = 17, Top = 150, Width = 680, Height = 27, Checked = true, Text = "Разрешить автоматическую обработку сканера в этом тестовом поле", Anchor = hint.Anchor }; page.Controls.Add(testAuto);
            FlowLayoutPanel row = new FlowLayoutPanel { Left = 17, Top = 187, Width = 680, Height = 48, WrapContents = false, Anchor = hint.Anchor };
            Button sample = ButtonOf("Тестовый пример", 150, false); sample.Click += delegate { testInput.Text = MarkingCode.DemoRaw; testOutput.Clear(); testStatus.Text = "Вставлен искусственный код. Нажмите «Преобразовать»."; testInput.Focus(); testInput.Select(testInput.TextLength, 0); };
            Button convert = ButtonOf("Преобразовать", 150, true); convert.Click += delegate { CodeResult result = MarkingCode.Normalize(testInput.Text); testOutput.Text = result.Ok ? result.Code : ""; testStatus.Text = result.Ok ? "Тест пройден: разделители подготовлены. Это не проверка подлинности товара." : result.Message; };
            Button windowsTest = ButtonOf("Тест автозамены", 170, false); windowsTest.Click += delegate
            {
                if (scanner == null || !scanner.Enabled) { testStatus.Text = "Для проверки автозамены сначала включите обработку."; return; }
                testInput.Text = MarkingCode.DemoRaw; testOutput.Clear(); testInput.Focus(); testInput.Select(testInput.TextLength, 0);
                testStatus.Text = "Проверяю чтение тестового поля и ввод Windows…";
                scanner.TestReplacement(testInput.Text, Handle);
            };
            Button clear = ButtonOf("Стереть код", 120, false); clear.Click += delegate { testInput.Clear(); testOutput.Clear(); testStatus.Text = "Ожидаю тестовый скан."; testInput.Focus(); };
            row.Controls.Add(sample); row.Controls.Add(convert); row.Controls.Add(windowsTest); row.Controls.Add(clear); page.Controls.Add(row);
            testOutput = new TextBox { Left = 17, Top = 242, Width = 680, Height = 76, ReadOnly = true, Multiline = true, Font = new Font("Consolas", 11), AccessibleName = "Результат преобразования", Anchor = hint.Anchor };
            page.Controls.Add(testOutput);
            testStatus = LabelAt("Ожидаю тестовый скан. Пример не относится к реальному товару.", 17, 332, 680, 68, 10, false); testStatus.Anchor = hint.Anchor; page.Controls.Add(testStatus);
            testStatus.Top = 375; testStatus.Height = 30;
            Button browserCheck = ButtonOf("Тест в браузере", 200, false); browserCheck.Left = 17; browserCheck.Top = 332; browserCheck.Height = 34;
            browserCheck.Click += delegate
            {
                if (scanner == null || !scanner.Enabled) { SetStatus("Сначала включите обработку.", true); return; }
                SetStatus("За 5 секунд перейдите в браузер и нажмите на поле с искусственным примером PEI. Настоящие коды этот тест не заменяет.", false);
                System.Windows.Forms.Timer wait = new System.Windows.Forms.Timer { Interval = 5000 };
                wait.Tick += delegate { wait.Stop(); wait.Dispose(); if (!closing && scanner != null) scanner.TestBrowserReplacement(Native.GetForegroundWindow()); };
                wait.Start();
            }; page.Controls.Add(browserCheck);
            Button mismatch = ButtonOf("Тест несовпадения", 220, false); mismatch.Left = 236; mismatch.Top = 332; mismatch.Height = 34;
            mismatch.Click += delegate
            {
                if (scanner == null || !scanner.Enabled) { SetStatus("Сначала включите обработку.", true); return; }
                testInput.Text = MarkingCode.DemoRaw; testInput.Focus(); testInput.Select(testInput.TextLength, 0);
                scanner.TestMismatch(Handle);
            }; page.Controls.Add(mismatch);
        }
        private void BuildOptions(TabPage page)
        {
            startup = new CheckBox { Left = 17, Top = 23, Width = 670, Height = 30, Text = "Запускать вместе с Windows (для текущего пользователя)" };
            startup.Enabled = !browserVerification && !scannerVerification;
            try { startup.Checked = Preferences.StartupEnabled(); } catch { }
            startup.CheckedChanged += delegate
            {
                if (changingStartup) return;
                try { Preferences.SetStartup(startup.Checked); SetStatus(startup.Checked ? "Автозапуск включён. Программа запустится в трее после входа в Windows." : "Автозапуск выключен.", false); }
                catch
                {
                    changingStartup = true;
                    try { startup.Checked = Preferences.StartupEnabled(); } catch { startup.Checked = false; }
                    finally { changingStartup = false; }
                    SetStatus("Не удалось изменить автозапуск. Обратитесь к администратору компьютера.", true);
                }
            };
            page.Controls.Add(startup);
            Label note = LabelAt("Параметры подключения подбираются автоматически. Выбирать USB, COM, номер порта или скорость не требуется.\r\n\r\nПоддерживаемая структура: GTIN 14 цифр, серийный номер 13 знаков, ключ 4 знака, проверочная часть 44 знака. Другие форматы программа не меняет.\r\n\r\nКоды обрабатываются локально и не передаются в интернет. Сохраняются настройки и найденные параметры COM. GitHub используется только для обновления программы.\r\n\r\nВерсия для Windows: 1.2.3\r\nby PEI", 17, 90, 680, 300, 10, false);
            note.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; note.ForeColor = muted; page.Controls.Add(note);
        }
        private void Loaded(object sender, EventArgs args)
        {
            try
            {
                scanner = new ScannerHook(delegate { return testAuto.Checked && testInput.Focused; }, !browserVerification, scannerVerification);
                scanner.SetTiming(500, 650);
                scanner.Status += SetStatus;
                scanner.Diagnosis += delegate(string report) { OnUi(delegate { diagnostics.Text = report; }); };
                scanner.Converted += delegate(string prepared, bool serial) { OnUi(delegate { count++; if (devices != null && !serial) devices.ScanSucceeded(); countLabel.Text = "Исправлено за этот запуск: " + count.ToString() + ". История кодов не сохраняется."; if (testInput.Focused) { testOutput.Text = prepared; testStatus.Text = "Автозамена Windows выполнена. Теперь проверьте реальный сканер в этом поле."; } }); };
                scanner.FailedInput += delegate { OnUi(delegate { SetEnabled(false); }); };
                if (!browserVerification && !scannerVerification)
                {
                devices = new DeviceMonitor(scanner, preferences.Rates);
                devices.DevicesChanged += delegate(ScannerDevice[] list) { OnUi(delegate { ShowDevices(list); }); };
                devices.Connection += delegate(string text) { OnUi(delegate { connection.Text = text; }); };
                devices.Learned += delegate(string key, int speed) { OnUi(delegate { preferences.Rates[key] = speed; preferences.Save(); }); };
                }
                SetEnabled(preferences.Enabled);
                SystemEvents.PowerModeChanged += PowerChanged;
                SystemEvents.SessionSwitch += SessionChanged;
            }
            catch { toggle.Enabled = false; SetStatus("Windows не разрешила подключить обработчик клавиатуры. Автоматическая обработка недоступна; тестовый преобразователь работает.", true); }
            updateTimer = new System.Windows.Forms.Timer { Interval = 15000 };
            updateTimer.Tick += delegate { updateTimer.Interval = 21600000; CheckUpdate(false); };
            if (!browserVerification && !scannerVerification) updateTimer.Start();
            else updateButton.Enabled = false;
            if (background) BeginInvoke(new Action(delegate { Hide(); }));
        }
        private void PowerChanged(object sender, PowerModeChangedEventArgs args)
        {
            if (args.Mode == PowerModes.Resume) OnUi(delegate { if (scanner != null) scanner.RenewHook(); if (devices != null) devices.Refresh(); });
        }
        private void SessionChanged(object sender, SessionSwitchEventArgs args)
        {
            if (args.Reason == SessionSwitchReason.SessionUnlock) OnUi(delegate { if (scanner != null) scanner.SyncKeyboard(); });
        }
        private void BuildDevices(TabPage page)
        {
            connection = LabelAt("Ищу подключённые устройства…", 17, 16, 680, 58, 10, true); page.Controls.Add(connection);
            deviceList = new TextBox { Left = 17, Top = 80, Width = 680, Height = 140, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, AccessibleName = "Найденные устройства" }; page.Controls.Add(deviceList);
            Label note = LabelAt("USB/HID работает автоматически. COM открывается только для распознанного или ранее проверенного сканера. Другие порты не открываются; скорость без поступления данных не переключается.\r\n\r\nСписок устройств обновляется при подключении/отключении и раз в минуту. Для неопознанного COM-сканера может потребоваться драйвер производителя. Формат COM — 8N1.\r\n\r\nBluetooth работает через базу или после сопряжения в Windows как клавиатура / COM.", 17, 245, 680, 175, 10, false); note.ForeColor = muted; page.Controls.Add(note);
        }
        private void ShowDevices(ScannerDevice[] list)
        {
            List<string> lines = new List<string>();
            foreach (ScannerDevice item in list)
            {
                string name = item.Name;
                if (name.StartsWith(@"\\?\")) name = "Клавиатурное HID-устройство";
                lines.Add(item.Keyboard ? "USB/HID: " + name : item.Port + ": " + name);
            }
            deviceList.Lines = lines.Count == 0 ? new string[] { "Windows пока не сообщает подключённых клавиатурных и COM-устройств." } : lines.ToArray();
        }
        private void BuildUpdates(TabPage page)
        {
            page.Controls.Add(LabelAt("Обновление программы", 17, 20, 680, 35, 16, true));
            updateStatus = LabelAt("Установлена версия 1.2.3. Проверяю наличие новых версий после запуска и каждые 6 часов. Интернет нужен только для обновлений.", 17, 70, 680, 170, 11, false); page.Controls.Add(updateStatus);
            updateButton = ButtonOf("Проверить обновления", 270, true); updateButton.Left = 17; updateButton.Top = 250;
            updateButton.Click += delegate { if (availableUpdate == null) CheckUpdate(true); else InstallUpdate(); }; page.Controls.Add(updateButton);
            page.Controls.Add(LabelAt("Когда появится новая версия, здесь будет кнопка «Обновить». Программа сама скачает установщик, закроется и запустится после установки. Настройки и ярлыки сохранятся. Скачивать файлы вручную не требуется.\r\n\r\nИсточник: GitHub · ozonopm-crypto/ozon-marking-converter\r\nby PEI", 17, 310, 680, 115, 10, false));
        }
        private void CheckUpdate(bool manual)
        {
            if (checkingUpdate || installingUpdate) return;
            checkingUpdate = true; updateButton.Enabled = false; updateStatus.Text = "Проверяю обновления…";
            Task.Factory.StartNew(delegate
            {
                try
                {
                    UpdateRelease release = Updates.Check();
                    OnUi(delegate
                    {
                        availableUpdate = UpdatePolicy.Newer(release.version, typeof(MainWindow).Assembly.GetName().Version) ? release : null;
                        updateButton.Text = availableUpdate == null ? "Проверить обновления" : "Обновить до " + release.version;
                        updateStatus.Text = availableUpdate == null ? "Установлена актуальная версия 1.2.3. Проверено: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm") : "Доступна версия " + release.version + ".\r\n\r\n" + release.notes;
                        if (availableUpdate != null) { tray.BalloonTipTitle = "Доступно обновление " + release.version; tray.BalloonTipText = "Откройте программу → Обновления → Обновить."; tray.ShowBalloonTip(8000); }
                    });
                }
                catch { OnUi(delegate { updateStatus.Text = "Не удалось проверить обновления. Проверьте доступ к GitHub и повторите попытку. Обработка сканера продолжает работать."; }); }
                finally { OnUi(delegate { checkingUpdate = false; updateButton.Enabled = true; }); }
            });
        }
        private void InstallUpdate()
        {
            if (availableUpdate == null || installingUpdate) return;
            installingUpdate = true; updateButton.Enabled = false;
            UpdateRelease release = availableUpdate;
            Task.Factory.StartNew(delegate
            {
                try
                {
                    string installer = Updates.Download(release, delegate(int progress) { OnUi(delegate { updateStatus.Text = "Скачиваю обновление: " + progress + "%"; }); });
                    OnUi(delegate
                    {
                        try { Updates.StartHelper(installer, release); ExitApp(); }
                        catch { installingUpdate = false; updateButton.Enabled = true; updateStatus.Text = "Не удалось запустить установку. Программа продолжает работать. Повторите попытку."; }
                    });
                }
                catch (Exception error) { OnUi(delegate { installingUpdate = false; updateButton.Enabled = true; updateStatus.Text = "Обновление отменено: " + error.Message + "\r\nПрограмма продолжает работать. Повторите попытку."; }); }
            });
        }
        private void OnUi(Action action)
        {
            if (IsDisposed || Disposing || !IsHandleCreated) return;
            try { if (InvokeRequired) BeginInvoke(action); else action(); } catch (InvalidOperationException) { }
        }
        private void SetStatus(string value, bool error)
        {
            OnUi(delegate { status.Text = value; status.ForeColor = error ? Color.FromArgb(159, 38, 30) : ink; status.BackColor = error ? Color.FromArgb(255, 242, 239) : Color.FromArgb(241, 245, 250); });
        }
        private void SetEnabled(bool active)
        {
            if (scanner == null) return;
            scanner.Enabled = active; preferences.Enabled = active; preferences.Save();
            if (devices != null) devices.SetActive(active);
            toggle.Text = active ? "Пауза" : "Включить обработку";
            trayToggle.Text = active ? "Пауза" : "Включить обработку";
            SetStatus(active ? "Обработка включена. Подключение, английская раскладка и выключение Caps Lock — автоматически. Сканируйте в поле маркировки." : "Обработка на паузе. Нажмите «Включить обработку».", false);
        }
        private void OpenWindow() { Show(); WindowState = FormWindowState.Normal; Activate(); }
        private void ExitApp() { closing = true; if (scanner != null) scanner.Enabled = false; Close(); }
    }

    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--apply-update") { Updates.Apply(args); return; }
            bool created;
            bool verifyBrowser = Array.IndexOf(args, "--verify-browser") >= 0;
            bool verifyScanner = Array.IndexOf(args, "--verify-scanner") >= 0;
            using (Mutex single = new Mutex(true, @"Local\OzonMarkingDesktop-1" + (verifyBrowser ? ".BrowserVerification" : verifyScanner ? ".ScannerVerification" : ""), out created))
            {
                if (!created) { MessageBox.Show("Программа уже запущена. Откройте значок красного сканера рядом с часами Windows.", "Преобразователь кода для Ozon"); return; }
                Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
                string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OzonMarkingConverter");
                bool background = false;
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i] == "--background") background = true;
                    if (args[i] == "--settings-dir" && i + 1 < args.Length) directory = Path.GetFullPath(args[++i]);
                }
                Application.Run(new MainWindow(new Preferences(directory), background, verifyBrowser, verifyScanner));
                single.ReleaseMutex();
            }
        }
    }
}
