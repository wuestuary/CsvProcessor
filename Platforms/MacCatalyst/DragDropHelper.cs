using UIKit;
using Foundation;
using UniformTypeIdentifiers;

namespace CsvProcessor.Platforms.MacCatalyst;

public class DragDropHelper : UIView, IUIDropInteractionDelegate
{
    public event EventHandler<string>? FileDropped;

    public DragDropHelper()
    {
        var dropInteraction = new UIDropInteraction(this);
        AddInteraction(dropInteraction);
    }

    public bool CanHandleDropSession(IUIDropInteraction interaction, UIDropSession session)
    {
        return session.HasItemsConformingTo(new[] { UTTypes.CommaSeparatedText.Identifier, "public.file-url" });
    }

    public void PerformDrop(IUIDropInteraction interaction, UIDropSession session)
    {
        foreach (var item in session.Items)
        {
            item.ItemProvider.LoadItem(typeof(NSUrl), null, (data, error) =>
            {
                if (data is NSUrl url)
                {
                    var path = url.Path;
                    if (path?.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        MainThread.BeginInvokeOnMainThread(() => FileDropped?.Invoke(this, path));
                    }
                }
            });
        }
    }
// 强制修改
    public UIDropProposal GetDropProposal(IUIDropInteraction interaction, UIDropSession session)
    {
        return new UIDropProposal(UIDropOperation.Copy);
    }
}