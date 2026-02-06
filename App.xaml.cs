namespace CsvProcessor;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new MainPage());
        
        // 强制设置窗口标题
        window.Title = "CsvProcessor";
        
#if WINDOWS
        // Windows 额外设置
        window.Width = 1200;
        window.Height = 800;
#endif
        
        return window;
    }
}