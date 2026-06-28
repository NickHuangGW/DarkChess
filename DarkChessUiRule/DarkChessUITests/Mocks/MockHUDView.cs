using DarkChess.UI;

namespace DarkChessUITests.Mocks;

/// <summary>IHUDView 的測試替身，記錄所有呼叫並可手動觸發事件。</summary>
public sealed class MockHUDView : IHUDView
{
    public event Action? OnNewGameRequested;
    public event Action<GameModeRequest>? OnModeSelected;

    // ── 呼叫記錄 ─────────────────────────────────────────────────────────
    public List<(string ColorName, bool IsBotTurn)> TurnIndicatorCalls { get; } = new();
    public List<string> MessageCalls { get; } = new();
    public List<string> GameOverCalls { get; } = new();
    public int ShowModeSelectionCount { get; private set; }
    public int HideModeSelectionCount { get; private set; }

    // ── IHUDView 實作 ─────────────────────────────────────────────────────
    public void UpdateTurnIndicator(string colorName, bool isBotTurn)
        => TurnIndicatorCalls.Add((colorName, isBotTurn));

    public void ShowMessage(string message) => MessageCalls.Add(message);

    public void ShowGameOver(string winnerColorName) => GameOverCalls.Add(winnerColorName);

    public void ShowModeSelection() => ShowModeSelectionCount++;

    public void HideModeSelection() => HideModeSelectionCount++;

    // ── 測試輔助 ─────────────────────────────────────────────────────────
    public void SimulateNewGameRequested() => OnNewGameRequested?.Invoke();

    public void SimulateModeSelected(GameModeRequest mode) => OnModeSelected?.Invoke(mode);
}
