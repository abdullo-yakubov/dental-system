using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
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
    private readonly string _serverUrl = "http://localhost:5000";

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

            var candidates = new[]
            {
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

            if (string.IsNullOrEmpty(serverExePath))
            {
                StatusText.Text = "Ошибка запуска";
                StatusDot.Fill = System.Windows.Media.Brushes.Red;
                DetailsText.Text = "Не удалось найти файл DentalLocal.Server.exe. Проверьте правильность сборки проекта.";
                return;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = serverExePath,
                WorkingDirectory = Path.GetDirectoryName(serverExePath),
                CreateNoWindow = true,
                UseShellExecute = false
            };

            _serverProcess = Process.Start(startInfo);
            DetailsText.Text = $"Процесс сервера запущен (PID: {_serverProcess?.Id}). Ожидание инициализации...";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Ошибка запуска";
            StatusDot.Fill = System.Windows.Media.Brushes.Red;
            DetailsText.Text = $"Произошла ошибка при запуске процесса сервера: {ex.Message}";
        }
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
        try
        {
            // Check process
            if (_serverProcess == null || _serverProcess.HasExited)
            {
                StatusText.Text = "Сервер остановлен";
                StatusDot.Fill = System.Windows.Media.Brushes.Red;
                DetailsText.Text = "Процесс сервера неожиданно завершился. Нажмите «Перезапустить» для повторной попытки.";
                BtnOpenBrowser.IsEnabled = false;
                return;
            }

            // Call health check endpoint
            var response = await _httpClient.GetAsync($"{_serverUrl}/api/v1/system/health");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                if (doc.RootElement.TryGetProperty("status", out var statusProp) && statusProp.GetString() == "ok")
                {
                    StatusText.Text = "Сервер работает стабильно";
                    StatusDot.Fill = System.Windows.Media.Brushes.Green;
                    DetailsText.Text = $"Dental IS запущен и готов к работе.\nАдрес: {_serverUrl}\nПроцесс PID: {_serverProcess.Id}";
                    BtnOpenBrowser.IsEnabled = true;
                    return;
                }
            }

            StatusText.Text = "Запуск веб-сервисов...";
            StatusDot.Fill = System.Windows.Media.Brushes.Orange;
            BtnOpenBrowser.IsEnabled = false;
        }
        catch
        {
            StatusText.Text = "Ожидание веб-сервисов...";
            StatusDot.Fill = System.Windows.Media.Brushes.Orange;
            BtnOpenBrowser.IsEnabled = false;
        }
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
        StopServer();
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }
        System.Windows.Application.Current.Shutdown();
    }

    private void BtnOpenBrowser_Click(object sender, RoutedEventArgs e) => OpenBrowser();

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