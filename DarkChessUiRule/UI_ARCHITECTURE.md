Dark Chess - Unity Client Architecture (UI 視覺層架構)

1. 核心設計模式：Passive View (被動視圖)

本 Unity 專案嚴格採用 Passive View (MVP 變體) 模式。

View (視圖): 所有的 MonoBehaviour 腳本（如 BoardView, HUDView）皆為「被動」的。它們沒有決策能力，不持有遊戲狀態，僅負責兩件事：

捕捉玩家輸入 (Click/Touch) 並觸發事件 (Events) 通知 Presenter。

接收 Presenter 的指令來播放動畫、更新圖片或文字。

Presenter (橋接層): 負責訂閱 View 的事件，呼叫底層 C# Core 邏輯，並根據 Core 返回的結果指揮 View 進行畫面更新。

2. 渲染策略 (Rendering Strategy)

棋盤與棋子 (Board & Pieces): 必須使用 2D World Space (SpriteRenderer)。

點擊偵測依賴 Main Camera 的 Physics2D.Raycast 與棋子上的 BoxCollider2D。

禁止將棋盤放進 Canvas / UGUI 系統。

抬頭顯示器 (HUD): 必須使用 UGUI (Canvas)。

包含回合指示器、分數、系統選單。

點擊偵測依賴 EventSystem 與 GraphicRaycaster。

3. 目錄結構 (Directory Structure)

Assets/Scripts/View/Interfaces/: 存放所有介面定義，如 IDarkChessView。

Assets/Scripts/View/Implementations/: 存放掛載於 GameObject 的 MonoBehaviour 實作。

Assets/Scripts/Presenter/: 存放純 C# 類別的 Presenter。

4. 禁忌與反模式 (Anti-Patterns)

嚴禁 在任何 MonoBehaviour 中呼叫如 if (piece.Rank == PieceType.King) 的遊戲規則邏輯。

嚴禁 View 元件之間互相呼叫 (例如 BoardView 直接呼叫 HUDView)。所有的溝通必須透過 Presenter 中轉。