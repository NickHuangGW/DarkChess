using System.Numerics;
using Raylib_cs;
using DarkChess.Core;
using Color = DarkChess.Core.Color;
using RColor = Raylib_cs.Color;

// --test 模式：只跑邏輯檢驗，不開視窗
if (args.Contains("--test"))
    return ConsoleApp1.SelfTest.Run();

GameApp.Run();
return 0;

internal enum GameMode
{
    None,
    PvP,    // 玩家對玩家
    PvBot   // 玩家對電腦
}

internal static class GameApp
{
    private const int CellSize = 88;
    private const int Margin = 24;
    private const int TopBar = 64;
    private const int BoardW = Banqi.Cols * CellSize;
    private const int BoardH = Banqi.Rows * CellSize;
    private const int ScreenW = BoardW + Margin * 2;
    private const int ScreenH = BoardH + Margin * 2 + TopBar;

    public static void Run()
    {
        Raylib.InitWindow(ScreenW, ScreenH, "暗棋 Banqi");
        Raylib.SetTargetFPS(60);

        var font = LoadChineseFont(48);
        var gameMode = GameMode.None;
        BotPlayer? bot = null;
        Color? playerColor = null;
        var game = new Banqi();
        var selected = -1;
        var moves = new List<Move>();
        var botThinking = false;
        var botThinkTimer = 0f;

        while (!Raylib.WindowShouldClose())
        {
            var dt = Raylib.GetFrameTime();

            // --- 選擇模式畫面 ---
            if (gameMode == GameMode.None)
            {
                Raylib.BeginDrawing();
                Raylib.ClearBackground(new RColor(238, 228, 210, 255));
                DrawModeSelection(font, ref gameMode, ref bot, ref playerColor, game);
                Raylib.EndDrawing();
                continue;
            }

            // --- Bot 思考延遲 ---
            if (botThinking)
            {
                botThinkTimer -= dt;
                if (botThinkTimer <= 0)
                {
                    var action = bot!.ChooseAction(game);
                    if (action.Kind == ActionKind.Flip)
                        game.Flip(action.From);
                    else
                        game.TryMove(action.From, action.To);
                    
                    botThinking = false;
                    selected = -1;
                    moves.Clear();
                }
                
                Raylib.BeginDrawing();
                Raylib.ClearBackground(new RColor(238, 228, 210, 255));
                DrawTopBar(font, game);
                DrawBoard(font, game, selected, moves);
                DrawText(font, "電腦思考中...", new Vector2(ScreenW / 2 - 80, ScreenH / 2), 24, new RColor(200, 60, 60, 255));
                Raylib.EndDrawing();
                continue;
            }

            // --- 檢查是否輪到 Bot ---
            if (gameMode == GameMode.PvBot && bot is not null && game.Winner is null)
            {
                if (game.CurrentColor == bot.BotColor || (!game.FirstMoveDone))
                {
                    botThinking = true;
                    botThinkTimer = 0.5f; // 延遲 0.5 秒
                    Raylib.BeginDrawing();
                    Raylib.ClearBackground(new RColor(238, 228, 210, 255));
                    DrawTopBar(font, game);
                    DrawBoard(font, game, selected, moves);
                    Raylib.EndDrawing();
                    continue;
                }
            }

            // --- 輸入 ---
            if (Raylib.IsKeyPressed(KeyboardKey.R))
            {
                gameMode = GameMode.None;
                game.Reset();
                selected = -1;
                moves.Clear();
                bot = null;
                playerColor = null;
            }

            if (game.Winner is null && Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                var cell = CellAtMouse();
                if (cell >= 0)
                    HandleClick(game, cell, ref selected, moves);
            }

            // --- 繪圖 ---
            Raylib.BeginDrawing();
            Raylib.ClearBackground(new RColor(238, 228, 210, 255));

            DrawTopBar(font, game);
            DrawBoard(font, game, selected, moves);

            Raylib.EndDrawing();
        }

        Raylib.UnloadFont(font);
        Raylib.CloseWindow();
    }

