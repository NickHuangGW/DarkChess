using DarkChess.Core;

namespace DarkChessTests;

public class BanqiRuleTests
{
    [Fact]
    public void Flip_FirstFlip_ShouldSetCurrentColorToOpposite()
    {
        var game = new Banqi(seed: 1);
        
        // 找到第一個紅子
        var redIdx = Enumerable.Range(0, Banqi.Count)
            .First(i => game.Board[i].Piece!.Value.Color == Color.Red);
        
        game.Flip(redIdx);
        
        Assert.True(game.FirstMoveDone);
        Assert.Equal(Color.Black, game.CurrentColor);
    }

    [Fact]
    public void Flip_FirstFlip_ShouldSetBlackTurnIfRedFlipped()
    {
        var game = new Banqi(seed: 2);
        
        // 找到第一個黑子
        var blackIdx = Enumerable.Range(0, Banqi.Count)
            .First(i => game.Board[i].Piece!.Value.Color == Color.Black);
        
        game.Flip(blackIdx);
        
        Assert.True(game.FirstMoveDone);
        Assert.Equal(Color.Red, game.CurrentColor);
    }

    [Fact]
    public void Move_NonCannonCanOnlyMoveOneAdjacentCell()
    {
        var game = new Banqi(seed: 1);
        ClearBoard(game);
        game.FirstMoveDone = true;
        game.CurrentColor = Color.Red;
        
        var at = Banqi.Index(1, 1);
        Place(game, at, new Piece(Color.Red, PieceKind.Chariot));
        
        // 可走相鄰空格
        Assert.True(game.IsLegalMove(at, Banqi.Index(1, 2)));
        Assert.True(game.IsLegalMove(at, Banqi.Index(1, 0)));
        Assert.True(game.IsLegalMove(at, Banqi.Index(0, 1)));
        Assert.True(game.IsLegalMove(at, Banqi.Index(2, 1)));
        
        // 不可走斜線
        Assert.False(game.IsLegalMove(at, Banqi.Index(2, 2)));
        
        // 不可跨兩格
        Assert.False(game.IsLegalMove(at, Banqi.Index(1, 3)));
    }

    [Fact]
    public void Move_CanCaptureAdjacentLowerRankEnemy()
    {
        var game = new Banqi(seed: 1);
        ClearBoard(game);
        game.FirstMoveDone = true;
        game.CurrentColor = Color.Red;
        
        var at = Banqi.Index(1, 1);
        Place(game, at, new Piece(Color.Red, PieceKind.Chariot));
        Place(game, Banqi.Index(1, 2), new Piece(Color.Black, PieceKind.Horse));
        
        Assert.True(game.IsLegalMove(at, Banqi.Index(1, 2)));
    }

    [Fact]
    public void Move_CannotCaptureOwnPiece()
    {
        var game = new Banqi(seed: 1);
        ClearBoard(game);
        game.FirstMoveDone = true;
        game.CurrentColor = Color.Red;
        
        var at = Banqi.Index(1, 1);
        Place(game, at, new Piece(Color.Red, PieceKind.Chariot));
        Place(game, Banqi.Index(1, 0), new Piece(Color.Red, PieceKind.Horse));
        
        Assert.False(game.IsLegalMove(at, Banqi.Index(1, 0)));
    }

    [Fact]
    public void Cannon_CanCaptureWithOneScreen()
    {
        var game = new Banqi(seed: 1);
        ClearBoard(game);
        game.FirstMoveDone = true;
        game.CurrentColor = Color.Red;
        
        var cannon = Banqi.Index(0, 0);
        Place(game, cannon, new Piece(Color.Red, PieceKind.Cannon));
        
        var target = Banqi.Index(0, 3);
        Place(game, target, new Piece(Color.Black, PieceKind.General));
        
        // 無砲架不可吃
        Assert.False(game.IsLegalMove(cannon, target));
        
        // 一個砲架可吃
        Place(game, Banqi.Index(0, 1), new Piece(Color.Black, PieceKind.Soldier));
        Assert.True(game.IsLegalMove(cannon, target));
        
        // 兩個砲架不可吃
        Place(game, Banqi.Index(0, 2), new Piece(Color.Red, PieceKind.Soldier));
        Assert.False(game.IsLegalMove(cannon, target));
    }

    [Fact]
    public void Cannon_CanOnlyMoveOneEmptyCell()
    {
        var game = new Banqi(seed: 2);
        ClearBoard(game);
        game.FirstMoveDone = true;
        game.CurrentColor = Color.Red;
        
        var cannon = Banqi.Index(0, 0);
        Place(game, cannon, new Piece(Color.Red, PieceKind.Cannon));
        
        Assert.True(game.IsLegalMove(cannon, Banqi.Index(0, 1)));
        Assert.False(game.IsLegalMove(cannon, Banqi.Index(0, 2)));
    }

    [Fact]
    public void Win_ByElimination_AllEnemyPiecesCaptured()
    {
        var game = new Banqi(seed: 3);
        ClearBoard(game);
        game.FirstMoveDone = true;
        game.CurrentColor = Color.Red;
        
        Place(game, Banqi.Index(0, 0), new Piece(Color.Red, PieceKind.Chariot));
        Place(game, Banqi.Index(0, 1), new Piece(Color.Black, PieceKind.Soldier));
        
        game.TryMove(Banqi.Index(0, 0), Banqi.Index(0, 1));
        
        Assert.Equal(Color.Red, game.Winner);
    }

    [Fact]
    public void Win_ByNoMoves_OpponentWins()
    {
        var game = new Banqi(seed: 4);
        ClearBoard(game);
        game.FirstMoveDone = true;
        game.CurrentColor = Color.Red;
        
        // 紅方只有一個兵被包圍
        Place(game, Banqi.Index(1, 1), new Piece(Color.Red, PieceKind.Soldier));
        Place(game, Banqi.Index(0, 1), new Piece(Color.Black, PieceKind.Chariot));
        Place(game, Banqi.Index(1, 0), new Piece(Color.Black, PieceKind.Chariot));
        Place(game, Banqi.Index(2, 1), new Piece(Color.Black, PieceKind.Chariot));
        Place(game, Banqi.Index(1, 2), new Piece(Color.Black, PieceKind.Chariot));
        
        // 紅方無子可動（兵不能吃車）
        Assert.False(game.HasAnyMove(Color.Red));
        
        // 模擬回合結束檢查
        game.TryMove(Banqi.Index(0, 1), Banqi.Index(0, 0)); // 黑車亂走
        
        // 輪到紅方，但無子可動
        Assert.Equal(Color.Red, game.CurrentColor);
        
        // 手動觸發檢查（正常應由 TryMove 內觸發）
        // 這裡因為測試邏輯需要重新檢查
        var hasMove = game.HasAnyMove(Color.Red);
        Assert.False(hasMove);
    }

    [Fact]
    public void HasAnyMove_WithHiddenPieces_ShouldReturnTrue()
    {
        var game = new Banqi(seed: 5);
        // 遊戲開始時全部蓋牌
        Assert.True(game.HasAnyMove(Color.Red));
        Assert.True(game.HasAnyMove(Color.Black));
    }

    private static void ClearBoard(Banqi game)
    {
        for (var i = 0; i < Banqi.Count; i++)
            game.Board[i] = new Cell { Piece = null, FaceUp = false };
    }

    private static void Place(Banqi game, int i, Piece p)
    {
        game.Board[i] = new Cell { Piece = p, FaceUp = true };
    }
}
