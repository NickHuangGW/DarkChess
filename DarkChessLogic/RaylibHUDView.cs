using System.Numerics;
using DarkChess.UI;
using Raylib_cs;
using RColor = Raylib_cs.Color;

namespace ConsoleApp1;

/// <summary>
/// IHUDView 的 Raylib 實作。
/// 負責：模式選擇畫面、回合指示器、訊息列、遊戲結束畫面。
/// 不含任何遊戲規則判斷。
/// </summary>
internal sealed class RaylibHUDView : IHUDView
{
    private enum HudState { ModeSelect, InGame, GameOver }

    private readonly int _screenW;
    private readonly int _screenH;
    private readonly int _margin;
    private readonly int _topBar;

    private HudState _state = HudState.ModeSelect;
    private string _turnText = "";
    private bool _isBotTurn;
    private string _message = "";
    private string _winnerName = "";

    // 動畫（Bot 思考提示倒數）
    private float _botIndicatorTimer;

    public event Action? OnNewGameRequested;
    public event Action<GameModeRequest>? OnModeSelected;

    /// <summary>是否應繪製棋盤（選單期間為 false，進入遊戲後為 true）。</summary>
    public bool ShowBoard => _state != HudState.ModeSelect;

    public RaylibHUDView(int screenW, int screenH, int margin, int topBar)
    {
        _screenW = screenW;
        _screenH = screenH;
        _margin = margin;
        _topBar = topBar;
    }

    // ── 更新（每幀呼叫）──────────────────────────────────────────────────

    public void Update(float dt)
    {
        if (_isBotTurn)
            _botIndicatorTimer += dt;
        else
            _botIndicatorTimer = 0f;

        if (!Raylib.IsMouseButtonPressed(MouseButton.Left)) return;

        switch (_state)
        {
            case HudState.ModeSelect:
                HandleModeSelectInput();
                break;
            case HudState.InGame:
                HandleInGameInput();
                break;
            case HudState.GameOver:
                HandleGameOverInput();
                break;
        }
    }

    private void HandleModeSelectInput()
    {
        var mouse = Raylib.GetMousePosition();
        if (Raylib.CheckCollisionPointRec(mouse, PvpButtonRect()))
            OnModeSelected?.Invoke(GameModeRequest.PvP);
        else if (Raylib.CheckCollisionPointRec(mouse, PvBotButtonRect()))
            OnModeSelected?.Invoke(GameModeRequest.PvBot);
    }

    private void HandleInGameInput()
    {
        var mouse = Raylib.GetMousePosition();
        if (Raylib.CheckCollisionPointRec(mouse, NewGameButtonRect()))
            OnNewGameRequested?.Invoke();
    }

    private void HandleGameOverInput()
    {
        var mouse = Raylib.GetMousePosition();
        if (Raylib.CheckCollisionPointRec(mouse, NewGameButtonRect()))
            OnNewGameRequested?.Invoke();
    }

    // ── IHUDView 實作 ─────────────────────────────────────────────────────

    public void UpdateTurnIndicator(string colorName, bool isBotTurn)
    {
        _turnText = colorName;
        _isBotTurn = isBotTurn;
    }

    public void ShowMessage(string message) => _message = message;

    public void ShowGameOver(string winnerColorName)
    {
        _winnerName = winnerColorName;
        _state = HudState.GameOver;
    }

    public void ShowModeSelection() => _state = HudState.ModeSelect;

    public void HideModeSelection() => _state = HudState.InGame;

    // ── 繪製（每幀呼叫）──────────────────────────────────────────────────

    public void Draw(Font font)
    {
        switch (_state)
        {
            case HudState.ModeSelect:
                DrawModeSelect(font);
                break;
            case HudState.InGame:
                DrawInGame(font);
                break;
            case HudState.GameOver:
                DrawGameOver(font);
                break;
        }
    }

    private void DrawModeSelect(Font font)
    {
        DrawText(font, "暗棋 Dark Chess", new Vector2(_screenW / 2f - 140, 80), 40,
            new RColor(60, 40, 30, 255));
        DrawText(font, "選擇遊戲模式", new Vector2(_screenW / 2f - 100, 140), 28,
            new RColor(110, 90, 70, 255));

        DrawButton(font, "玩家對玩家", PvpButtonRect());
        DrawButton(font, "對戰電腦", PvBotButtonRect());

        DrawText(font, "按 R 重置", new Vector2(_screenW / 2f - 50, 370), 18,
            new RColor(140, 120, 100, 255));
    }

    private void DrawInGame(Font font)
    {
        // 回合指示列
        var turnLabel = _isBotTurn ? $"輪到：{_turnText}（電腦思考中）" : $"輪到：{_turnText}";
        DrawText(font, turnLabel, new Vector2(_margin, 14), 26, new RColor(60, 40, 30, 255));
        DrawText(font, _message, new Vector2(_margin, 42), 18, new RColor(110, 90, 70, 255));

        // 重新開始按鈕（右上角小按鈕）
        var btnRect = NewGameButtonRect();
        var hover = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), btnRect);
        Raylib.DrawRectangleRec(btnRect, hover ? new RColor(200, 180, 150, 255) : new RColor(220, 200, 170, 255));
        Raylib.DrawRectangleLinesEx(btnRect, 2, new RColor(120, 100, 80, 255));
        DrawText(font, "重置", new Vector2(btnRect.X + 8, btnRect.Y + 6), 20,
            new RColor(60, 40, 30, 255));
    }

    private void DrawGameOver(Font font)
    {
        // 保持 InGame HUD（底層訊息列已顯示）
        DrawText(font, $"{_winnerName} 勝！", new Vector2(_margin, 14), 30,
            new RColor(190, 60, 50, 255));
        DrawText(font, _message, new Vector2(_margin, 42), 18, new RColor(110, 90, 70, 255));

        // 遮罩 + 提示
        var cx = _screenW / 2f;
        var cy = _screenH / 2f;
        Raylib.DrawRectangle(0, (int)(cy - 40), _screenW, 80, new RColor(0, 0, 0, 120));
        DrawText(font, $"{_winnerName}獲勝！按「重置」再來一局",
            new Vector2(cx - 170, cy - 20), 26, new RColor(255, 235, 180, 255));

        DrawButton(font, "重置", NewGameButtonRect());
    }

    // ── 矩形定義 ──────────────────────────────────────────────────────────

    private Rectangle PvpButtonRect()
        => new(_screenW / 2f - 120, 200, 240, 56);

    private Rectangle PvBotButtonRect()
        => new(_screenW / 2f - 120, 274, 240, 56);

    private Rectangle NewGameButtonRect()
        => new(_screenW - _margin - 64, 10, 64, 36);

    // ── 繪圖輔助 ─────────────────────────────────────────────────────────

    private void DrawButton(Font font, string label, Rectangle rect)
    {
        var hover = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), rect);
        Raylib.DrawRectangleRec(rect, hover ? new RColor(200, 180, 150, 255) : new RColor(220, 200, 170, 255));
        Raylib.DrawRectangleLinesEx(rect, 3, new RColor(100, 80, 60, 255));

        var m = Raylib.MeasureTextEx(font, label, 26, 0);
        var pos = new Vector2(rect.X + (rect.Width - m.X) / 2, rect.Y + (rect.Height - m.Y) / 2);
        Raylib.DrawTextEx(font, label, pos, 26, 0, new RColor(60, 40, 30, 255));
    }

    private static void DrawText(Font font, string text, Vector2 pos, float size, RColor color)
        => Raylib.DrawTextEx(font, text, pos, size, 1, color);
}