    private static void DrawModeSelection(Font font, ref GameMode mode, ref BotPlayer? bot, ref Color? playerColor, Banqi game)
    {
        DrawText(font, "暗棋 Dark Chess", new Vector2(ScreenW / 2 - 140, 80), 40, new RColor(60, 40, 30, 255));
        DrawText(font, "選擇遊戲模式", new Vector2(ScreenW / 2 - 100, 140), 28, new RColor(110, 90, 70, 255));

        var pvpRect = new Rectangle(ScreenW / 2 - 120, 200, 240, 60);
        var pvBotRect = new Rectangle(ScreenW / 2 - 120, 280, 240, 60);

        // PvP 按鈕
        var pvpHover = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), pvpRect);
        Raylib.DrawRectangleRec(pvpRect, pvpHover ? new RColor(200, 180, 150, 255) : new RColor(220, 200, 170, 255));
        Raylib.DrawRectangleLinesEx(pvpRect, 3, new RColor(100, 80, 60, 255));
        DrawText(font, "玩家對玩家", new Vector2(pvpRect.X + 40, pvpRect.Y + 15), 28, new RColor(60, 40, 30, 255));

        // PvBot 按鈕
        var pvBotHover = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), pvBotRect);
        Raylib.DrawRectangleRec(pvBotRect, pvBotHover ? new RColor(200, 180, 150, 255) : new RColor(220, 200, 170, 255));
        Raylib.DrawRectangleLinesEx(pvBotRect, 3, new RColor(100, 80, 60, 255));
        DrawText(font, "對戰電腦", new Vector2(pvBotRect.X + 60, pvBotRect.Y + 15), 28, new RColor(60, 40, 30, 255));

        if (Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            if (pvpHover)
            {
                mode = GameMode.PvP;
            }
            else if (pvBotHover)
            {
                mode = GameMode.PvBot;
                // Bot 扮演黑方，玩家先手翻紅
                bot = new BotPlayer(Color.Black, searchDepth: 3);
                playerColor = Color.Red;
            }
        }

        DrawText(font, "按 R 返回選單", new Vector2(ScreenW / 2 - 90, 380), 20, new RColor(140, 120, 100, 255));
    }

    private static void HandleClick(Banqi game, int cell, ref int selected, List<Move> moves)
    {
        var c = game.Board[cell];

        // 已選取己方子：點到合法目的地 → 移動/吃子
        if (selected >= 0 && moves.Any(m => m.To == cell))
        {
            game.TryMove(selected, cell);
            selected = -1;
            moves.Clear();
            return;
        }

        // 點蓋著的子 → 翻棋
        if (c.IsHidden)
        {
            game.Flip(cell);
            selected = -1;
            moves.Clear();
            return;
        }

        // 點已翻開的己方子 → 選取並計算可走格
        if (c is { IsEmpty: false, FaceUp: true }
            && (game.CurrentColor is null || c.Piece!.Value.Color == game.CurrentColor))
        {
            selected = cell;
            moves.Clear();
            moves.AddRange(game.LegalMovesFrom(cell));
            return;
        }

        // 其他情況 → 取消選取
        selected = -1;
        moves.Clear();
    }

    private static int CellAtMouse()
    {
        var m = Raylib.GetMousePosition();
        var x = m.X - Margin;
        var y = m.Y - Margin - TopBar;
        if (x < 0 || y < 0 || x >= BoardW || y >= BoardH) return -1;
        var col = (int)(x / CellSize);
        var row = (int)(y / CellSize);
        return Banqi.Index(row, col);
    }

    private static void DrawTopBar(Font font, Banqi game)
    {
        var turn = game.Winner is { } w
            ? $"{Banqi.ColorName(w)}勝，按 R 重新開始"
            : game.CurrentColor is { } cc
                ? $"輪到：{Banqi.ColorName(cc)}"
                : "請翻棋開始";

        DrawText(font, turn, new Vector2(Margin, 14), 28, new RColor(60, 40, 30, 255));
        DrawText(font, game.Message, new Vector2(Margin, 40), 18, new RColor(110, 90, 70, 255));
    }

    private static void DrawBoard(Font font, Banqi game, int selected, List<Move> moves)
    {
        for (var i = 0; i < Banqi.Count; i++)
        {
            var (row, col) = Banqi.RowCol(i);
            var x = Margin + col * CellSize;
            var y = Margin + TopBar + row * CellSize;
            var pad = 4;
            var rect = new Rectangle(x + pad, y + pad, CellSize - pad * 2, CellSize - pad * 2);

            var cell = game.Board[i];

            if (cell.IsEmpty)
            {
                Raylib.DrawRectangleRec(rect, new RColor(222, 206, 178, 255));
                Raylib.DrawRectangleLinesEx(rect, 1, new RColor(180, 160, 130, 255));
            }
            else if (!cell.FaceUp)
            {
                // 蓋著的棋子：深色圓底
                DrawDisc(rect, new RColor(120, 80, 60, 255), new RColor(80, 50, 35, 255));
            }
            else
            {
                var piece = cell.Piece!.Value;
                var isRed = piece.Color == Color.Red;
                var face = isRed ? new RColor(245, 235, 220, 255) : new RColor(60, 55, 50, 255);
                var ring = isRed ? new RColor(190, 60, 50, 255) : new RColor(30, 28, 26, 255);
                DrawDisc(rect, face, ring);

                var ink = isRed ? new RColor(190, 40, 35, 255) : new RColor(235, 230, 225, 255);
                DrawGlyphCentered(font, piece.Glyph, rect, ink);
            }

            // 選取高亮
            if (i == selected)                                  
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                Raylib.DrawRectangleLinesEx(rect, 4, new RColor(40, 140, 60, 255));

            // 可走/可吃提示
            var mv = moves.FirstOrDefault(m => m.To == i);
            if (mv.To == i && (mv.Kind == MoveKind.Move || mv.Kind == MoveKind.Capture))
            {
                var cx = rect.X + rect.Width / 2;
                var cy = rect.Y + rect.Height / 2;
                if (mv.Kind == MoveKind.Move)
                    Raylib.DrawCircle((int)cx, (int)cy, 10, new RColor(40, 140, 60, 180));
                else
                    Raylib.DrawRectangleLinesEx(rect, 4, new RColor(210, 90, 40, 220));
            }
        }
    }

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

    private static void DrawText(Font font, string text, Vector2 pos, float size, RColor color)
        => Raylib.DrawTextEx(font, text, pos, size, 1, color);

    /// <summary>載入含中文字符的標楷體；找不到則退回 raylib 預設字型。</summary>
    private static Font LoadChineseFont(int size)
    {
        // 收集所有要顯示的字，建立 codepoint 表
        var sb = "帥仕相俥傌炮兵將士象車馬包卒"
                 + "暗棋輪到請翻開始紅方黑方勝先手換行動移子吃被光無可動點任格即為你"
                 + "重新按勝利，。（）";
        var codepoints = sb.Distinct().Select(ch => (int)ch).ToArray();

        var path = "C:/Windows/Fonts/kaiu.ttf"; // 標楷體
        if (!File.Exists(path)) path = "C:/Windows/Fonts/msjh.ttc"; // 退回微軟正黑體

        if (File.Exists(path))
        {
            var font = Raylib.LoadFontEx(path, size, codepoints, codepoints.Length);
            Raylib.SetTextureFilter(font.Texture, TextureFilter.Bilinear);
            return font;
        }
        return Raylib.GetFontDefault();
    }
}
