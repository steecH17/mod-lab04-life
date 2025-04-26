using Xunit;
using cli_life;
using System.Text.Json;

namespace TestLife;

public class CellTests
{
    [Fact]
    public void Cell_StaysAlive_With2or3Neighbors()
    {
        var cell = new Cell { IsAlive = true };
        cell.neighbors.AddRange(new[] { new Cell { IsAlive = true }, new Cell { IsAlive = true } });
        cell.DetermineNextLiveState();
        Assert.True(cell.IsAliveNext);
    }

    [Fact]
    public void Cell_Dies_WithLessThan2Neighbors()
    {
        var cell = new Cell { IsAlive = true };
        cell.neighbors.Add(new Cell { IsAlive = true });
        cell.DetermineNextLiveState();
        Assert.False(cell.IsAliveNext);
    }

    [Fact]
    public void Cell_BecomesAlive_WithExactly3Neighbors()
    {
        var cell = new Cell { IsAlive = false };
        cell.neighbors.AddRange(new[] { new Cell { IsAlive = true }, new Cell { IsAlive = true }, new Cell { IsAlive = true } });
        cell.DetermineNextLiveState();
        Assert.True(cell.IsAliveNext);
    }

    [Fact]
    public void Cell_Advance_CorrectlyUpdatesIsAlive()
    {
        // Arrange
        var cell = new Cell { IsAlive = false };
        cell.IsAliveNext = true; // Имитируем расчет следующего состояния

        // Act
        cell.Advance();

        // Assert
        Assert.True(cell.IsAlive); // Состояние должно обновиться
    }

    [Fact]
    public void Cell_WithNoNeighbors_Dies_IfAlive()
    {
        var cell = new Cell { IsAlive = true };
        cell.DetermineNextLiveState();
        Assert.False(cell.IsAliveNext);
    }
}

public class BoardTests
{
    [Fact]
    public void Board_Initializes_CorrectDimensions()
    {
        var board = new Board(100, 50, 10);
        Assert.Equal(10, board.Columns);
        Assert.Equal(5, board.Rows);
    }

    [Fact]
    public void Board_ConnectsNeighbors_Correctly()
    {
        var board = new Board(30, 30, 10);
        var cell = board.Cells[1, 1];
        Assert.Equal(8, cell.neighbors.Count);
    }

    [Fact]
    public void Board_Advances_GenerationCounter()
    {
        var board = new Board(10, 10, 1);
        board.Advance();
        Assert.Equal(1, board.generation);
    }
}

public class PatternTest
{
    [Fact]
    public void Pattern_Initializes_WithCorrectSize()
    {
        var pattern = new Pattern("test", new bool[2, 3]);
        Assert.Equal(2, pattern.Size);
    }
}

public class JsonReaderTests
{
    [Fact]
    public void JsonReader_ReadsBoardProperty_Correctly()
    {
        string projectDirectory = Environment.CurrentDirectory;
        string propertyPath = Path.Combine(projectDirectory, "test_property.json");
        var expected = new BoardProperty(60, 20, 1, 0.4);
        File.WriteAllText(propertyPath, JsonSerializer.Serialize(expected));
        
        var actual = JsonReader.PropertyReader(propertyPath);
        
        Assert.Equal(expected.BoardWidth, actual.BoardWidth);
        Assert.Equal(expected.BoardHeight, actual.BoardHeight);
        Assert.Equal(expected.BoardCellSize, actual.BoardCellSize);
        Assert.Equal(expected.LiveDensity, actual.LiveDensity);
        File.Delete(propertyPath);
    }
}

public class DataStorageTest
{
     [Fact]
    public void DataStorage_SavesAndLoads_BoardState()
    {
        var board = new Board(3, 3, 1);
        board.Cells[0, 0].IsAlive = true;
        board.Cells[1, 1].IsAlive = true;
        
        string testFile = Path.Combine(Environment.CurrentDirectory, "data_test.txt");
        DataStorage.SaveState(testFile, board);
        var loadedBoard = DataStorage.LoadState(testFile);
        
        Assert.True(loadedBoard.Cells[0, 0].IsAlive);
        Assert.True(loadedBoard.Cells[1, 1].IsAlive);
        File.Delete(testFile);
    }
}

public class BoardAnalysisTests
{
    readonly string dirName = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "Life", "pattern");

    [Fact]
    public void BoardAnalysis_GetPatterns_Correctly()
    {
        var board = new Board(10, 10, 1);
        var analysis = new BoardAnalysis(board, dirName);
        var patterns = analysis.KnownPatterns;

        Assert.Equal(8, patterns.Count);
        Assert.Contains(patterns, p => p.Name == "blinker");
        Assert.Contains(patterns, p => p.Name == "block");
        Assert.Contains(patterns, p => p.Name == "boat");
        Assert.Contains(patterns, p => p.Name == "bond");
        Assert.Contains(patterns, p => p.Name == "box");
        Assert.Contains(patterns, p => p.Name == "glider");
        Assert.Contains(patterns, p => p.Name == "hive");
        Assert.Contains(patterns, p => p.Name == "ship");
    }

    [Fact]
    public void BoardAnalysis_CountsLiveCells_Correctly()
    {
        var board = new Board(3, 3, 1);
        board.Cells[0, 0].IsAlive = true;
        board.Cells[1, 1].IsAlive = true;
        var analysis = new BoardAnalysis(board, dirName);
        Assert.Equal(2, analysis.CountLiveCells());
    }

    [Fact]
    public void BoardAnalysis_DetectsStability()
    {
        var board = new Board(3, 3, 1);
        var analysis = new BoardAnalysis(board, dirName);
        
        for (int i = 0; i < 19; i++)
        {
            Assert.False(analysis.IsStable());
            board.Advance();
        }
        
        Assert.True(analysis.IsStable());
    }

    [Fact]
    public void BoardAnalysis_CountsLiveCells_InEmptyBoard()
    {
        var board = new Board(5, 5, 1);
        var analysis = new BoardAnalysis(board, dirName);
        
        var count = analysis.CountLiveCells();
        
        Assert.Equal(0, count);
    }

    [Fact]
    public void BoardAnalysis_CountsLiveCells_InFullBoard()
    {
        var board = new Board(3, 3, 1);
        foreach (var cell in board.Cells) cell.IsAlive = true;
        var analysis = new BoardAnalysis(board, dirName);
        
        var count = analysis.CountLiveCells();
        
        Assert.Equal(9, count);
    }

    [Fact]
    public void BoardAnalysis_CountsCombinations_Correctly()
    {
        // Arrange
        var board = new Board(5, 5, 1);
        
        // block
        board.Cells[0, 0].IsAlive = true;
        board.Cells[0, 1].IsAlive = true;
        board.Cells[1, 0].IsAlive = true;
        board.Cells[1, 1].IsAlive = true;
        
        // unknow combination
        board.Cells[3, 1].IsAlive = true;
        board.Cells[4, 1].IsAlive = true;

        //blinker
        board.Cells[1, 4].IsAlive = true;
        board.Cells[2, 4].IsAlive = true;
        board.Cells[3, 4].IsAlive = true;
        
        var analysis = new BoardAnalysis(board, dirName);
        
        // Act
        var combinations = analysis.ClassifyPatterns();
        
        // Assert
        Assert.Equal(1, combinations["block"]); // Блок не распознан, но считается как комбинация
        Assert.Equal(1, combinations["Unknown"]); // Блок не распознан, но считается как комбинация
        Assert.Equal(1, combinations["blinker"]); // Блок не распознан, но считается как комбинация
        Assert.Equal(3, combinations.Values.Sum()); // Всего живых клеток
    }
}


