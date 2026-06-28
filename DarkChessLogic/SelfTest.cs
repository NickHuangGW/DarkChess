namespace ConsoleApp1;

/// <summary>規則邏輯檢驗。以 `dotnet run -- --test` 執行，全數通過印出 ALL PASS。</summary>
public static class SelfTest
{
    private static int _pass;
    private static int _fail;

    public static int Run()
    {
        _pass = _fail = 0;

        TestPieceCounts();
        TestCaptureByRank();
        TestAdjacentMoveOnly();
        TestCannonScreenCapture();
        TestColorBinding();
        TestWinByElimination();

        Console.WriteLine($"\n通過 {_pass}，失敗 {_fail}");
        if (_fail == 0) Console.WriteLine("ALL PASS");
        return _fail == 0 ? 0 : 1;
    }

    private static void Check(string name, bool ok)
    {
        if (ok) { _pass++; Console.WriteLine($"  [PASS] {name}"); }
        else { _fail++; Console.WriteLine($"  [FAIL] {name}"); }
    }

    // ① 棋子數量
    private static void TestPieceCounts()
    {
        Console.WriteLine("① 棋子數量");
        var set = Piece.FullSet();
        Check("共 32 子", set.Count == 32);
        Check("紅 16 子", set.Count(p => p.Color == Color.Red) == 16);
        Check("黑 16 子", set.Count(p => p.Color == Color.Black) == 16);
        Check("帥/將 各 1", set.Count(p => p.Kind == PieceKind.General) == 2);
        Check("兵/卒 各 5", set.Count(p => p is { Kind: PieceKind.Soldier, Color: Color.Red }) == 5);
        Check("仕/相/俥/傌/包 各 2",
            new[] { PieceKind.Advisor, PieceKind.Elephant, PieceKind.Chariot, PieceKind.Horse, PieceKind.Cannon }
                .All(k => set.Count(p => p.Kind == k && p.Color == Color.Red) == 2));
    }

    // ③ 吃子大小
    private static void TestCaptureByRank()
    {
        Console.WriteLine("③ 吃子大小");
        Piece R(PieceKind k) => new(Color.Red, k);
        Piece B(PieceKind k) => new(Color.Black, k);

        Check("帥吃將(同階)", R(PieceKind.General).CanCaptureByRank(B(PieceKind.General)));
        Check("帥吃士(高吃低)", R(PieceKind.General).CanCaptureByRank(B(PieceKind.Advisor)));
        Check("車不能吃帥(低吃高)", !R(PieceKind.Chariot).CanCaptureByRank(B(PieceKind.General)));
        Check("兵吃帥(例外)", R(PieceKind.Soldier).CanCaptureByRank(B(PieceKind.General)));
        Check("帥不能吃兵(例外)", !R(PieceKind.General).CanCaptureByRank(B(PieceKind.Soldier)));
        Check("同色不可吃", !R(PieceKind.General).CanCaptureByRank(R(PieceKind.Soldier)));
    }

    // ④-a 移動限制：非炮只能走相鄰空格
    private static void TestAdjacentMoveOnly()
    {
        Console.WriteLine("④ 移動限制（非炮）");
        var g = new Banqi(seed: 1);
        ClearBoard(g);
        g.FirstMoveDone = true;
        g.CurrentColor = Color.Red;

        // 在 (1,1) 放紅車
        var at = Banqi.Index(1, 1);
        Place(g, at, new Piece(Color.Red, PieceKind.Chariot));

        Check("可走相鄰空格", g.IsLegalMove(at, Banqi.Index(1, 2)));
        Check("不可走斜線", !g.IsLegalMove(at, Banqi.Index(2, 2)));
        Check("不可跨兩格", !g.IsLegalMove(at, Banqi.Index(1, 3)));

        // 鄰格放低階敵子可吃，放己方子不可
        Place(g, Banqi.Index(1, 2), new Piece(Color.Black, PieceKind.Horse));
        Check("可吃相鄰低階敵子", g.IsLegalMove(at, Banqi.Index(1, 2)));
        Place(g, Banqi.Index(1, 0), new Piece(Color.Red, PieceKind.Horse));
        Check("不可吃己方子", !g.IsLegalMove(at, Banqi.Index(1, 0)));
    }

