using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using ImGuiNET;
using ClickableTransparentOverlay;
using POECraftMaster.Helper;
using POECraftMaster.Crafting;

namespace POECraftMaster;

public class PoeOverlay : Overlay
{
    public const string TargetProcessName = "PathOfExile_x64";

    private readonly CraftPanel _craftPanel = new();
    private readonly CraftRunner _craftRunner = new();
    private volatile bool _settingsOpen;
    private bool _nativeSuppressed;
    private long _nextProcessSearch;

    private Process? _targetProcess;
    private IntPtr _targetWindowHandle;
    private int _targetProcessId;
    private bool _overlayHasFocus;

    private volatile bool _overlayVisible = true;
    private bool _backdropVisible;
    private bool _blurAvailable;
    private const float FadeDurationSeconds = 0.25f;
    private float _fadeProgress;
    private long _lastFrameTimestamp = Stopwatch.GetTimestamp();

    public void ToggleVisibility()
    {
        if (_craftRunner.Progress.Running)
        {
            _craftRunner.Stop();
            _overlayVisible = true;
        }
        else _overlayVisible = !_overlayVisible;
    }

    public CraftProgress CraftProgress => _craftRunner.Progress;
    public void StopCrafting() => _craftRunner.Stop();
    public void SetSettingsOpen(bool open) => _settingsOpen = open;

    public bool IsRender
    {
        get
        {
            _overlayHasFocus = false;
            if (_targetWindowHandle == IntPtr.Zero)
                return false;

            IntPtr handle = ScreenHelper.GetForegroundWindow();
            if (handle == _targetWindowHandle) return true;

            ScreenHelper.GetWindowThreadProcessId(
                handle,
                out var processId);

            _overlayHasFocus = processId == Environment.ProcessId;
            return _overlayHasFocus || processId == _targetProcessId;
        }
    }

    protected override Task PostInitialized()
    {
        ScreenHelper.HideFromTaskbar(window.Handle);
        RefreshTargetProcess();

        FPSLimit = 60;
        ConfigureChineseFont();

        _craftPanel.Initialize(relativePath =>
        {
            var imagePath = Path.Combine(AppContext.BaseDirectory, "Assets", relativePath);
            AddOrGetImagePointer(imagePath, false, out var texture, out _, out _);
            return texture;
        });

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
        RefreshTargetProcess();
        bool suppress = _craftRunner.Progress.Running || _settingsOpen;
        if (_nativeSuppressed != suppress)
        {
            // A hidden native window cannot intercept the automated clicks or obscure Settings.
            ScreenHelper.ShowWindow(window.Handle, suppress ? 0 : 8); // HIDE / SHOWNA
            _nativeSuppressed = suppress;
        }
        long now = Stopwatch.GetTimestamp();
        float elapsed = (float)Stopwatch.GetElapsedTime(_lastFrameTimestamp, now).TotalSeconds;
        _lastFrameTimestamp = now;

        // Never leave a fading overlay on the desktop or over a minimized game.
        bool canRender = !suppress && IsRender && Resize();
        if (!canRender)
            _fadeProgress = 0f;
        else
            _fadeProgress = Math.Clamp(_fadeProgress
                + (_overlayVisible ? 1f : -1f) * elapsed / FadeDurationSeconds, 0f, 1f);

        // Smoothstep eases both endpoints; reversing direction keeps the current opacity.
        float opacity = _fadeProgress * _fadeProgress * (3f - 2f * _fadeProgress);
        bool visible = canRender && _fadeProgress > 0f;
        // The library pumps Win32 input once per frame. Keep typing responsive without
        // rendering the hidden overlay at the same rate as the active editor.
        int frameRate = visible ? (_overlayHasFocus ? 120 : 60) : 15;
        if (FPSLimit != frameRate) FPSLimit = frameRate;
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

        _craftPanel.Render(_craftRunner, StartCrafting);

        ImGui.End();
        ImGui.PopStyleVar(3);
    }

    private void RefreshTargetProcess()
    {
        long now = Stopwatch.GetTimestamp();
        if (now < _nextProcessSearch) return;
        _nextProcessSearch = now + Stopwatch.Frequency / 2;
        if (_targetProcess != null && !_targetProcess.HasExited)
        {
            // Process.Refresh invalidates MainWindowHandle and triggers a window lookup.
            // Poll here instead of repeating that lookup from every rendered frame.
            _targetProcess.Refresh();
            _targetWindowHandle = _targetProcess.MainWindowHandle;
            return;
        }
        _targetProcess?.Dispose();
        _targetProcess = null;
        _targetWindowHandle = IntPtr.Zero;
        _targetProcessId = 0;
        foreach (var process in Process.GetProcessesByName(TargetProcessName))
        {
            if (_targetProcess == null)
            {
                var handle = process.MainWindowHandle;
                if (handle != IntPtr.Zero)
                {
                    _targetProcess = process;
                    _targetWindowHandle = handle;
                    _targetProcessId = process.Id;
                    continue;
                }
            }
            process.Dispose();
        }
    }

    private void StartCrafting(CraftOptions options)
    {
        if (_targetProcess == null || _targetProcess.HasExited)
            throw new InvalidOperationException("游戏尚未运行。");
        _targetProcess.Refresh();
        IntPtr handle = _targetProcess.MainWindowHandle;
        if (!GameAutomation.TryGetBounds(handle, out var bounds))
            throw new InvalidOperationException("无法读取游戏客户区。");
        BackdropHelper.SetBlur(window.Handle, false);
        _backdropVisible = false;
        ScreenHelper.ShowWindow(window.Handle, 0);
        _nativeSuppressed = true;
        if (!ScreenHelper.SetForegroundWindow(handle))
            throw new InvalidOperationException("无法激活游戏窗口，请切回游戏后重试。");
        _craftRunner.Start(options, handle, bounds);
    }

    public override void Close()
    {
        _craftRunner.Stop();
        base.Close();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _craftRunner.Stop();
        base.Dispose(disposing);
        if (disposing)
        {
            _craftRunner.Dispose();
            _targetProcess?.Dispose();
        }
    }

    public bool Resize()
    {
        var handle = _targetWindowHandle;
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
