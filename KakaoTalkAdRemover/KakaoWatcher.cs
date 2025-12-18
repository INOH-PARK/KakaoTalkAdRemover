using System.Diagnostics;
using System.Runtime.Versioning;

namespace KakaoTalkAdRemover;

[SupportedOSPlatform("windows")]
internal static class KakaoWatcher
{
    private static nint _cachedMainHandle;
    private static nint _cachedAdHandle;
    private static nint _cachedListHandle;

    public static void Run(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                if (_cachedMainHandle == 0 || !NativeMethods.IsWindow(_cachedMainHandle))
                {
                    using var currentProcess = Process.GetCurrentProcess();
                    var pros = Process.GetProcessesByName("KakaoTalk");
                    var targetProc = pros.FirstOrDefault(p => p.MainWindowTitle == "카카오톡");
                    
                    _cachedMainHandle = targetProc?.MainWindowHandle ?? 0;

                    if (_cachedMainHandle != 0)
                    {
                        _cachedAdHandle = 0;
                        _cachedListHandle = 0;
                    }

                    foreach (var p in pros) if (p != targetProc) p.Dispose();
                    targetProc?.Dispose();
                }

                if (_cachedMainHandle != 0)
                {
                    ApplyMainFix();
                    HideStandaloneAds();
                }
            }
            catch
            {
                // ignored
            }

            if (token.WaitHandle.WaitOne(200)) break;
        }
    }

    private static void ApplyMainFix()
    {
        if (_cachedAdHandle == 0 || !NativeMethods.IsWindow(_cachedAdHandle) ||
            _cachedListHandle == 0 || !NativeMethods.IsWindow(_cachedListHandle))
        {
            nint childHandle = 0;
            long maxArea = 0;

            while ((childHandle = NativeMethods.FindWindowEx(_cachedMainHandle, childHandle, "EVA_ChildWindow", null)) != 0)
            {
                NativeMethods.GetWindowRect(childHandle, out var rect);
                long area = (long)rect.Width * rect.Height;

                if (rect.Height is >= 90 and <= 92)
                {
                    _cachedAdHandle = childHandle;
                }
                else if (area > maxArea)
                {
                    maxArea = area;
                    _cachedListHandle = childHandle;
                }
            }
        }

        if (_cachedAdHandle == 0 || _cachedListHandle == 0) return;

        if (NativeMethods.IsWindowVisible(_cachedAdHandle))
        {
            NativeMethods.ShowWindow(_cachedAdHandle, 0);
        }

        NativeMethods.GetWindowRect(_cachedListHandle, out var listRect);
        NativeMethods.GetWindowRect(_cachedMainHandle, out var mainRect);

        int gap = mainRect.Bottom - listRect.Bottom;
        if (gap <= 20) return;

        int listTopOffset = listRect.Top - mainRect.Top;
        int newHeight = mainRect.Height - listTopOffset;

        NativeMethods.SetWindowPos(
            _cachedListHandle, 
            0, 0, 0, 
            listRect.Width, newHeight,
            NativeMethods.SwpNomove | NativeMethods.SwpNozorder | NativeMethods.SwpNoactivate
        );
    }

    private static void HideStandaloneAds()
    {
        nint currentHandle = 0;
        Span<char> buf = stackalloc char[256]; 

        while ((currentHandle = NativeMethods.FindWindowEx(0, currentHandle, "EVA_Window_Dblclk", null)) != 0)
        {
            if (!NativeMethods.IsWindowVisible(currentHandle)) continue;
            if (NativeMethods.GetWindowText(currentHandle, buf, 256) > 0) continue;

            NativeMethods.GetWindowRect(currentHandle, out var rect);
            var isAd = rect switch
            {
                { Height: >= 90 and <= 92 } => true,
                { Width: 156, Height: 44 } => true,
                _ => false
            };

            if (isAd)
            {
                NativeMethods.ShowWindow(currentHandle, 0);
            }
        }
    }
}