    // ④-b 炮跳吃
    private static void TestCannonScreenCapture()
    {
        Console.WriteLine("④ 炮跳吃");
        var g = new Banqi(seed: 1);
        ClearBoard(g);
        g.FirstMoveDone = true;
        g.CurrentColor = Color.Red;

        var cannon = Banqi.Index(0, 0);
        Place(g, cannon, new Piece(Color.Red, PieceKind.Cannon));

        // 同列 (0,3) 放敵將，(0,1) 放砲架
        var target = Banqi.Index(0, 3);
        Place(g, target, new Piece(Color.Black, PieceKind.General));

        Check("無砲架不可吃", !g.IsLegalMove(cannon, target));

        Place(g, Banqi.Index(0, 1), new Piece(Color.Black, PieceKind.Soldier)); // 一個砲架
        Check("隔一砲架可吃任意階級(吃將)", g.IsLegalMove(cannon, target));

        Place(g, Banqi.Index(0, 2), new Piece(Color.Red, PieceKind.Soldier)); // 變兩個砲架
        Check("兩個砲架不可吃", !g.IsLegalMove(cannon, target));

        // 炮走空格仍只能一格
        var g2 = new Banqi(seed: 2);
        ClearBoard(g2);
        g2.FirstMoveDone = true;
        g2.CurrentColor = Color.Red;
        var c2 = Banqi.Index(0, 0);
        Place(g2, c2, new Piece(Color.Red, PieceKind.Cannon));
        Check("炮走空格走一格", g2.IsLegalMove(c2, Banqi.Index(0, 1)));
        Check("炮不可走空格兩格", !g2.IsLegalMove(c2, Banqi.Index(0, 2)));
    }

    // ⑤ 顏色綁定
    private static void TestColorBinding()
    {
        Console.WriteLine("⑤ 顏色綁定");
        var g = new Banqi(seed: 1);
        // 找一個紅子翻開作為第一手
        var redIdx = Enumerable.Range(0, Banqi.Count)
            .First(i => g.Board[i].Piece!.Value.Color == Color.Red);
        g.Flip(redIdx);
        Check("首手翻紅 → 換黑方行動", g.CurrentColor == Color.Black);
        Check("首手後 FirstMoveDone", g.FirstMoveDone);
    }

    // ⑥ 吃光判定勝負
    private static void TestWinByElimination()
    {
        Console.WriteLine("⑥ 勝負（吃光）");
        var g = new Banqi(seed: 3);
        ClearBoard(g);
        g.FirstMoveDone = true;
        g.CurrentColor = Color.Red;

        // 場上只剩紅車與唯一黑卒相鄰，紅吃掉即黑方全滅
        Place(g, Banqi.Index(0, 0), new Piece(Color.Red, PieceKind.Chariot));
        Place(g, Banqi.Index(0, 1), new Piece(Color.Black, PieceKind.Soldier));

        var ok = g.TryMove(Banqi.Index(0, 0), Banqi.Index(0, 1));
        Check("成功吃掉最後黑子", ok);
        Check("黑子全滅 → 紅方勝", g.Winner == Color.Red);
    }

    // --- 測試輔助：清空棋盤、放子 ---
    private static void ClearBoard(Banqi g)
    {
        for (var i = 0; i < Banqi.Count; i++)
            g.Board[i] = new Cell { Piece = null, FaceUp = false };
    }

    private static void Place(Banqi g, int i, Piece p)
    {
        g.Board[i] = new Cell { Piece = p, FaceUp = true };
    }
}
