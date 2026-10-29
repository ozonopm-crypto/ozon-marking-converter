using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace OzonMarkingDesktop
{
    public sealed class UpdateRelease
    {
        public string version { get; set; }
        public string url { get; set; }
        public string sha256 { get; set; }
        public int size { get; set; }
        public string notes { get; set; }
    }
    public static class UpdatePolicy
    {
        public const string Root = "https://raw.githubusercontent.com/ozonopm-crypto/ozon-marking-converter/main/";
        public const string Feed = Root + "windows-updates.json";
        public static UpdateRelease Parse(string json)
        {
            if (String.IsNullOrEmpty(json) || json.Length > 65536) throw new InvalidDataException("Неверный файл обновления.");
            UpdateRelease value = new JavaScriptSerializer { MaxJsonLength = 65536 }.Deserialize<UpdateRelease>(json);
            Version parsed;
            if (value == null || !Regex.IsMatch(value.version ?? "", @"\A[0-9]{1,4}\.[0-9]{1,4}\.[0-9]{1,4}\z") || !Version.TryParse(value.version, out parsed)) throw new InvalidDataException("Неверная версия обновления.");
            string expected = Root + "downloads/OzonMarkingConverter-Setup-" + value.version + ".exe";
            if (value.url != expected || !Regex.IsMatch(value.sha256 ?? "", @"\A[0-9a-fA-F]{64}\z") || value.size < 100000 || value.size > 50000000) throw new InvalidDataException("Некорректный адрес или параметры установщика.");
            value.notes = value.notes ?? "Новая версия программы.";
            if (value.notes.Length > 4000) throw new InvalidDataException("Слишком длинное описание обновления.");
            return value;
        }
        public static bool Newer(string latest, Version current) { return new Version(latest) > new Version(current.Major, current.Minor, current.Build); }
        public static void Verify(string path, UpdateRelease release)
        {
            FileInfo file = new FileInfo(path);
            if (!file.Exists || file.Length != release.size) throw new InvalidDataException("Установщик скачан не полностью.");
            using (FileStream stream = File.OpenRead(path))
            {
                if (stream.ReadByte() != 'M' || stream.ReadByte() != 'Z') throw new InvalidDataException("Неверный формат установщика.");
                stream.Position = 0;
                using (SHA256 hash = SHA256.Create())
                    if (!String.Equals(BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", ""), release.sha256, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Контрольная сумма не совпадает. Обновление отменено.");
            }
        }
    }

    internal static class Updates
    {
        private static HttpWebRequest Request(string url)
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.AllowAutoRedirect = false; request.Timeout = 20000; request.ReadWriteTimeout = 20000;
            request.UserAgent = "PEI-OzonMarkingConverter/" + typeof(Updates).Assembly.GetName().Version;
            request.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
            return request;
        }
        public static UpdateRelease Check()
        {
            using (WebResponse response = Request(UpdatePolicy.Feed + "?t=" + DateTime.UtcNow.Ticks).GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (MemoryStream data = new MemoryStream())
            {
                byte[] buffer = new byte[4096]; int read;
                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    if (data.Length + read > 65536) throw new InvalidDataException("Файл обновлений слишком большой.");
                    data.Write(buffer, 0, read);
                }
                return UpdatePolicy.Parse(Encoding.UTF8.GetString(data.ToArray()).TrimStart('\ufeff'));
            }
        }
        public static string Download(UpdateRelease release, Action<int> progress)
        {
            // Revalidate, even if called with an object obtained outside the feed parser.
            release = UpdatePolicy.Parse(new JavaScriptSerializer().Serialize(release));
            string root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OzonMarkingConverter", "updates", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            string path = Path.Combine(root, "OzonMarkingConverter-Setup-" + release.version + ".exe");
            string partial = path + ".part";
            try
            {
                using (WebResponse response = Request(release.url).GetResponse())
                using (Stream input = response.GetResponseStream())
                using (FileStream output = new FileStream(partial, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    byte[] buffer = new byte[65536]; int read; long total = 0;
                    while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        total += read;
                        if (total > release.size) throw new InvalidDataException("Размер обновления превышает ожидаемый.");
                        output.Write(buffer, 0, read); if (progress != null) progress((int)(100L * total / release.size));
                    }
                }
                UpdatePolicy.Verify(partial, release); File.Move(partial, path); return path;
            }
            catch { if (File.Exists(partial)) File.Delete(partial); throw; }
        }
        private static string Quote(string value) { return "\"" + value + "\""; }
        public static void StartHelper(string installer, UpdateRelease release)
        {
            string helper = Path.Combine(Path.GetDirectoryName(installer), "PEI-Update.exe");
            File.Copy(System.Windows.Forms.Application.ExecutablePath, helper, false);
            string config = System.Windows.Forms.Application.ExecutablePath + ".config";
            if (File.Exists(config)) File.Copy(config, helper + ".config", false);
            Process.Start(new ProcessStartInfo(helper, "--apply-update " + Process.GetCurrentProcess().Id + " " + Quote(installer) + " " + release.version + " " + release.sha256 + " " + release.size + " " + Quote(AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\'))) { UseShellExecute = false, CreateNoWindow = true });
        }
        public static void Apply(string[] args)
        {
            try
            {
                if (args.Length != 7) throw new InvalidDataException("Неверные параметры обновления.");
                string path = Path.GetFullPath(args[2]);
                string root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OzonMarkingConverter", "updates") + Path.DirectorySeparatorChar;
                if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Неверная папка обновления.");
                UpdateRelease release = UpdatePolicy.Parse(new JavaScriptSerializer().Serialize(new UpdateRelease { version = args[3], url = UpdatePolicy.Root + "downloads/OzonMarkingConverter-Setup-" + args[3] + ".exe", sha256 = args[4], size = Int32.Parse(args[5]) }));
                try { using (Process previous = Process.GetProcessById(Int32.Parse(args[1]))) if (!previous.WaitForExit(30000)) throw new IOException("Программа ещё не завершила работу. Повторите обновление."); }
                catch (ArgumentException) { }
                UpdatePolicy.Verify(path, release);
                string target = Path.GetFullPath(args[6]).TrimEnd('\\');
                FileVersionInfo installed = FileVersionInfo.GetVersionInfo(Path.Combine(target, "OzonMarkingConverter.exe"));
                if (installed.CompanyName != "by PEI") throw new InvalidDataException("Не найдена установленная программа by PEI.");
                Process.Start(new ProcessStartInfo(path, "/SILENT /SUPPRESSMSGBOXES /NORESTART /SP- /AUTORESTART=1 /DIR=" + Quote(target)) { UseShellExecute = true });
            }
            catch (Exception error) { System.Windows.Forms.MessageBox.Show("Обновление не выполнено. " + error.Message + "\r\nОткройте программу с рабочего стола и повторите попытку.", "Обновление — by PEI"); }
        }
    }
}
