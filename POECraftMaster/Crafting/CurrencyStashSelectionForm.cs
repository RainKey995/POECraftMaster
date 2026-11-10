using System.Drawing;
using System.Windows.Forms;

namespace POECraftMaster.Crafting;

internal static class CurrencyStashRegion
{
    public static Rectangle FromDrag(Point start, Point end, Size clientSize)
    {
        int x1 = Math.Clamp(start.X, 0, clientSize.Width), y1 = Math.Clamp(start.Y, 0, clientSize.Height);
        int x2 = Math.Clamp(end.X, 0, clientSize.Width), y2 = Math.Clamp(end.Y, 0, clientSize.Height);
        return Rectangle.FromLTRB(Math.Min(x1, x2), Math.Min(y1, y2), Math.Max(x1, x2), Math.Max(y1, y2));
    }

    public static void Validate(Rectangle region, Size clientSize)
    {
        if (region.Width < 16 || region.Height < 16 || !new Rectangle(Point.Empty, clientSize).Contains(region))
            throw new ArgumentException("请拖动框选游戏客户区内的完整通货仓库区域。");
    }
}

// Runs on the crafting worker's STA thread. The modal form pumps its own messages.
internal sealed class CurrencyStashSelectionForm : Form
{
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 40 };
    private Point? _start;
    private Rectangle _preview;
    private bool _finished;
    private string _hint = "按住鼠标左键，拖动框选通货仓库区域；松开确认。Esc / F12 / 右键取消。";
    public Rectangle Selection { get; private set; }

    public CurrencyStashSelectionForm(Rectangle gameBounds, CancellationToken token, Func<bool> gameUnchanged)
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.None;
        Bounds = gameBounds;
        ShowInTaskbar = false;
        TopMost = true;
        BackColor = Color.Black;
        ForeColor = Color.White;
        Opacity = .45;
        Cursor = Cursors.Cross;
        DoubleBuffered = true;
        KeyPreview = true;
        _timer.Tick += (_, _) =>
        {
            if (token.IsCancellationRequested || !gameUnchanged()) CancelSelection();
        };
        Shown += (_, _) => _timer.Start();
        Deactivate += (_, _) => CancelSelection();
    }

    private void CancelSelection()
    {
        if (_finished) return;
        _finished = true;
        _timer.Stop();
        DialogResult = DialogResult.Cancel;
        Close();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode is Keys.Escape or Keys.F12)
        {
            e.SuppressKeyPress = true;
            CancelSelection();
        }
        base.OnKeyDown(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Right) { CancelSelection(); return; }
        if (e.Button != MouseButtons.Left) return;
        _start = e.Location;
        _preview = Rectangle.Empty;
        Capture = true;
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_start is not Point start) return;
        _preview = CurrencyStashRegion.FromDrag(start, e.Location, ClientSize);
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button != MouseButtons.Left || _start is not Point start) return;
        _preview = CurrencyStashRegion.FromDrag(start, e.Location, ClientSize);
        _start = null;
        Capture = false;
        try { CurrencyStashRegion.Validate(_preview, ClientSize); }
        catch (ArgumentException)
        {
            _hint = "选择区域太小，请重新拖动框选完整仓库；Esc / F12 / 右键取消。";
            Invalidate();
            return;
        }
        Selection = _preview;
        _finished = true;
        _timer.Stop();
        DialogResult = DialogResult.OK;
        Close();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        TextRenderer.DrawText(e.Graphics, _hint, Font,
            new Rectangle(16, 16, Math.Max(1, ClientSize.Width - 32), 80), Color.White,
            TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
        if (_preview.Width <= 0 || _preview.Height <= 0) return;
        using var pen = new Pen(Color.Lime, 3);
        e.Graphics.DrawRectangle(pen, _preview);
        TextRenderer.DrawText(e.Graphics, $"{_preview.Width} × {_preview.Height}", Font,
            new Point(_preview.Left + 6, _preview.Top + 6), Color.Lime);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _timer.Dispose();
        base.Dispose(disposing);
    }
}
