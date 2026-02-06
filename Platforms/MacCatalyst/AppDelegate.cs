using Foundation;
using UIKit;
using UniformTypeIdentifiers;
using System.Linq;

namespace CsvProcessor;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    public override bool OpenUrl(UIApplication app, NSUrl url, NSDictionary options)
    {
        if (url.Scheme == "file" && url.Path?.EndsWith(".csv") == true)
        {
            LoadCsvFile(url.Path);
            return true;
        }
        return base.OpenUrl(app, url, options);
    }

    public override void OnActivated(UIApplication application)
    {
        base.OnActivated(application);
        SetupDragDropOnce();
    }

    private bool _dropSetup = false;
    
    private void SetupDragDropOnce()
    {
        if (_dropSetup) return;
        
        var rootView = Window?.RootViewController?.View;
        if (rootView == null)
        {
            Task.Run(async () =>
            {
                await Task.Delay(1000);
                MainThread.BeginInvokeOnMainThread(SetupDragDropOnce);
            });
            return;
        }

        _dropSetup = true;
        
        var interaction = new UIDropInteraction(new DropHelper(this));
        rootView.AddInteraction(interaction);
        
        Console.WriteLine($"<<< Mac: 拖放已设置 >>>");
    }

    private void LoadCsvFile(string? path)
    {
        if (string.IsNullOrEmpty(path)) return;
        
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            // 通过 MAUI Application 获取 MainPage，而不是转换 ViewController
            var page = Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page is MainPage mainPage)
            {
                await mainPage.HandleFileDrop(path);
            }
        });
    }

    private class DropHelper : NSObject, IUIDropInteractionDelegate
    {
        private readonly AppDelegate _app;
        
        public DropHelper(AppDelegate app) => _app = app;

        [Export("dropInteraction:canHandleSession:")]
        public bool CanHandleSession(UIDropInteraction interaction, IUIDropSession session)
        {
            // 使用 Items.Any 替代 HasItemsConformingTo
            return session.Items.Any(item => 
                item.ItemProvider.HasItemConformingTo(UTTypes.FileUrl.Identifier));  // 使用 UTTypes（复数）和 Identifier
        }

        [Export("dropInteraction:sessionDidUpdate:")]
        public UIDropProposal SessionDidUpdate(UIDropInteraction interaction, IUIDropSession session)
        {
            return new UIDropProposal(UIDropOperation.Copy);
        }

        [Export("dropInteraction:performDrop:")]
        public async void PerformDrop(UIDropInteraction interaction, IUIDropSession session)
        {
            foreach (var item in session.Items)
            {
                try
                {
                    // 使用 UTTypes.FileUrl.Identifier（复数形式）
                    var url = await item.ItemProvider.LoadItemAsync(UTTypes.FileUrl.Identifier, null) as NSUrl;
                    
                    if (url?.Path?.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        _app.LoadCsvFile(url.Path);
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"<<< Mac 错误: {ex.Message} >>>");
                }
            }
        }
    }
}