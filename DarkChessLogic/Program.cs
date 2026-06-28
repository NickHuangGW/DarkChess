using System.Linq;
using Raylib_cs;
using DarkChess.Core;
using DarkChess.UI.Presenter;

// --test 模式：只跑邏輯檢驗，不開視窗
if (args.Contains("--test"))
    return ConsoleApp1.SelfTest.Run();

GameApp.Run();
return 0;

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

        var boardView = new ConsoleApp1.RaylibBoardView(CellSize, Margin, TopBar);
        var hudView   = new ConsoleApp1.RaylibHUDView(ScreenW, ScreenH, Margin, TopBar);
        var presenter = new GamePresenter(boardView, hudView);

        while (!Raylib.WindowShouldClose())
        {
            var dt = Raylib.GetFrameTime();

            hudView.Update(dt);
            if (hudView.ShowBoard)
            {
                boardView.Update(dt);
                presenter.TriggerBotActionIfNeeded();
            }

            Raylib.BeginDrawing();
            Raylib.ClearBackground(new Raylib_cs.Color(238, 228, 210, 255));
            hudView.Draw(font);
            if (hudView.ShowBoard)
                boardView.Draw(font);
            Raylib.EndDrawing();
        }

        Raylib.UnloadFont(font);
        Raylib.CloseWindow();
    }

    /// <summary>載入含中文字符的標楷體；找不到則退回 raylib 預設字型。</summary>
    private static Font LoadChineseFont(int size)
    {
        var sb = "帥仕相俥傌炮兵將士象車馬包卒"
                 + "暗棋輪到請翻開始紅方黑方勝先手換行動移子吃被光無可動點任格即為你"
                 + "重新按勝利電腦對戰玩家模式選擇思考中重置獲，。（）！";
        var codepoints = sb.Distinct().Select(ch => (int)ch).ToArray();

        var path = "C:/Windows/Fonts/kaiu.ttf";
        if (!File.Exists(path)) path = "C:/Windows/Fonts/msjh.ttc";

        if (File.Exists(path))
        {
            var font = Raylib.LoadFontEx(path, size, codepoints, codepoints.Length);
            Raylib.SetTextureFilter(font.Texture, TextureFilter.Bilinear);
            return font;
        }
        return Raylib.GetFontDefault();
    }
}
