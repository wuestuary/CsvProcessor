using System.Text;

namespace CsvProcessor;

public partial class MainPage : ContentPage
{
    private string? _currentFilePath;
    [System.Runtime.InteropServices.DllImport("user32.dll")]
static extern int GetSystemMetrics(int nIndex);
private const int SM_CXSCREEN = 0; // 屏幕宽度
private const int SM_CYSCREEN = 1; // 屏幕高度

    public MainPage()
{
    InitializeComponent();
    
    // 所有平台都启用拖放
    SetupDragDrop();
    
    LoadSettings();
}

private void SetupDragDrop()
{
    var dropGesture = new DropGestureRecognizer();
    
    dropGesture.DragOver += (s, e) =>
    {
        e.AcceptedOperation = DataPackageOperation.Copy;
        // 显示视觉反馈
    };
    
    dropGesture.Drop += async (s, e) =>
    {
        // 处理拖放
        if (e.Data.Properties.TryGetValue("FilePath", out var path))
        {
            await LoadFileOnly(path.ToString());
        }
    };

    // 添加到页面或特定区域
    this.GestureRecognizers.Add(dropGesture);
}


    private void LoadSettings()
    {
        EnableFilterSwitch.IsToggled = Preferences.Get("EnableFilterSwitch", true);
        ColumnFilterEntry.Text = Preferences.Get("ColumnFilterEntry", "1-6");
    }

    private void OnFilterParamsChanged(object sender, TextChangedEventArgs e)
    {
        Preferences.Set("ColumnFilterEntry", ColumnFilterEntry.Text);
    }

    private void OnFilterToggled(object sender, ToggledEventArgs e)
    {
        Preferences.Set("EnableFilterSwitch", EnableFilterSwitch.IsToggled);
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        // 自适应布局代码...
    }

    // ==================== 打开文件 ====================
    private async void OnOpenFileClicked(object sender, EventArgs e)
    {
        try
        {
            var result = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "选择 CSV 文件",
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.Android, new[] { "text/csv", ".csv" } },
                    { DevicePlatform.WinUI, new[] { ".csv" } },
                    { DevicePlatform.MacCatalyst, new[] { "public.comma-separated-values-text" } }
                })
            });

            if (result != null)
            {
                string targetPath = result.FullPath;
                
                // Android 处理 content:// URI
                if (DeviceInfo.Platform == DevicePlatform.Android && 
                    result.FullPath.StartsWith("content://", StringComparison.OrdinalIgnoreCase))
                {
                    using var sourceStream = await result.OpenReadAsync();
                    var tempPath = Path.Combine(FileSystem.CacheDirectory, $"picker_{Guid.NewGuid()}.csv");
                    using var destStream = File.Create(tempPath);
                    await sourceStream.CopyToAsync(destStream);
                    targetPath = tempPath;
                }

                await LoadFileOnly(targetPath);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("错误", $"打开文件失败: {ex.Message}", "确定");
        }
    }

    // ==================== 带进度条加载全部行 ====================
    // 更精确的进度计算
