# Dark Chess (暗棋) 專案架構與 AI 開發指南

## 1. 專案背景與領域知識 (Domain Knowledge)
本專案為**「象棋暗棋 (Dark Chess / Banqi)」**的單機版遊戲。
AI 在生成邏輯時，**絕對禁止**使用傳統中國象棋 (Xiangqi) 或西洋棋 (Chess) 的移動與吃子規則。

### 1.1 棋盤與棋子定義
*   **棋盤配置 (Board)**: 嚴格定義為 `4 x 8` 的二維陣列（共 32 格）。不具有「楚河漢界」、「九宮格」等概念。
*   **棋子狀態 (Piece State)**: 每個棋子有三種狀態：`蓋牌 (Face-down)`、`紅方正面 (Red Face-up)`、`黑方正面 (Black Face-up)`。
*   **初始佈局**: 遊戲開始時，32 顆棋子隨機覆蓋於 4x8 的格子中。

### 1.2 行動規則 (Action Rules)
每回合玩家只能從以下兩種行動擇一：
1.  **翻棋 (Reveal)**: 將一個蓋牌的棋子翻轉為正面。
    *   *特例*: 第一位翻開棋子的玩家，該棋子的顏色即代表他本局的陣營。
2.  **移動/吃子 (Move / Capture)**:
    *   只能操作己方已翻開的棋子。
    *   **移動**: 只能往上下左右（直或橫）移動**恰好一格**至空格。禁止斜走。
    *   **吃子**: 只能往上下左右（直或橫）移動**恰好一格**，取代敵方已翻開的棋子（需符合吃子位階，見 1.3）。
    *   *【特例 - 炮/砲】*: 「炮」在移動時與一般棋子相同（僅走一格）；但「吃子」時，**必須且只能**沿直/橫線隔著**恰好一顆棋子**（不論敵我、蓋牌或翻開的炮台）跳吃任意敵方已翻開的棋子（無視階級限制）。炮不能空跳。

### 1.3 階級吃子規則 (Hierarchy)
*   **一般排序**: `帥(將) > 仕(士) > 相(象) > 俥(車) > 傌(馬) > 兵(卒)`
*   同階級可互吃。
*   *【特例 - 將帥與兵卒】*: 帥(將) **絕對不可** 吃兵(卒)；兵(卒) **只能** 吃帥(將)與兵(卒)。
*   *【特例 - 炮】*: 炮可跳吃任何階級，也可被比自己高階的棋子（將、士、象、車、馬、炮）吃掉。

---

## 2. 系統架構 (Clean Architecture)
本專案嚴格區分「核心邏輯 (Core)」與「Unity 視覺層 (View)」。AI 在生成程式碼時，需依據資料夾路徑套用對應的規則。

### 2.1 `Assets/Scripts/Core/` (純 C# 邏輯層)
此目錄下的程式碼為純 .NET 邏輯，**嚴禁** `using UnityEngine;`，也不可繼承 `MonoBehaviour`。
*   **領域模型 (Domain)**: `Board`, `Piece`, `Enums (PieceType, PieceColor, PieceState)`。
*   **規則引擎 (Rule Engine)**: 負責實作上述 1.2 與 1.3 的驗證邏輯，如 `bool CanCapture(Piece source, Piece target)`。必須具備高度可測試性。
*   **狀態機 (Game State Machine)**: 管理回合流程（如 `RedTurn`, `BlackTurn`, `GameOver`）與勝利判定（一方棋子全滅或無步可走）。
*   **電腦 AI (Bot)**: 實作 Minimax 演算法搭配 Alpha-Beta 剪枝的 `BotPlayer`。

### 2.2 `Assets/Scripts/View/` (Unity 客戶端)
此目錄負責視覺渲染與輸入處理，作為 Core 層的 Passive View。
*   **`BoardView` (2D Sprites)**:
    *   使用 `SpriteRenderer` 在 World Space (世界座標) 繪製棋盤與棋子。
    *   負責根據螢幕比例動態計算 `Camera.main.orthographicSize`。
    *   接收物理射線 (`Physics2D.Raycast`) 處理玩家點擊。
*   **`HUDView` (UGUI)**:
    *   使用 Canvas 繪製回合指示器、死亡棋子區 (Graveyard) 與系統選單。
*   **`GamePresenter` (橋接層)**:
    *   負責監聽 View 的點擊事件，呼叫 Core 執行邏輯，並根據 Core 的結果回呼 View 播放翻棋/移動/吃子動畫。

---

## 3. 程式碼撰寫規範 (Coding Conventions)
AI 在協助重構或新增功能時，請遵守：
1.  **測試驅動 (TDD 友善)**: Core 層邏輯設計應便於編寫 xUnit/NUnit 單元測試，特別是吃棋位階與「炮」的判定邏輯。
2.  **解耦 (Decoupling)**: Unity 腳本只能呼叫 Core 層公開的 API，不應包含任何遊戲勝負判斷邏輯。
3.  **命名慣例**:
    *   C# Property 使用 PascalCase (`CurrentTurn`)。
    *   Private 欄位使用底線開頭駝峰式 (`_boardConfig`)。
    *   Interface 必須以 `I` 開頭 (`IDarkChessView`)。
```eof