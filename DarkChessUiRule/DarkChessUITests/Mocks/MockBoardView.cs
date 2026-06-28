using DarkChess.UI;

namespace DarkChessUITests.Mocks;

/// <summary>IBoardView 的測試替身，記錄所有呼叫並可手動觸發事件。</summary>
public sealed class MockBoardView : IBoardView
{
    public event Action<int>? OnCellClicked;

    // ── 呼叫記錄 ─────────────────────────────────────────────────────────
    public List<(int Index, CellViewModel Vm)> ShowCellCalls { get; } = new();
    public List<IReadOnlyList<CellViewModel>> RefreshAllCalls { get; } = new();
    public List<int> HighlightSelectedCalls { get; } = new();
    public List<IReadOnlyList<int>> ShowLegalMovesCalls { get; } = new();
    public List<IReadOnlyList<int>> ShowLegalCapturesCalls { get; } = new();
    public int ClearHighlightCount { get; private set; }
    public List<int> FlipAnimCalls { get; } = new();
    public List<(int From, int To)> MoveAnimCalls { get; } = new();
    public List<(int From, int To)> CaptureAnimCalls { get; } = new();

    // ── IBoardView 實作 ───────────────────────────────────────────────────
    public void ShowCell(int index, CellViewModel vm) => ShowCellCalls.Add((index, vm));

    public void RefreshAll(IReadOnlyList<CellViewModel> cells) => RefreshAllCalls.Add(cells);

    public void HighlightSelected(int index) => HighlightSelectedCalls.Add(index);

    public void ShowLegalMoves(IReadOnlyList<int> indices) => ShowLegalMovesCalls.Add(indices);

    public void ShowLegalCaptures(IReadOnlyList<int> indices) => ShowLegalCapturesCalls.Add(indices);

    public void ClearHighlights() => ClearHighlightCount++;

    public void PlayFlipAnimation(int index, Action? onComplete = null)
    {
        FlipAnimCalls.Add(index);
        onComplete?.Invoke(); // 測試環境立即完成
    }

    public void PlayMoveAnimation(int from, int to, Action? onComplete = null)
    {
        MoveAnimCalls.Add((from, to));
        onComplete?.Invoke();
    }

    public void PlayCaptureAnimation(int from, int to, Action? onComplete = null)
    {
        CaptureAnimCalls.Add((from, to));
        onComplete?.Invoke();
    }

    // ── 測試輔助 ─────────────────────────────────────────────────────────
    public void SimulateClick(int index) => OnCellClicked?.Invoke(index);
}
