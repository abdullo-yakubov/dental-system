using System.Configuration;
using System.Data;
using System.Threading;
using System.Windows;

namespace DentalLocal.Launcher;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private const string SingleInstanceMutexName = "Global\\DentalLocal.Launcher.SingleInstance";
    private const string ShowExistingWindowEventName = "Global\\DentalLocal.Launcher.ShowWindow";

    private Mutex? _singleInstanceMutex;
    private EventWaitHandle? _showWindowEvent;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Второй запуск лаунчера (например, повторный клик по ярлыку) не должен поднимать второй
        // процесс сервера поверх уже работающего — вместо этого сигналим первому экземпляру
        // развернуть своё окно и сразу завершаемся, не трогая StartServer().
        _singleInstanceMutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out var isFirstInstance);
        _showWindowEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowExistingWindowEventName);

        if (!isFirstInstance)
        {
            _showWindowEvent.Set();
            Shutdown();
            return;
        }

        var mainWindow = new MainWindow();
        MainWindow = mainWindow;
        mainWindow.Show();
        mainWindow.ListenForShowRequests(_showWindowEvent);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _singleInstanceMutex?.ReleaseMutex();
        _singleInstanceMutex?.Dispose();
        _showWindowEvent?.Dispose();
        base.OnExit(e);
    }
}
