using Microsoft.UI.Xaml;
using System.Runtime.InteropServices;
using System.Text;

namespace CsvProcessor.WinUI;

public partial class App : MauiWinUIApplication
{
    // Win32 API 声明
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern void DragAcceptFiles(IntPtr hwnd, bool fAccept);
    
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern uint DragQueryFile(IntPtr hDrop, uint iFile, StringBuilder lpszFile, uint cch);
    
    [DllImport("comctl32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowSubclass(IntPtr hWnd, SubclassProc pfnSubclass, uint uIdSubclass, IntPtr dwRefData);
    
    [DllImport("comctl32.dll")]
    private static extern IntPtr DefSubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);
    
    // 必须有此委托定义，且必须是静态字段防止 GC
    private delegate IntPtr SubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, IntPtr uIdSubclass, IntPtr dwRefData);
    private static SubclassProc? _subclassDelegate;
    
    private const uint WM_DROPFILES = 0x0233;
    private IntPtr _hwnd;
    private bool _subclassed = false;

    public App() => this.InitializeComponent();
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);
        
        // 文件关联
        Task.Delay(500).ContinueWith(_ => MainThread.BeginInvokeOnMainThread(HandleFileActivation));
        
        // 延迟 5 秒确保窗口完全稳定
        Task.Delay(5000).ContinueWith(_ => MainThread.BeginInvokeOnMainThread(TrySetupWin32Drop));
    }

    private void TrySetupWin32Drop()
    {
        try
        {
            if (_subclassed) return;
            
            var win = Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault()
                ?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
            
            if (win == null) return;

            // 获取 HWND
            _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(win);
            
            // 启用拖放
            DragAcceptFiles(_hwnd, true);
            
            // 子类化（必须用静态字段保存委托）
            _subclassDelegate = new SubclassProc(WindowProc);
            SetWindowSubclass(_hwnd, _subclassDelegate, 0, IntPtr.Zero);
            _subclassed = true;
            
            System.Diagnostics.Debug.WriteLine("<<< Win32 子类化拖放已启用 >>>");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"<<< Win32 设置失败: {ex.Message} >>>");
        }
    }

    private IntPtr WindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, IntPtr uIdSubclass, IntPtr dwRefData)
    {
        if (uMsg == WM_DROPFILES)
        {
            System.Diagnostics.Debug.WriteLine("<<< WM_DROPFILES 收到! >>>");
            
            try
            {
                // 获取文件数量
                uint count = DragQueryFile(wParam, 0xFFFFFFFF, null!, 0);
                System.Diagnostics.Debug.WriteLine($"<<< 文件数: {count} >>>");
                
                // 获取第一个文件
                StringBuilder sb = new StringBuilder(1024);
                if (DragQueryFile(wParam, 0, sb, 1024) > 0)
                {
                    string path = sb.ToString();
                    System.Diagnostics.Debug.WriteLine($"<<< 路径: {path} >>>");
                    
                    if (path.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                    {
                        // 必须在主线程处理
                        MainThread.BeginInvokeOnMainThread(async () =>
                        {
                            try
                            {
                                var page = Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault()?.Page as MainPage;
                                if (page != null)
                                {
                                    await page.HandleFileDrop(path);
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"<<< 处理错误: {ex.Message} >>>");
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"<<< Win32 错误: {ex.Message} >>>");
            }
            
            return IntPtr.Zero; // 处理完毕
        }
        
        // 默认处理
        return DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }

    private void HandleFileActivation()
    {
        try
        {
            var args = Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().GetActivatedEventArgs();
            if (args.Kind == Microsoft.Windows.AppLifecycle.ExtendedActivationKind.File)
            {
                var fileArgs = args.Data as Windows.ApplicationModel.Activation.FileActivatedEventArgs;
                var file = fileArgs?.Files.FirstOrDefault() as Windows.Storage.StorageFile;
                
                if (file?.Name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) == true)
                {
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await Task.Delay(1000);
                        var page = Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault()?.Page as MainPage;
                        if (page != null) await page.HandleFileDrop(file.Path);
                    });
                }
            }
        }
        catch { }
    }
}