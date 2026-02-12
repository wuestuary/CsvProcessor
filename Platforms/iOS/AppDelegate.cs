using Foundation;
using UIKit;
using Microsoft.Maui;

namespace CsvProcessor;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    // ✅ 正确的 MAUI iOS 方法签名
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

        // 通知主页面加载文件
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            // 等待页面加载完成
            await Task.Delay(500);
            
            if (Application.Current?.Windows.FirstOrDefault()?.Page is MainPage mainPage)
            {
                await mainPage.HandleExternalFile(destPath);
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