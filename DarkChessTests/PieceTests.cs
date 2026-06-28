using DarkChess.Core;

namespace DarkChessTests;

public class PieceTests
{
    [Fact]
    public void FullSet_ShouldReturn32Pieces()
    {
        var set = Piece.FullSet();
        Assert.Equal(32, set.Count);
    }

    [Fact]
    public void FullSet_ShouldHave16RedAnd16Black()
    {
        var set = Piece.FullSet();
        Assert.Equal(16, set.Count(p => p.Color == Color.Red));
        Assert.Equal(16, set.Count(p => p.Color == Color.Black));
    }

    [Fact]
    public void FullSet_ShouldHave2Generals()
    {
        var set = Piece.FullSet();
        Assert.Equal(2, set.Count(p => p.Kind == PieceKind.General));
    }

    [Fact]
    public void FullSet_ShouldHave10Soldiers()
    {
        var set = Piece.FullSet();
        Assert.Equal(5, set.Count(p => p is { Kind: PieceKind.Soldier, Color: Color.Red }));
        Assert.Equal(5, set.Count(p => p is { Kind: PieceKind.Soldier, Color: Color.Black }));
    }

    [Fact]
    public void FullSet_ShouldHave4OfEachMidRank()
    {
        var set = Piece.FullSet();
        var midRanks = new[] { PieceKind.Advisor, PieceKind.Elephant, PieceKind.Chariot, PieceKind.Horse, PieceKind.Cannon };
        
        foreach (var kind in midRanks)
        {
            Assert.Equal(2, set.Count(p => p.Kind == kind && p.Color == Color.Red));
            Assert.Equal(2, set.Count(p => p.Kind == kind && p.Color == Color.Black));
        }
    }

    [Fact]
    public void CanCaptureByRank_SameRankShouldCapture()
    {
        var redGeneral = new Piece(Color.Red, PieceKind.General);
        var blackGeneral = new Piece(Color.Black, PieceKind.General);
        
        Assert.True(redGeneral.CanCaptureByRank(blackGeneral));
        Assert.True(blackGeneral.CanCaptureByRank(redGeneral));
    }

    [Fact]
    public void CanCaptureByRank_HigherShouldCaptureLoower()
    {
        var redGeneral = new Piece(Color.Red, PieceKind.General);
        var blackAdvisor = new Piece(Color.Black, PieceKind.Advisor);
        
        Assert.True(redGeneral.CanCaptureByRank(blackAdvisor));
    }

    [Fact]
    public void CanCaptureByRank_LowerShouldNotCaptureHigher()
    {
        var redChariot = new Piece(Color.Red, PieceKind.Chariot);
        var blackGeneral = new Piece(Color.Black, PieceKind.General);
        
        Assert.False(redChariot.CanCaptureByRank(blackGeneral));
    }

    [Fact]
    public void CanCaptureByRank_SoldierCanCaptureGeneral()
    {
        var redSoldier = new Piece(Color.Red, PieceKind.Soldier);
        var blackGeneral = new Piece(Color.Black, PieceKind.General);
        
        Assert.True(redSoldier.CanCaptureByRank(blackGeneral));
    }

    [Fact]
    public void CanCaptureByRank_GeneralCannotCaptureSoldier()
    {
        var redGeneral = new Piece(Color.Red, PieceKind.General);
        var blackSoldier = new Piece(Color.Black, PieceKind.Soldier);
        
        Assert.False(redGeneral.CanCaptureByRank(blackSoldier));
    }

    [Fact]
    public void CanCaptureByRank_SameColorCannotCapture()
    {
        var redGeneral = new Piece(Color.Red, PieceKind.General);
        var redSoldier = new Piece(Color.Red, PieceKind.Soldier);
        
        Assert.False(redGeneral.CanCaptureByRank(redSoldier));
    }

    [Fact]
    public void Glyph_ShouldReturnCorrectChinese()
    {
        Assert.Equal("帥", new Piece(Color.Red, PieceKind.General).Glyph);
        Assert.Equal("將", new Piece(Color.Black, PieceKind.General).Glyph);
        Assert.Equal("兵", new Piece(Color.Red, PieceKind.Soldier).Glyph);
        Assert.Equal("卒", new Piece(Color.Black, PieceKind.Soldier).Glyph);
        Assert.Equal("炮", new Piece(Color.Red, PieceKind.Cannon).Glyph);
        Assert.Equal("包", new Piece(Color.Black, PieceKind.Cannon).Glyph);
    }
}