private async Task LoadFileOnly(string filePath)
{
    try
    {
        System.Diagnostics.Debug.WriteLine($"[LoadFileOnly] 开始加载文件: {filePath}");

        var fileInfo = new FileInfo(filePath);
        if (fileInfo.Length > 50 * 1024 * 1024)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await DisplayAlert("提示", "文件过大，无法在内存中处理 (限制 50MB)", "确定");
            });
            return;
        }

        _currentFilePath = filePath;
        
        // [优化] 使用流式读取，只读取前 5000 字符用于预览
        string previewContent;
        using (var stream = File.OpenRead(filePath))
        using (var reader = new StreamReader(stream, Encoding.UTF8))
        {
            var buffer = new char[5000];
            int charsRead = await reader.ReadAsync(buffer, 0, buffer.Length);
            previewContent = new string(buffer, 0, charsRead);

            if (charsRead >= 5000 && !reader.EndOfStream)
            {
                previewContent += "\n...";
            }
        }

        System.Diagnostics.Debug.WriteLine($"[LoadFileOnly] 预览读取完成，长度: {previewContent.Length}");

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            OriginalText.Text = previewContent;
            ProcessedText.Text = string.Empty; 
            StatusLabel.Text = $"已加载: {Path.GetFileName(filePath)}";
            StatusLabel.TextColor = Colors.Green;
        });
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"[LoadFileOnly] 发生异常: {ex.Message}");
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            StatusLabel.Text = $"错误: {ex.Message}";
            StatusLabel.TextColor = Colors.Red;
            await DisplayAlert("错误", $"加载文件时出错:\n{ex.Message}", "确定");
        });
    }
}


    // ==================== 转换并复制 ====================
    private async void OnConvertAndCopyClicked(object sender, EventArgs e)
{
    try
    {
        if (_currentFilePath == null || !File.Exists(_currentFilePath))
        {
            await DisplayAlert("提示", "请先打开 CSV 文件", "确定");
            return;
        }

        StatusLabel.Text = "正在转换...";
        StatusLabel.TextColor = Colors.Orange;

        // [修改] 使用 Task.Run 在后台线程执行流式转换，避免阻塞 UI
        string processed = await Task.Run(() => ProcessCsvAsync(_currentFilePath));

        // 显示结果
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            ProcessedText.Text = processed.Length > 5000 ? processed[..5000] + "\n..." : processed;
            StatusLabel.Text = $"已转换: {Path.GetFileName(_currentFilePath)} | 已复制到剪贴板";
            StatusLabel.TextColor = Colors.Green;
        });

        // 复制到剪贴板
        try
        {
            await Clipboard.SetTextAsync(processed);
        }
        catch (Exception clipEx)
        {
            System.Diagnostics.Debug.WriteLine($"[ConvertAndCopy] 剪贴板复制失败: {clipEx.Message}");
        }
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"[ConvertAndCopy] 发生异常: {ex.Message}");
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            StatusLabel.Text = $"错误: {ex.Message}";
            StatusLabel.TextColor = Colors.Red;
            await DisplayAlert("错误", $"转换失败:\n{ex.Message}", "确定");
        });
    }
}


    private void OnClearClicked(object sender, EventArgs e)
    {
        OriginalText.Text = string.Empty;
        ProcessedText.Text = string.Empty;
        _currentFilePath = null;
        StatusLabel.Text = "已清空";
        StatusLabel.TextColor = Colors.Gray;
    }

    private void OnExitClicked(object sender, EventArgs e)
    {
        Application.Current?.Quit();
    }

    // ==================== CSV 处理逻辑 ====================
    private string ProcessCsv(string csvContent)
    {
        bool useFilter = EnableFilterSwitch.IsToggled;
        var columnsToKeep = useFilter ? ParseColumnFilter(ColumnFilterEntry.Text) : null;
        
        var result = new StringBuilder();
        var currentField = new StringBuilder();
        var fields = new List<string>();
        bool inQuote = false;
        int i = 0;
        
        while (i < csvContent.Length)
        {
            char c = csvContent[i];
            
            if (c == '"')
            {
                currentField.Append('"');
                if (inQuote && i + 1 < csvContent.Length && csvContent[i + 1] == '"')
                {
                    currentField.Append('"');
                    i += 2;
                }
                else
                {
                    inQuote = !inQuote;
                    i++;
                }
            }
            else if (c == ',' && !inQuote)
            {
                fields.Add(currentField.ToString());
                currentField.Clear();
                i++;
            }
            else if ((c == '\r' || c == '\n') && !inQuote)
            {
                fields.Add(currentField.ToString());
                ProcessLine(fields, columnsToKeep, result);
                currentField.Clear();
                fields.Clear();
                if (c == '\r' && i + 1 < csvContent.Length && csvContent[i + 1] == '\n') i++;
                i++;
            }
            else
            {
                currentField.Append(c);
                i++;
            }
        }
        
        if (currentField.Length > 0 || fields.Count > 0)
        {
            fields.Add(currentField.ToString());
            ProcessLine(fields, columnsToKeep, result);
        }
        
        return result.ToString();
    }

    private void ProcessLine(List<string> fields, List<int>? columnsToKeep, StringBuilder result)
    {
        if (columnsToKeep != null && columnsToKeep.Count > 0)
        {
            var selected = columnsToKeep
                .Select(colIndex => colIndex >= 1 && colIndex <= fields.Count ? fields[colIndex - 1] : "")
                .ToList();
            fields = selected;
        }
        
        if (result.Length > 0) result.Append('\n');
        result.Append(string.Join('\t', fields));
    }

    private List<int> ParseColumnFilter(string filterText)
    {
        var result = new List<int>();
        if (string.IsNullOrWhiteSpace(filterText)) return result;
        
        foreach (var part in filterText.Split(','))
        {
            var trimmed = part.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;
            
            if (trimmed.Contains('-'))
            {
                var range = trimmed.Split('-');
                if (range.Length >= 2 && 
                    int.TryParse(range[0].Trim(), out int start) && 
                    int.TryParse(range[1].Trim(), out int end))
                {
                    for (int i = Math.Min(start, end); i <= Math.Max(start, end); i++)
                        if (i > 0) result.Add(i);
                }
            }
            else if (int.TryParse(trimmed, out int col) && col > 0)
            {
                result.Add(col);
            }
        }
        
        return result.Distinct().OrderBy(x => x).ToList();
    }

    // 拖放支持
    public Task HandleFileDrop(string filePath)
    {
        return LoadFileOnly(filePath);
    }
    // ======================== 新增：流式转换逻辑 ========================
