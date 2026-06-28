using DarkChess.Core;

namespace DarkChessTests;

public class BotPlayerTests
{
    [Fact]
    public void ChooseAction_ShouldReturnLegalAction()
    {
        var game = new Banqi(seed: 1);
        var bot = new BotPlayer(Color.Red, seed: 1);
        
        var action = bot.ChooseAction(game);
        
        Assert.Equal(ActionKind.Flip, action.Kind);
        Assert.InRange(action.From, 0, Banqi.Count - 1);
    }

    [Fact]
    public void ChooseAction_FirstFlip_ShouldChooseFlipAction()
    {
        var game = new Banqi(seed: 2);
        var bot = new BotPlayer(Color.Black, seed: 2);
        
        var action = bot.ChooseAction(game);
        
        Assert.Equal(ActionKind.Flip, action.Kind);
    }

    [Fact]
    public void ChooseAction_WithMovesAvailable_ShouldChooseMoveOrFlip()
    {
        var game = new Banqi(seed: 3);
        ClearBoard(game);
        game.FirstMoveDone = true;
        game.CurrentColor = Color.Red;
        
        // 給紅方一個可移動的棋子
        Place(game, Banqi.Index(1, 1), new Piece(Color.Red, PieceKind.Chariot));
        
        // 加些黑子
        Place(game, Banqi.Index(0, 0), new Piece(Color.Black, PieceKind.Soldier));
        
        var bot = new BotPlayer(Color.Red, searchDepth: 2, seed: 3);
        var action = bot.ChooseAction(game);
        
        // 應該回傳合法行動（移動或翻棋，因為還有蓋牌）
        Assert.True(action.Kind == ActionKind.Move || action.Kind == ActionKind.Flip);
    }

    [Fact]
    public void ChooseAction_ShouldNotChooseIllegalMove()
    {
        var game = new Banqi(seed: 4);
        ClearBoard(game);
        game.FirstMoveDone = true;
        game.CurrentColor = Color.Red;
        
        // 紅方車被包圍，只能往一個方向走
        Place(game, Banqi.Index(1, 1), new Piece(Color.Red, PieceKind.Chariot));
        Place(game, Banqi.Index(0, 1), new Piece(Color.Black, PieceKind.General));
        Place(game, Banqi.Index(1, 0), new Piece(Color.Black, PieceKind.General));
        Place(game, Banqi.Index(2, 1), new Piece(Color.Black, PieceKind.General));
        // (1,2) 空著
        
        var bot = new BotPlayer(Color.Red, searchDepth: 1, seed: 4);
        var action = bot.ChooseAction(game);
        
        // 應該選擇移動到 (1,2) 或翻棋
        if (action.Kind == ActionKind.Move)
        {
            Assert.Equal(Banqi.Index(1, 1), action.From);
            Assert.Equal(Banqi.Index(1, 2), action.To);
        }
        else
        {
            Assert.Equal(ActionKind.Flip, action.Kind);
        }
    }

    [Fact]
    public void ChooseAction_WithCaptureAvailable_ShouldChooseMoveAction()
    {
        var game = new Banqi(seed: 5);
        ClearBoard(game);
        game.FirstMoveDone = true;
        game.CurrentColor = Color.Red;
        
        // 紅車可吃黑將
        Place(game, Banqi.Index(1, 1), new Piece(Color.Red, PieceKind.Chariot));
        Place(game, Banqi.Index(1, 2), new Piece(Color.Black, PieceKind.General));
        // 加一些其他已翻開的子
        Place(game, Banqi.Index(3, 0), new Piece(Color.Black, PieceKind.Soldier));
        Place(game, Banqi.Index(3, 7), new Piece(Color.Red, PieceKind.Soldier));
        
        var bot = new BotPlayer(Color.Red, searchDepth: 2, seed: 5);
        var action = bot.ChooseAction(game);
        
        // 應該選擇移動行動（因為可以吃高價值目標）
        Assert.Equal(ActionKind.Move, action.Kind);
        
        // 驗證移動是合法的
        var legalMoves = game.LegalMovesFrom(action.From);
        Assert.Contains(legalMoves, m => m.To == action.To);
    }

    [Fact]
    public void ChooseAction_MultipleBotsPlayingDoesNotCrash()
    {
        var game = new Banqi(seed: 6);
        var redBot = new BotPlayer(Color.Red, searchDepth: 2, seed: 6);
        var blackBot = new BotPlayer(Color.Black, searchDepth: 2, seed: 7);
        
        // 模擬幾回合
        for (var i = 0; i < 10 && game.Winner is null; i++)
        {
            var currentBot = game.CurrentColor == Color.Red ? redBot :
                             game.CurrentColor == Color.Black ? blackBot : redBot;
            
            var action = currentBot.ChooseAction(game);
            
            if (action.Kind == ActionKind.Flip)
                game.Flip(action.From);
            else
                game.TryMove(action.From, action.To);
        }
        
        // 不應該崩潰，且至少執行了一些回合
        Assert.True(game.FirstMoveDone);
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
