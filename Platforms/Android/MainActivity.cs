using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using AndroidUri = Android.Net.Uri;
using SystemIO = System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Controls; // 新增：用于 MessagingCenter

namespace CsvProcessor;

// 主 Activity 特性
[Activity(
    Name = "com.companyname.csvprocessor.MainActivity",
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    Exported = true,
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation |
                          ConfigChanges.UiMode | ConfigChanges.ScreenLayout |
                          ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]

// 扩展 IntentFilter 配置
// 1. 通用 MIME 类型支持
[IntentFilter(
    new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
    DataMimeType = "text/csv")]
[IntentFilter(
    new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
    DataMimeType = "application/csv")]

// 2. 文件路径支持 (当文件管理器使用 file:// 协议时)
[IntentFilter(
    new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
    DataScheme = "file",
    DataPathPattern = ".*\\.csv",
    DataMimeType = "*/*")]

// 3. Content 协议支持 (当文件管理器使用 content:// 协议且无明确 MIME 时)
[IntentFilter(
    new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
    DataScheme = "content",
    DataPathPattern = ".*\\.csv",
    DataMimeType = "*/*")]

// 4. 分享支持
[IntentFilter(
    new[] { Intent.ActionSend },
    Categories = new[] { Intent.CategoryDefault },
    DataMimeType = "text/csv")]

public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        HandleIntent(Intent);
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        Intent = intent;
        HandleIntent(intent);
    }

    private void HandleIntent(Intent? intent)
    {
        if (intent == null) return;

        System.Diagnostics.Debug.WriteLine($"<<< Intent Action: {intent.Action} >>>");
        System.Diagnostics.Debug.WriteLine($"<<< Intent Data: {intent.Data} >>>");

        if (intent.Action == Intent.ActionView || intent.Action == Intent.ActionSend)
        {
            var uri = intent.Data;
            if (uri != null)
            {
                ProcessFileUri(uri);
            }
            // 处理 ActionSend 时，ClipData 可能包含 Uri
            else if (intent.ClipData != null && intent.ClipData.ItemCount > 0)
            {
                var clipItem = intent.ClipData.GetItemAt(0);
                if (clipItem?.Uri != null)
                {
                    ProcessFileUri(clipItem.Uri);
                }
            }
        }
    }

    private void ProcessFileUri(AndroidUri uri)
    {
        Task.Run(() =>
        {
            try
            {
                var fileName = GetFileName(uri) ?? $"temp_{Guid.NewGuid()}.csv";
                var tempPath = SystemIO.Path.Combine(CacheDir!.AbsolutePath, fileName);

                using var input = ContentResolver!.OpenInputStream(uri);
                using var output = new SystemIO.FileStream(tempPath, SystemIO.FileMode.Create);
                input?.CopyTo(output);

                System.Diagnostics.Debug.WriteLine($"<<< 临时文件: {tempPath} >>>");

                if (SystemIO.File.Exists(tempPath))
                {
                    // 在主线程发送消息，通知 MainPage 处理文件
                    RunOnUiThread(() =>
                    {
                        // 发送消息：发送者是 this，消息主题是 "FileOpened"，内容是 tempPath
                        MessagingCenter.Send(this, "FileOpened", tempPath);
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"<<< 处理异常: {ex.Message} >>>");
            }
        });
    }

    private string? GetFileName(AndroidUri uri)
    {
        try
        {
            using var cursor = ContentResolver?.Query(uri, null, null, null, null);
            if (cursor?.MoveToFirst() == true)
            {
                int idx = cursor.GetColumnIndex(OpenableColumns.DisplayName);
                if (idx >= 0) return cursor.GetString(idx);
            }
        }
        catch { }
        return uri.LastPathSegment;
    }
}