private async Task<string> ProcessCsvAsync(string filePath)
{
    bool useFilter = EnableFilterSwitch.IsToggled;
    var columnsToKeep = useFilter ? ParseColumnFilter(ColumnFilterEntry.Text) : null;

    var result = new StringBuilder();
    var currentField = new StringBuilder();
    var fields = new List<string>();
    bool inQuote = false;
    //
    // 创建一个大小为1的缓冲区，用于适配 .NET 9 的 ReadAsync
    var buffer = new char[1];

    using (var stream = File.OpenRead(filePath))
    using (var reader = new StreamReader(stream, Encoding.UTF8))
    {
        int charsRead;
        // 循环读取，直到读取不到字符 (charsRead == 0)
        while ((charsRead = await reader.ReadAsync(buffer, 0, 1)) > 0)
        {
            char c = buffer[0];

            if (c == '"')
            {
                currentField.Append('"');

                if (inQuote && reader.Peek() == '"')
                {
                    currentField.Append('"');
                    // 修正：传入 buffer 参数
                    await reader.ReadAsync(buffer, 0, 1); 
                }
                else
                {
                    inQuote = !inQuote;
                }
            }
            else if (c == ',' && !inQuote)
            {
                fields.Add(currentField.ToString());
                currentField.Clear();
            }
            else if ((c == '\r' || c == '\n') && !inQuote)
            {
                fields.Add(currentField.ToString());
                ProcessLine(fields, columnsToKeep, result);
                currentField.Clear();
                fields.Clear();

                if (c == '\r' && reader.Peek() == '\n')
                {
                    // 修正：传入 buffer 参数
                    await reader.ReadAsync(buffer, 0, 1);
                }
            }
            else
            {
                currentField.Append(c);
            }
        }
    }

    if (currentField.Length > 0 || fields.Count > 0)
    {
        fields.Add(currentField.ToString());
        ProcessLine(fields, columnsToKeep, result);
    }

    return result.ToString();
}

// ===============================================================

}