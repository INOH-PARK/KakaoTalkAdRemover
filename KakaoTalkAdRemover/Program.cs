using Microsoft.Win32;
using System.Reflection;
using System.Runtime.Versioning;

namespace KakaoTalkAdRemover;

[SupportedOSPlatform("windows")]
internal static class Program
{
    private const string AppName = "KakaoTalk Ad Remover";
    
    [STAThread]
    private static void Main()
    {
        if (!OperatingSystem.IsWindows()) return;

        ApplicationConfiguration.Initialize();

        using var icon = LoadEmbeddedIcon("KakaoTalkAdRemover.app.ico") ?? SystemIcons.Shield;
        
        using NotifyIcon trayIcon = new();
        trayIcon.Icon = icon;
        trayIcon.Text = "KakaoTalk Ad Remover";
        trayIcon.Visible = true;

        trayIcon.ShowBalloonTip(3000, "KakaoTalk Ad Remover", "백그라운드에서 실행 중입니다.", ToolTipIcon.Info);

        ContextMenuStrip menu = new();
        var startupItem = new ToolStripMenuItem("윈도우 시작 시 자동 실행");
        startupItem.Checked = IsStartupEnabled();
        startupItem.Click += (_, _) =>
        {
            SetStartup(!startupItem.Checked);
            startupItem.Checked = IsStartupEnabled();
        };
        menu.Items.Add(startupItem);

        menu.Items.Add(new ToolStripSeparator());

        menu.Items.Add("종료", null, (_, _) => Application.Exit());
        trayIcon.ContextMenuStrip = menu;

        CancellationTokenSource cts = new();
        Task.Run(() => KakaoWatcher.Run(cts.Token), cts.Token);

        Application.Run();

        cts.Cancel();
    }
    
    private static bool IsStartupEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);
        return key?.GetValue(AppName) != null;
    }

    private static void SetStartup(bool enable)
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
        if (enable)
        {
            key?.SetValue(AppName, Application.ExecutablePath);
        }
        else
        {
            key?.DeleteValue(AppName, false);
        }
    }

    private static Icon? LoadEmbeddedIcon(string resourceName)
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(resourceName);
            return stream != null ? new Icon(stream) : null;
        }
        catch
        {
            return null;
        }
    }
}
