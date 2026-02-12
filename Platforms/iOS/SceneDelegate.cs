using Foundation;
using UIKit;
using Microsoft.Maui;

namespace CsvProcessor;

[Register("SceneDelegate")]
public class SceneDelegate : MauiUISceneDelegate
{
    // 处理从外部打开的文件（iOS 13+）
    public override void OpenUrlContexts(UIScene scene, NSSet<UIOpenUrlContext> urlContexts)
    {
        foreach (UIOpenUrlContext urlContext in urlContexts)
        {
            NSUrl url = urlContext.Url;
            if (url.Scheme == "file")
            {
                HandleIncomingFile(url);
            }
        }
    }

    private void HandleIncomingFile(NSUrl url)
    {
        string filePath = url.Path;
        if (string.IsNullOrEmpty(filePath)) return;

        // 复制到应用目录
        string documentsPath = FileSystem.AppDataDirectory;
        string fileName = Path.GetFileName(filePath);
        string destPath = Path.Combine(documentsPath, fileName);

        try
        {
            if (File.Exists(filePath))
            {
                File.Copy(filePath, destPath, true);
                
                // 发送消息给 MainPage
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    if (Application.Current?.MainPage is MainPage mainPage)
                    {
                        await mainPage.HandleFileDrop(destPath);
                    }
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SceneDelegate] 处理文件失败: {ex.Message}");
        }
    }
}