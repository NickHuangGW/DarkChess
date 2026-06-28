using System.Numerics;
using DarkChess.Core;
using DarkChess.UI;
using Raylib_cs;
using RColor = Raylib_cs.Color;

namespace ConsoleApp1;

/// <summary>
/// IBoardView 的 Raylib 實作。
/// 職責：維護顯示狀態、繪製棋盤、偵測點擊、推進動畫計時器。
/// 不含任何遊戲規則邏輯。
/// </summary>
internal sealed class RaylibBoardView : IBoardView
{
    // ── 版面常數（由外部傳入，與 Program.cs 共用）────────────────────────
    private readonly int _cellSize;
    private readonly int _margin;
    private readonly int _topBar;
    private int BoardW => Banqi.Cols * _cellSize;
    private int BoardH => Banqi.Rows * _cellSize;

    // ── 顯示狀態 ─────────────────────────────────────────────────────────
    private readonly CellViewModel?[] _cells = new CellViewModel?[Banqi.Count];
    private int _selectedIndex = -1;
    private readonly HashSet<int> _legalMoves = new();
    private readonly HashSet<int> _legalCaptures = new();

    // ── 動畫計時器 ────────────────────────────────────────────────────────
    private Action? _pendingAnim;
    private float _animTimer;

    // ── 事件 ──────────────────────────────────────────────────────────────
    public event Action<int>? OnCellClicked;

    public RaylibBoardView(int cellSize, int margin, int topBar)
    {
        _cellSize = cellSize;
        _margin = margin;
        _topBar = topBar;
    }

    // ── 更新（每幀呼叫）──────────────────────────────────────────────────

    /// <summary>推進動畫計時器，計時結束後執行回呼；並偵測滑鼠點擊。</summary>
    public void Update(float dt)
    {
        // 動畫計時
        if (_pendingAnim is not null)
        {
            _animTimer -= dt;
            if (_animTimer <= 0f)
            {
                var cb = _pendingAnim;
                _pendingAnim = null;
                cb.Invoke();
            }
            return; // 動畫中不處理點擊
        }

        HandleInput();
    }

    private void HandleInput()
    {
        if (!Raylib.IsMouseButtonPressed(MouseButton.Left)) return;
        var idx = CellAtMouse();
        if (idx >= 0)
            OnCellClicked?.Invoke(idx);
    }

    private int CellAtMouse()
    {
        var m = Raylib.GetMousePosition();
        var x = m.X - _margin;
        var y = m.Y - _margin - _topBar;
        if (x < 0 || y < 0 || x >= BoardW || y >= BoardH) return -1;
        var col = (int)(x / _cellSize);
        var row = (int)(y / _cellSize);
        return Banqi.Index(row, col);
    }

    // ── IBoardView 實作 ───────────────────────────────────────────────────

    public void ShowCell(int index, CellViewModel vm) => _cells[index] = vm;

    public void RefreshAll(IReadOnlyList<CellViewModel> cells)
    {
        for (var i = 0; i < cells.Count; i++)
            _cells[i] = cells[i];
        _selectedIndex = -1;
        _legalMoves.Clear();
        _legalCaptures.Clear();
    }

    public void HighlightSelected(int index) => _selectedIndex = index;

    public void ShowLegalMoves(IReadOnlyList<int> indices)
    {
        _legalMoves.Clear();
        foreach (var i in indices) _legalMoves.Add(i);
    }

    public void ShowLegalCaptures(IReadOnlyList<int> indices)
    {
        _legalCaptures.Clear();
        foreach (var i in indices) _legalCaptures.Add(i);
    }

    public void ClearHighlights()
    {
        _selectedIndex = -1;
        _legalMoves.Clear();
        _legalCaptures.Clear();
    }

    public void PlayFlipAnimation(int index, Action? onComplete = null)
        => ScheduleAnim(0.25f, onComplete);

    public void PlayMoveAnimation(int from, int to, Action? onComplete = null)
        => ScheduleAnim(0.25f, onComplete);

    public void PlayCaptureAnimation(int from, int to, Action? onComplete = null)
        => ScheduleAnim(0.3f, onComplete);

    private void ScheduleAnim(float duration, Action? onComplete)
    {
        _animTimer = duration;
        _pendingAnim = onComplete ?? (() => { });
    }

    // ── 繪製（每幀呼叫）──────────────────────────────────────────────────

    public void Draw(Font font)
    {
        for (var i = 0; i < Banqi.Count; i++)
        {
            var (row, col) = Banqi.RowCol(i);
            var x = _margin + col * _cellSize;
            var y = _margin + _topBar + row * _cellSize;
            const int pad = 4;
            var rect = new Rectangle(x + pad, y + pad, _cellSize - pad * 2, _cellSize - pad * 2);

            var vm = _cells[i];

            if (vm is null || vm.IsEmpty)
            {
                Raylib.DrawRectangleRec(rect, new RColor(222, 206, 178, 255));
                Raylib.DrawRectangleLinesEx(rect, 1, new RColor(180, 160, 130, 255));
            }
            else if (vm.IsHidden)
            {
                DrawDisc(rect, new RColor(120, 80, 60, 255), new RColor(80, 50, 35, 255));
            }
            else if (vm.Piece is { } piece)
            {
                var isRed = piece.Color == DarkChess.Core.Color.Red;
                var face = isRed ? new RColor(245, 235, 220, 255) : new RColor(60, 55, 50, 255);
                var ring = isRed ? new RColor(190, 60, 50, 255) : new RColor(30, 28, 26, 255);
                DrawDisc(rect, face, ring);

                var ink = isRed ? new RColor(190, 40, 35, 255) : new RColor(235, 230, 225, 255);
                DrawGlyphCentered(font, piece.Glyph, rect, ink);
            }

            // 選取高亮
            if (i == _selectedIndex)
                Raylib.DrawRectangleLinesEx(rect, 4, new RColor(40, 140, 60, 255));

            // 可走提示（綠點）
            if (_legalMoves.Contains(i))
            {
                var cx = (int)(rect.X + rect.Width / 2);
                var cy = (int)(rect.Y + rect.Height / 2);
                Raylib.DrawCircle(cx, cy, 10, new RColor(40, 140, 60, 180));
            }

            // 可吃提示（橘框）
            if (_legalCaptures.Contains(i))
                Raylib.DrawRectangleLinesEx(rect, 4, new RColor(210, 90, 40, 220));

            // 動畫中閃爍效果（整體略亮）
            if (_pendingAnim is not null)
                Raylib.DrawRectangleRec(rect, new RColor(255, 255, 255, 30));
        }
    }

    // ── 繪圖輔助 ─────────────────────────────────────────────────────────

    private static void DrawDisc(Rectangle rect, RColor face, RColor ring)
    {
        var cx = (int)(rect.X + rect.Width / 2);
        var cy = (int)(rect.Y + rect.Height / 2);
        var r = (int)(rect.Width / 2);
        Raylib.DrawCircle(cx, cy, r, ring);
        Raylib.DrawCircle(cx, cy, r - 4, face);
    }

    private static void DrawGlyphCentered(Font font, string text, Rectangle rect, RColor color)
    {
        const float size = 52;
        var m = Raylib.MeasureTextEx(font, text, size, 0);
        var pos = new Vector2(
            rect.X + (rect.Width - m.X) / 2,
            rect.Y + (rect.Height - m.Y) / 2);
        Raylib.DrawTextEx(font, text, pos, size, 0, color);
    }
}
