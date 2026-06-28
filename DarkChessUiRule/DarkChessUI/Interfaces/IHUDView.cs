namespace DarkChess.UI;

/// <summary>
/// HUD 視圖介面（Canvas / UGUI 層）。
/// 實作者（Unity HUDView MonoBehaviour）負責：
///   - 觸發重新開始、模式選擇等事件
///   - 依 Presenter 指令更新回合指示、訊息、遊戲結束畫面
/// 嚴禁 HUDView 直接呼叫 BoardView，所有溝通必須透過 Presenter。
/// </summary>
public interface IHUDView
{
    // ── View → Presenter ────────────────────────────────────────────────

    /// <summary>玩家請求重新開始遊戲。</summary>
    event Action OnNewGameRequested;

    /// <summary>玩家選擇遊戲模式（PvP / PvBot）。</summary>
    event Action<GameModeRequest> OnModeSelected;

    // ── Presenter → View ────────────────────────────────────────────────

    /// <summary>更新回合指示器。</summary>
    /// <param name="colorName">輪到的顏色名稱（如「紅方」）。</param>
    /// <param name="isBotTurn">是否為 Bot 回合（可用於顯示提示文字）。</param>
    void UpdateTurnIndicator(string colorName, bool isBotTurn);

    /// <summary>顯示即時訊息（如「紅方 炮 吃 將」）。</summary>
    void ShowMessage(string message);

    /// <summary>顯示遊戲結束畫面。</summary>
    /// <param name="winnerColorName">勝方顏色名稱。</param>
    void ShowGameOver(string winnerColorName);

    /// <summary>顯示模式選擇 UI（遊戲開始/重置時）。</summary>
    void ShowModeSelection();

    /// <summary>隱藏模式選擇 UI（進入遊戲後）。</summary>
    void HideModeSelection();
}
