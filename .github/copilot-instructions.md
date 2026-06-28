# Copilot 全局開發守則

1. **領域限制**: 本專案為「象棋暗棋 (Dark Chess)」，嚴禁使用標準象棋或西洋棋的規則。
2. **架構分層**:
    - `Assets/Scripts/Core/` 下的程式碼必須是純 C#，絕對禁止引入 `UnityEngine`。
    - `Assets/Scripts/View/` 下的程式碼負責 Unity 渲染，禁止包含遊戲勝負判定等核心商業邏輯。
3. **深入指引**: 當使用者要求實作「吃棋規則」、「狀態機」或「AI 演算法」時，請務必主動讀取並嚴格遵循專案根目錄的 `ARCHITECTURE.md` 文件。
```eof