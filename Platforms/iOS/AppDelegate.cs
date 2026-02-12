using Foundation;
using UIKit;
using Microsoft.Maui;

namespace CsvProcessor;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    // 处理从其他应用打开的文件（iOS 13+ 使用 SceneDelegate，旧版使用此方法）
    public override bool OpenUrl(UIApplication application, NSUrl url, string sourceApplication, NSObject annotation)
    {
        HandleIncomingFile(url);
        return true;
    }

    // 处理传入的文件
    private void HandleIncomingFile(NSUrl url)
    {
        if (url == null) return;

        // 获取文件路径
        string filePath = url.Path;
        
        // 如果文件在 Inbox 目录（从其他应用分享过来的）
        if (filePath.Contains("Inbox"))
        {
            // 复制到应用目录以便长期访问
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string fileName = Path.GetFileName(filePath);
            string destPath = Path.Combine(documentsPath, fileName);
            
            if (File.Exists(filePath))
            {
                File.Copy(filePath, destPath, true);
                
                // 通知 MAUI 页面加载此文件
                // 使用 WeakReferenceMessenger 或 Event 通知 MainPage
                WeakReferenceMessenger.Default.Send(new FileOpenedMessage(destPath));
            }
        }
    }
}