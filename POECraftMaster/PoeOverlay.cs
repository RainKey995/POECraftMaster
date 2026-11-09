using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using ImGuiNET;
using ClickableTransparentOverlay;
using POECraftMaster.Helper;

namespace POECraftMaster;

public class PoeOverlay : Overlay
{
    public const string TargetProcessName = "PathOfExile_x64";

    private bool checkBoxValue = false;
    private int loveMeter = 0;
    private string input1 = string.Empty;
    private string input2 = string.Empty;

    private Process? _targetProcess;
    private Process _selfProcess = null!;

    // Direct3D 11 shader resource view; owned and released by Overlay.
    private IntPtr _itemTexture;
    private Vector2 _itemTextureSize;
    private volatile bool _overlayVisible = true;
    private bool _backdropVisible;
    private bool _blurAvailable;
    private const float FadeDurationSeconds = 0.25f;
    private float _fadeProgress;
    private long _lastFrameTimestamp = Stopwatch.GetTimestamp();

    public void ToggleVisibility() => _overlayVisible = !_overlayVisible;

    public bool IsRender
    {
        get
        {
            if (_targetProcess == null)
                return false;

            IntPtr handle = ScreenHelper.GetForegroundWindow();

            ScreenHelper.GetWindowThreadProcessId(
                handle,
                out var processId);

            if (processId != _targetProcess.Id &&
                processId != _selfProcess.Id)
            {
                return false;
            }

            return true;
        }
    }

    protected override Task PostInitialized()
    {
        ScreenHelper.HideFromTaskbar(window.Handle);
        _selfProcess = Process.GetCurrentProcess();
        _targetProcess = Process.GetProcessesByName(TargetProcessName).FirstOrDefault();

        FPSLimit = 30;
        ConfigureChineseFont();

        var imagePath = Path.Combine(AppContext.BaseDirectory, "Assets", "Sprites", "Grid.png");
        AddOrGetImagePointer(imagePath, false, out _itemTexture, out var width, out var height);
        _itemTextureSize = new Vector2(width, height);

        return Task.CompletedTask;
    }

    private void ConfigureChineseFont()
    {
        var fontsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
        string[] fontFiles = ["msyh.ttc", "simhei.ttf", "simsun.ttc"];

        foreach (var fontFile in fontFiles)
        {
            var fontPath = Path.Combine(fontsDirectory, fontFile);
            // Queue the font update so Overlay rebuilds its Direct3D font texture.
            // ChineseFull also includes Latin characters and Chinese punctuation.
            if (ReplaceFont(fontPath, 18, FontGlyphRangeType.ChineseFull))
                return;
        }

        throw new FileNotFoundException(
            "无法加载中文字体，请在 Windows 中安装微软雅黑、黑体或宋体。");
    }

    protected override void Render()
    {
        long now = Stopwatch.GetTimestamp();
        float elapsed = (float)Stopwatch.GetElapsedTime(_lastFrameTimestamp, now).TotalSeconds;
        _lastFrameTimestamp = now;

        // Never leave a fading overlay on the desktop or over a minimized game.
        bool canRender = IsRender && Resize();
        if (!canRender)
            _fadeProgress = 0f;
        else
            _fadeProgress = Math.Clamp(_fadeProgress
                + (_overlayVisible ? 1f : -1f) * elapsed / FadeDurationSeconds, 0f, 1f);

        // Smoothstep eases both endpoints; reversing direction keeps the current opacity.
        float opacity = _fadeProgress * _fadeProgress * (3f - 2f * _fadeProgress);
        bool visible = canRender && _fadeProgress > 0f;
        if (_backdropVisible != visible)
        {
            // Run after Overlay's transparency initialization, on the render thread.
            _blurAvailable = BackdropHelper.SetBlur(window.Handle, visible);
            _backdropVisible = visible;
        }
        if (!visible)
            return;

        ImGui.SetNextWindowPos(Vector2.Zero, ImGuiCond.Always);
        ImGui.SetNextWindowSize(new Vector2(Size.Width, Size.Height), ImGuiCond.Always);
        ImGui.SetNextWindowBgAlpha(_blurAvailable ? 0.22f : 0.65f);
        ImGui.PushStyleVar(ImGuiStyleVar.Alpha, ImGui.GetStyle().Alpha * opacity);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
        ImGui.Begin("Main Window", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar
            | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse
            | (_overlayVisible ? ImGuiWindowFlags.None : ImGuiWindowFlags.NoInputs));

        ImGui.Text("改造+增幅");
        ImGui.Image(_itemTexture, _itemTextureSize, Vector2.Zero, Vector2.One,
            new Vector4(1f, 1f, 1f, opacity), Vector4.Zero);
        ImGui.SliderInt("hot much i appreciate this video", ref loveMeter, 1, 10);
        ImGui.Checkbox("unlimited arm", ref checkBoxValue);
        
        ImGui.BeginChild("first child window", new Vector2(300, 200));
        ImGui.Text("child window content");
        ImGui.InputText("first input", ref input1, 16);
        ImGui.EndChild();

        ImGui.SameLine();

        ImGui.BeginChild("second child window", new Vector2(300, 200));
        ImGui.Text("child window content");
        ImGui.InputText("second input", ref input2, 16);
        ImGui.EndChild();

        ImGui.End();
        ImGui.PopStyleVar(3);
    }

    public bool Resize()
    {
        if (_targetProcess == null || _targetProcess.HasExited)
            return false;

        // Refresh the cached handle in case the game recreates its window.
        _targetProcess.Refresh();
        var handle = _targetProcess.MainWindowHandle;
        if (handle == IntPtr.Zero || ScreenHelper.IsIconic(handle))
            return false;

        var rect = new ScreenHelper.U32Rect();
        var origin = new ScreenHelper.U32Point();
        if (!ScreenHelper.GetClientRect(handle, ref rect)
            || !ScreenHelper.ClientToScreen(handle, ref origin)
            || rect.Width <= 0 || rect.Height <= 0)
            return false;

        // Align to the game content, excluding its title bar and window borders.
        Position = new Point(origin.x, origin.y);
        Size = new Size(rect.Width, rect.Height);
        return true;
    }

}
