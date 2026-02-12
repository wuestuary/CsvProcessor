using Foundation;
using UIKit;
using Microsoft.Maui;
using Microsoft.Maui.Controls; // 添加这个命名空间

namespace CsvProcessor;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    // 正确的 OpenUrl 签名
    [Export("application:openURL:options:")]
    public override bool OpenUrl(UIApplication app, NSUrl url, NSDictionary options)
    {
        if (url?.Path == null) return false;

        HandleIncomingFile(url.Path);
        return true;
    }

    // 处理传入的文件
    private void HandleIncomingFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return;

        // iOS：复制到应用目录（沙盒要求）
        string destPath = CopyToAppDirectory(filePath);
        if (destPath == null) return;

        // 使用 MainThread 确保在主线程执行
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            // 等待页面加载完成
            await Task.Delay(500);
            
            // ✅ 修复：使用 Microsoft.Maui.Controls.Application.Current.Windows
            // 或者使用 IPlatformApplication.Current.Application
            if (Microsoft.Maui.Controls.Application.Current is Application app)
            {
                var window = app.Windows.FirstOrDefault();
                if (window?.Page is MainPage mainPage)
                {
                    await mainPage.HandleExternalFile(destPath);
                }
            }
        });
    }

    private string CopyToAppDirectory(string sourcePath)
    {
        try
        {
            string documentsPath = FileSystem.AppDataDirectory;
            string fileName = Path.GetFileName(sourcePath);
            string destPath = Path.Combine(documentsPath, fileName);
            
            File.Copy(sourcePath, destPath, true);
            return destPath;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AppDelegate] 复制文件失败: {ex.Message}");
            return null;
        }
    }
}