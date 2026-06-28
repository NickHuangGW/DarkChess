namespace DarkChess.UI;

/// <summary>
/// 棋盤視圖介面。
/// 實作者（Unity BoardView MonoBehaviour）負責：
///   - 偵測玩家點擊後觸發 OnCellClicked
///   - 依 Presenter 指令渲染棋子、動畫與高亮
/// </summary>
public interface IBoardView
{
    // ── View → Presenter ────────────────────────────────────────────────

    /// <summary>玩家點擊棋盤上某格（0-based index）。</summary>
    event Action<int> OnCellClicked;

    // ── Presenter → View ────────────────────────────────────────────────

    /// <summary>更新單一格子的顯示狀態。</summary>
    void ShowCell(int index, CellViewModel vm);

    /// <summary>全盤刷新（遊戲重置或初始化時呼叫）。</summary>
    void RefreshAll(IReadOnlyList<CellViewModel> cells);

    /// <summary>標示選取的格子。</summary>
    void HighlightSelected(int index);

    /// <summary>顯示可走空格提示（圓點）。</summary>
    void ShowLegalMoves(IReadOnlyList<int> indices);

    /// <summary>顯示可吃目標提示（紅框）。</summary>
    void ShowLegalCaptures(IReadOnlyList<int> indices);

    /// <summary>清除所有高亮與提示。</summary>
    void ClearHighlights();

    /// <summary>播放翻棋動畫，完成後呼叫 onComplete（可為 null）。</summary>
    void PlayFlipAnimation(int index, Action? onComplete = null);

    /// <summary>播放移動動畫，完成後呼叫 onComplete（可為 null）。</summary>
    void PlayMoveAnimation(int from, int to, Action? onComplete = null);

    /// <summary>播放吃子動畫，完成後呼叫 onComplete（可為 null）。</summary>
    void PlayCaptureAnimation(int from, int to, Action? onComplete = null);
}
