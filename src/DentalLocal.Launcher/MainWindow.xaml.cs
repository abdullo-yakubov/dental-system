using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace DentalLocal.Launcher;

public partial class MainWindow : Window
{
    private Process? _serverProcess;
    private DispatcherTimer? _healthCheckTimer;
    private System.Windows.Forms.NotifyIcon? _notifyIcon;
    private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(2) };
    private bool _isExplicitExit;
    private string _serverUrl = "http://localhost:5000";
    private string? _serverExePath;
    private CancellationTokenSource? _showListenerCts;

    // Дедлайн на первичный запуск (см. промпт 9 аудита зависаний) — вместо бесконечного "Ожидание
    // веб-сервисов..." при упавшем/зависшем старте показываем причину и хвост лога через 90 секунд.
    private static readonly TimeSpan StartupDeadline = TimeSpan.FromSeconds(90);
    private DateTime? _startupDeadlineUtc;
    private bool _hasEverBeenReady;
    private bool _startupFailureShown;

    // Раздел 21.8 ТЗ: если сервер не найден рядом с лаунчером (не первый запуск после установки,
    // а, например, перенос установки на другой диск) — путь, указанный пользователем вручную,
    // запоминается здесь и используется при последующих запусках без повторного запроса.
    private static readonly string ConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DentalLocal", "launcher.config.json");

    public MainWindow()
    {
        InitializeComponent();
        InitializeTray();
        StartServer();
        StartHealthCheckTimer();
    }

    private void InitializeTray()
    {
        _notifyIcon = new System.Windows.Forms.NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Shield, // Standard system icon that is guaranteed to exist
            Text = "DentalLocal Control Panel",
            Visible = true
        };

        _notifyIcon.DoubleClick += (s, e) => ShowWindow();

        var contextMenu = new System.Windows.Forms.ContextMenuStrip();
        contextMenu.Items.Add("Открыть Dental IS", null, (s, e) => OpenBrowser());
        contextMenu.Items.Add("Перезапустить сервер", null, (s, e) => RestartServer());
        contextMenu.Items.Add("-");
        contextMenu.Items.Add("Выйти", null, (s, e) => ExitApplication());

        _notifyIcon.ContextMenuStrip = contextMenu;
    }

    private void StartServer()
    {
        try
        {
            if (_serverProcess != null && !_serverProcess.HasExited)
                return;

            StatusText.Text = "Запуск сервера...";
            StatusDot.Fill = System.Windows.Media.Brushes.Orange;
            DetailsText.Text = "Поиск исполняемого файла сервера и запуск фонового процесса...";

            // Locate server exe
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var serverExePath = "";

            // Путь, указанный пользователем вручную ранее (см. BtnLocateServer_Click), имеет приоритет —
            // например, после переноса установки на другой диск/папку.
            var savedPath = LoadSavedServerPath();
            if (!string.IsNullOrEmpty(savedPath) && File.Exists(savedPath))
            {
                serverExePath = savedPath;
            }
            else
            {
                var candidates = new[]
                {
                    // Раскладка установщика (см. installer/DentalLocal.iss): Server лежит в корне {app},
                    // Launcher.exe — в {app}\Launcher, поэтому сервер находится на уровень выше.
                    Path.Combine(baseDir, "..", "DentalLocal.Server.exe"),
                    Path.Combine(baseDir, "DentalLocal.Server.exe"),
                    Path.Combine(baseDir, "..", "DentalLocal.Server", "DentalLocal.Server.exe"),
                    Path.Combine(baseDir, "..", "..", "..", "..", "src", "DentalLocal.Server", "bin", "Debug", "net10.0", "DentalLocal.Server.exe")
                };

                foreach (var candidate in candidates)
                {
                    if (File.Exists(candidate))
                    {
                        serverExePath = Path.GetFullPath(candidate);
                        break;
                    }
                }
            }

            if (string.IsNullOrEmpty(serverExePath))
            {
                // Раздел 21.8 ТЗ: первый запуск, когда сервер не найден автоматически — не просто
                // сообщение об ошибке, а конкретное действие для пользователя.
                StatusText.Text = "Сервер не найден";
                StatusDot.Fill = System.Windows.Media.Brushes.Red;
                DetailsText.Text = "Не удалось найти DentalLocal.Server.exe рядом с лаунчером. " +
                    "Если установка была перенесена в другую папку — укажите файл сервера вручную кнопкой ниже.";
                BtnLocateServer.Visibility = Visibility.Visible;
                return;
            }

            BtnLocateServer.Visibility = Visibility.Collapsed;
            BtnOpenLogs.Visibility = Visibility.Collapsed;

            _serverExePath = serverExePath;
            _startupDeadlineUtc = DateTime.UtcNow.Add(StartupDeadline);
            _hasEverBeenReady = false;
            _startupFailureShown = false;

            var startInfo = new ProcessStartInfo
            {
                FileName = serverExePath,
                WorkingDirectory = Path.GetDirectoryName(serverExePath),
                CreateNoWindow = true,
                UseShellExecute = false
            };

            _serverProcess = Process.Start(startInfo);
            DetailsText.Text = $"Процесс сервера запущен (PID: {_serverProcess?.Id}). Ожидание инициализации...";

            _ = ResolveServerPortAsync(Path.GetDirectoryName(serverExePath)!);
        }
        catch (Exception ex)
        {
            StatusText.Text = "Ошибка запуска";
            StatusDot.Fill = System.Windows.Media.Brushes.Red;
            DetailsText.Text = $"Произошла ошибка при запуске процесса сервера: {ex.Message}";
        }
    }

    /// <summary>
    /// Порт сервера может однажды перестать быть захардкоженной константой (например, при
    /// конфликте порта 5000) — Server пишет фактический порт в runtime.json рядом со своим .exe
    /// при старте (см. промпт 10 аудита зависаний). Опрашиваем файл несколько секунд вместо
    /// того, чтобы полагаться только на "http://localhost:5000".
    /// </summary>
    private async Task ResolveServerPortAsync(string serverDir)
    {
        var runtimeInfoPath = Path.Combine(serverDir, "runtime.json");
        var deadline = DateTime.UtcNow.AddSeconds(15);

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                if (File.Exists(runtimeInfoPath))
                {
                    using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(runtimeInfoPath));
                    if (doc.RootElement.TryGetProperty("port", out var portProp) && portProp.TryGetInt32(out var port))
                    {
                        Dispatcher.Invoke(() => _serverUrl = $"http://localhost:{port}");
                        return;
                    }
                }
            }
            catch
            {
                // файл мог быть частично записан — просто попробуем ещё раз
            }

            await Task.Delay(500);
        }
    }

    /// <summary>
    /// Фоновое ожидание сигнала от повторно запущенного экземпляра лаунчера (single-instance guard
    /// в App.xaml.cs) — второй процесс сразу завершается, а этот разворачивает окно вместо него.
    /// </summary>
    public void ListenForShowRequests(EventWaitHandle showWindowEvent)
    {
        _showListenerCts = new CancellationTokenSource();
        var token = _showListenerCts.Token;

        _ = Task.Run(() =>
        {
            while (!token.IsCancellationRequested)
            {
                if (showWindowEvent.WaitOne(1000))
                {
                    Dispatcher.Invoke(ShowWindow);
                }
            }
        }, token);
    }

    private void StartHealthCheckTimer()
    {
        _healthCheckTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _healthCheckTimer.Tick += async (s, e) => await CheckHealthAsync();
        _healthCheckTimer.Start();
    }

    private async Task CheckHealthAsync()
    {
        // Process check
        if (_serverProcess == null || _serverProcess.HasExited)
        {
            StatusText.Text = "Сервер остановлен";
            StatusDot.Fill = System.Windows.Media.Brushes.Red;
            DetailsText.Text = "Процесс сервера неожиданно завершился. Нажмите «Перезапустить» для повторной попытки.";
            BtnOpenBrowser.IsEnabled = false;
            return;
        }

        try
        {
            var response = await _httpClient.GetAsync($"{_serverUrl}/health/ready");
            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            var status = doc.RootElement.TryGetProperty("status", out var statusProp) ? statusProp.GetString() : null;
            var stage = doc.RootElement.TryGetProperty("stage", out var stageProp) ? stageProp.GetString() : null;

            if (response.IsSuccessStatusCode && status == "ready")
            {
                _hasEverBeenReady = true;
                StatusText.Text = "Сервер работает стабильно";
                StatusDot.Fill = System.Windows.Media.Brushes.Green;
                DetailsText.Text = $"Dental IS запущен и готов к работе.\nАдрес: {_serverUrl}\nПроцесс PID: {_serverProcess.Id}";
                BtnOpenBrowser.IsEnabled = true;
                return;
            }

            StatusText.Text = StageToMessage(stage);
            StatusDot.Fill = System.Windows.Media.Brushes.Orange;
            BtnOpenBrowser.IsEnabled = false;
        }
        catch
        {
            // Kestrel ещё не слушает порт (bootstrap Postgres/миграции ещё идут) — это ожидаемо
            // в начале запуска, отличаем от "зависло" только по дедлайну ниже.
            StatusText.Text = "Ожидание веб-сервисов...";
            StatusDot.Fill = System.Windows.Media.Brushes.Orange;
            BtnOpenBrowser.IsEnabled = false;
        }

        CheckStartupDeadline();
    }

    private static string StageToMessage(string? stage) => stage switch
    {
        "database" => "Запуск базы данных...",
        "migrations" => "Применение обновлений...",
        _ => "Запуск веб-сервисов..."
    };

    private void CheckStartupDeadline()
    {
        if (_hasEverBeenReady || _startupFailureShown || _startupDeadlineUtc is null)
            return;

        if (DateTime.UtcNow < _startupDeadlineUtc.Value)
            return;

        _startupFailureShown = true;

        StatusText.Text = "Не удалось запустить сервер за отведённое время";
        StatusDot.Fill = System.Windows.Media.Brushes.Red;
        BtnOpenBrowser.IsEnabled = false;
        BtnOpenLogs.Visibility = Visibility.Visible;
        DetailsText.Text = "Сервер не стал готов за 90 секунд. Последние строки лога:\n\n" + ReadLastLogLines(20) +
            "\n\nНажмите «Открыть папку логов» для подробностей или «Перезапустить».";
    }

    private string ReadLastLogLines(int count)
    {
        try
        {
            var logsDir = GetLogsDir();
            if (logsDir is null || !Directory.Exists(logsDir))
                return "(папка логов не найдена)";

            var latestLog = new DirectoryInfo(logsDir).GetFiles("log-*.txt")
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .FirstOrDefault();

            if (latestLog is null)
                return "(файлы логов ещё не созданы)";

            var lines = File.ReadAllLines(latestLog.FullName);
            return string.Join(Environment.NewLine, lines.TakeLast(count));
        }
        catch (Exception ex)
        {
            return $"(не удалось прочитать лог: {ex.Message})";
        }
    }

    private string? GetLogsDir()
    {
        if (string.IsNullOrEmpty(_serverExePath))
            return null;

        var serverDir = Path.GetDirectoryName(_serverExePath);
        return serverDir is null ? null : Path.Combine(serverDir, "Logs");
    }

    private void OpenBrowser()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _serverUrl,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Не удалось открыть браузер: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RestartServer()
    {
        StopServer();
        StartServer();
    }

    private void StopServer()
    {
        try
        {
            if (_serverProcess != null && !_serverProcess.HasExited)
            {
                _serverProcess.Kill();
                _serverProcess.WaitForExit(1000);
            }
        }
        catch
        {
            // Ignore
        }
        finally
        {
            _serverProcess = null;
        }
    }

    private void ShowWindow()
    {
        this.Show();
        this.WindowState = WindowState.Normal;
        this.Activate();
    }

    private void ExitApplication()
    {
        _isExplicitExit = true;
        _healthCheckTimer?.Stop();
        _showListenerCts?.Cancel();
        StopServer();
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }
        System.Windows.Application.Current.Shutdown();
    }

    private static string? LoadSavedServerPath()
    {
        try
        {
            if (!File.Exists(ConfigPath)) return null;
            using var doc = JsonDocument.Parse(File.ReadAllText(ConfigPath));
            return doc.RootElement.TryGetProperty("serverExePath", out var prop) ? prop.GetString() : null;
        }
        catch
        {
            return null;
        }
    }

    private static void SaveServerPath(string path)
    {
        try
        {
            var dir = Path.GetDirectoryName(ConfigPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(ConfigPath, JsonSerializer.Serialize(new { serverExePath = path }));
        }
        catch
        {
            // Не критично: при следующем запуске просто снова предложим указать путь вручную.
        }
    }

    private void BtnLocateServer_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Укажите файл DentalLocal.Server.exe",
            Filter = "DentalLocal.Server.exe|DentalLocal.Server.exe|Исполняемые файлы (*.exe)|*.exe",
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) == true)
        {
            SaveServerPath(dialog.FileName);
            StartServer();
        }
    }

    private void BtnOpenBrowser_Click(object sender, RoutedEventArgs e) => OpenBrowser();

    private void BtnOpenLogs_Click(object sender, RoutedEventArgs e)
    {
        var logsDir = GetLogsDir();
        if (logsDir is null || !Directory.Exists(logsDir))
        {
            System.Windows.MessageBox.Show("Папка логов не найдена.", "DentalLocal", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Process.Start(new ProcessStartInfo { FileName = logsDir, UseShellExecute = true });
    }

    private void BtnRestart_Click(object sender, RoutedEventArgs e) => RestartServer();

    private void BtnMinimizeToTray_Click(object sender, RoutedEventArgs e)
    {
        this.Hide();
    }

    private void BtnStop_Click(object sender, RoutedEventArgs e) => ExitApplication();

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (!_isExplicitExit)
        {
            e.Cancel = true;
            this.Hide();
            _notifyIcon?.ShowBalloonTip(2000, "DentalLocal", "Лаунчер свернут в трей и продолжает работу.", System.Windows.Forms.ToolTipIcon.Info);
        }
    }

    private void Window_StateChanged(object sender, EventArgs e)
    {
        if (this.WindowState == WindowState.Minimized)
        {
            this.Hide();
        }
    }
}