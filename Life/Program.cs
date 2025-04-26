using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Dynamic;
using System.IO;
using System.Text.Json;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;

namespace cli_life
{
    public class JsonReader
    {
        public static BoardProperty PropertyReader(string filePath)
            => JsonSerializer.Deserialize<BoardProperty>(File.ReadAllText(filePath));
    }

    public class BoardProperty
    {
        public int BoardWidth { get; set; }
        public int BoardHeight { get; set; }
        public int BoardCellSize { get; set; }
        public double LiveDensity { get; set; }

        public BoardProperty(int boardWidth, int boardHeight, int boardCellSize, double liveDensity)
        {
            this.BoardWidth = boardWidth;
            this.BoardHeight = boardHeight;
            this.BoardCellSize = boardCellSize;
            this.LiveDensity = liveDensity;
        }
    }

    public class Cell
    {
        public bool IsAlive;
        public readonly List<Cell> neighbors = new List<Cell>();
        public bool IsAliveNext;
        public void DetermineNextLiveState()
        {
            int liveNeighbors = neighbors.Where(x => x.IsAlive).Count();
            if (IsAlive)
                IsAliveNext = liveNeighbors == 2 || liveNeighbors == 3;
            else
                IsAliveNext = liveNeighbors == 3;
        }
        public void Advance()
        {
            IsAlive = IsAliveNext;
        }
    }
    public class Board
    {
        public readonly Cell[,] Cells;
        public readonly int CellSize;
        public int generation;
        public int Columns { get { return Cells.GetLength(0); } }
        public int Rows { get { return Cells.GetLength(1); } }
        public int Width { get { return Columns * CellSize; } }
        public int Height { get { return Rows * CellSize; } }

        public Board(int width, int height, int cellSize, double liveDensity = .1)
        {
            CellSize = cellSize;

            Cells = new Cell[width / cellSize, height / cellSize];
            for (int x = 0; x < Columns; x++)
                for (int y = 0; y < Rows; y++)
                    Cells[x, y] = new Cell();

            generation = 0;

            ConnectNeighbors();
            Randomize(liveDensity);
        }

        public Board(int width, int height, int cellSize)
        {
            CellSize = cellSize;

            Cells = new Cell[width / cellSize, height / cellSize];
            for (int x = 0; x < Columns; x++)
                for (int y = 0; y < Rows; y++)
                    Cells[x, y] = new Cell();

            generation = 0;

            ConnectNeighbors();
        }

        readonly Random rand = new Random();
        public void Randomize(double liveDensity)
        {
            foreach (var cell in Cells)
                cell.IsAlive = rand.NextDouble() < liveDensity;
        }

        public void Advance()
        {
            generation++;

            foreach (var cell in Cells)
                cell.DetermineNextLiveState();
            foreach (var cell in Cells)
                cell.Advance();
        }
        private void ConnectNeighbors()
        {
            for (int x = 0; x < Columns; x++)
            {
                for (int y = 0; y < Rows; y++)
                {
                    int xL = (x > 0) ? x - 1 : Columns - 1;
                    int xR = (x < Columns - 1) ? x + 1 : 0;

                    int yT = (y > 0) ? y - 1 : Rows - 1;
                    int yB = (y < Rows - 1) ? y + 1 : 0;

                    Cells[x, y].neighbors.Add(Cells[xL, yT]);
                    Cells[x, y].neighbors.Add(Cells[x, yT]);
                    Cells[x, y].neighbors.Add(Cells[xR, yT]);
                    Cells[x, y].neighbors.Add(Cells[xL, y]);
                    Cells[x, y].neighbors.Add(Cells[xR, y]);
                    Cells[x, y].neighbors.Add(Cells[xL, yB]);
                    Cells[x, y].neighbors.Add(Cells[x, yB]);
                    Cells[x, y].neighbors.Add(Cells[xR, yB]);
                }
            }
        }

    }

    public class Pattern
    {
        public string Name { get; set; }
        public bool[,] Structure { get; set; }
        public int Size { get; set; }

        public Pattern(string name, bool[,] structure)
        {
            Name = name;
            Structure = structure;
            Size = structure.GetLength(0); // Предполагаем квадратные шаблоны
        }
    }
    public class BoardAnalysis
    {
        public Board Board { get; set; }
        public int GetCountLiveCells {get;set;}
        public int GetCountCombinations {get;set;}
        public int timeStable;
        public readonly List<Pattern> KnownPatterns = new List<Pattern>();
        int periodStablePhase;
        int currentTimeStable;
        public BoardAnalysis(Board board, string dirName)
        {
            this.Board = board;
            KnownPatterns = GetPatterns(dirName);
            periodStablePhase = 20;
            currentTimeStable = 0;
            GetCountLiveCells = CountLiveCells();
        }

        private List<Pattern> GetPatterns(string dirName)
        {
            List<Pattern> patterns = new List<Pattern>();
            string[] filenames = Directory.GetFiles(dirName);
            foreach (var filename in filenames)
            {
                string[] lines = File.ReadAllLines(filename);
                bool[,] templates = new bool[lines.Length, lines[0].Length];

                for (int i = 0; i < templates.GetLength(0); i++)
                {
                    for (int j = 0; j < templates.GetLength(1); j++)
                    {
                        if (lines[i][j] == '1') templates[i, j] = true;
                        else templates[i, j] = false;
                    }
                }

                patterns.Add(new Pattern(Path.GetFileNameWithoutExtension(filename), templates));
            }

            return patterns;
        }

        public bool IsStable()
        {
            if (GetCountLiveCells == CountLiveCells())
                currentTimeStable++;
            else
                currentTimeStable = 0;

            if (currentTimeStable >= periodStablePhase)
            {
                timeStable = currentTimeStable == periodStablePhase ? Board.generation : timeStable;
                return true;
            }

            GetCountLiveCells = CountLiveCells();

            return false;
        }

        public int CountLiveCells()
        {
            int count = 0;
            for (int x = 0; x < Board.Columns; x++)
                for (int y = 0; y < Board.Rows; y++)
                    if (Board.Cells[x, y].IsAlive)
                        count++;

            return count;
        }

        public Dictionary<string, int> ClassifyPatterns()
        {
            var patternCounts = new Dictionary<string, int>();
            bool[,] visited = new bool[Board.Columns, Board.Rows];

            // Инициализация словаря
            foreach (var pattern in KnownPatterns)
            {
                patternCounts[pattern.Name] = 0;
            }
            patternCounts["Unknown"] = 0;

            for (int x = 0; x < Board.Columns; x++)
            {
                for (int y = 0; y < Board.Rows; y++)
                {
                    if (Board.Cells[x, y].IsAlive && !visited[x, y])
                    {
                        var group = ExtractGroup(x, y, visited);
                        bool isPatternFound = false;

                        foreach (var pattern in KnownPatterns)
                        {
                            if (IsPatternMatch(group, pattern))
                            {
                                patternCounts[pattern.Name]++;
                                isPatternFound = true;
                                break;
                            }
                        }

                        if (!isPatternFound && group.Count > 1) // Не учитываем одиночные клетки
                        {
                            patternCounts["Unknown"]++;
                        }
                    }
                }
            }

            return patternCounts;
        }

        // Извлекает группу связанных живых клеток
        private List<(int x, int y)> ExtractGroup(int startX, int startY, bool[,] visited)
        {
            var group = new List<(int x, int y)>();
            var queue = new Queue<(int x, int y)>();
            queue.Enqueue((startX, startY));
            visited[startX, startY] = true;

            while (queue.Count > 0)
            {
                var (x, y) = queue.Dequeue();
                group.Add((x, y));

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;

                        int nx = x + dx;
                        int ny = y + dy;

                        if (nx >= 0 && nx < Board.Columns && ny >= 0 && ny < Board.Rows)
                        {
                            if (Board.Cells[nx, ny].IsAlive && !visited[nx, ny])
                            {
                                visited[nx, ny] = true;
                                queue.Enqueue((nx, ny));
                            }
                        }
                    }
                }
            }

            return group;
        }

        // Проверяет, соответствует ли группа шаблону (с учётом поворотов и отражений)
        private bool IsPatternMatch(List<(int x, int y)> group, Pattern pattern)
        {
            if (group.Count != pattern.Structure.Cast<bool>().Count(c => c))
                return false;

            // Находим границы группы
            int minX = group.Min(c => c.x);
            int maxX = group.Max(c => c.x);
            int minY = group.Min(c => c.y);
            int maxY = group.Max(c => c.y);

            int groupWidth = maxX - minX + 1;
            int groupHeight = maxY - minY + 1;

            int patternRows = pattern.Structure.GetLength(0);
            int patternCols = pattern.Structure.GetLength(1);

            // Группа должна помещаться в шаблон после поворота/отражения
            if ((groupWidth > patternRows && groupWidth > patternCols) ||
                (groupHeight > patternRows && groupHeight > patternCols))
                return false;

            // Проверяем все возможные повороты и отражения шаблона
            for (int rotation = 0; rotation < 4; rotation++)
            {
                var rotatedPattern = RotatePattern(pattern.Structure, rotation);
                if (CheckPatternMatch(group, rotatedPattern, minX, minY))
                    return true;

                var mirroredPattern = MirrorPattern(rotatedPattern);
                if (CheckPatternMatch(group, mirroredPattern, minX, minY))
                    return true;
            }

            return false;
        }

        private bool CheckPatternMatch(List<(int x, int y)> group, bool[,] pattern, int offsetX, int offsetY)
        {
            int patternRows = pattern.GetLength(0);
            int patternCols = pattern.GetLength(1);

            // Проверяем, что группа точно соответствует шаблону
            for (int i = 0; i < patternRows; i++)
            {
                for (int j = 0; j < patternCols; j++)
                {
                    bool expected = pattern[i, j];
                    int x = offsetX + i;
                    int y = offsetY + j;

                    bool actual = group.Contains((x, y));

                    if (expected != actual)
                        return false;
                }
            }

            return true;
        }

        // Поворачивает шаблон на 90° * rotation
        private bool[,] RotatePattern(bool[,] pattern, int rotation)
        {
            int rows = pattern.GetLength(0);
            int cols = pattern.GetLength(1);

            // При повороте на 90° или 270° размеры меняются местами
            int newRows = (rotation % 2 == 1) ? cols : rows;
            int newCols = (rotation % 2 == 1) ? rows : cols;

            var result = new bool[newRows, newCols];

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    switch (rotation % 4)
                    {
                        case 0: // Без поворота
                            result[i, j] = pattern[i, j];
                            break;
                        case 1: // Поворот на 90° по часовой
                            result[j, newCols - 1 - i] = pattern[i, j];
                            break;
                        case 2: // Поворот на 180°
                            result[newRows - 1 - i, newCols - 1 - j] = pattern[i, j];
                            break;
                        case 3: // Поворот на 270° по часовой (или 90° против)
                            result[newRows - 1 - j, i] = pattern[i, j];
                            break;
                    }
                }
            }

            return result;
        }

        // Отражает шаблон по горизонтали
        private bool[,] MirrorPattern(bool[,] pattern)
        {
            int rows = pattern.GetLength(0);
            int cols = pattern.GetLength(1);
            var result = new bool[rows, cols];

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    result[i, cols - 1 - j] = pattern[i, j];
                }
            }

            return result;
        }

    }

    public class DataStorage
    {
        public static void SaveState(string filePath, Board board)
        {
            using StreamWriter streamWriter = new StreamWriter(filePath);
            Cell[,] cells = board.Cells;

            for (int row = 0; row < board.Rows; row++)
            {
                for (int col = 0; col < board.Columns; col++)
                {
                    streamWriter.Write(cells[col, row].IsAlive ? '1' : '0');
                }
                streamWriter.Write('\n');
            }
        }

        public static Board LoadState(string filePath)
        {
            string[] lines = File.ReadAllLines(filePath);
            int width = lines[0].Length;
            int height = lines.Length;
            Board board = new Board(width, height, 1);

            for (int row = 0; row < lines.Length; row++)
            {
                for (int col = 0; col < lines[row].Length; col++)
                {
                    board.Cells[col, row].IsAlive = lines[row][col] == '1';
                }
            }

            return board;
        }
    }
    class Program
    {
        static bool isPause = false;
        static Board board;
        static BoardAnalysis boardAnalysis;
        static BoardProperty boardProperty;
        static int generation = 0;

        static private void Reset(BoardProperty boardProperty, string dirName)
        {
            board = new Board(
                boardProperty.BoardWidth,
                boardProperty.BoardHeight,
                boardProperty.BoardCellSize,
                boardProperty.LiveDensity);

            boardAnalysis = new BoardAnalysis(board, dirName);
        }
        static void Render()
        {
            StringBuilder stringBuilder = new StringBuilder();

            for (int row = 0; row < board.Rows; row++)
            {
                for (int col = 0; col < board.Columns; col++)
                {
                    var cell = board.Cells[col, row];
                    if (cell.IsAlive)
                    {
                        stringBuilder.Append('*');
                        //Console.Write("*");
                    }
                    else
                    {
                        stringBuilder.Append(' ');
                        //Console.Write(" ");
                    }
                }
                stringBuilder.Append('\n');
                //Console.Write('\n');
            }
            stringBuilder.Append(GetInfoString());

            Console.Write(stringBuilder);
        }

        static string GetInfoString()
        {
            StringBuilder builder = new StringBuilder();
            int totalCombinations = 0;

            builder.Append($"Generation:{generation}\n");
            builder.Append($"Count live cells:{boardAnalysis.CountLiveCells()}\n");

            if (boardAnalysis.IsStable())
            {
                builder.Append($"System is Stable on generation : {boardAnalysis.timeStable}!!!\n");

                var patternCounts = boardAnalysis.ClassifyPatterns();

                foreach (var entry in patternCounts)
                {
                    builder.Append($"{entry.Key}: {entry.Value}\n");
                    if(entry.Value > 0)
                        totalCombinations+= entry.Value;
                }
                builder.Append($"Total combinations:{totalCombinations}\n");
            }

            return builder.ToString();
        }

        static bool? KeyPressHandler(string filePath)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(intercept: true);

                switch (key.Key)
                {
                    case ConsoleKey.S:
                        DataStorage.SaveState(filePath, board);
                        Console.WriteLine("File Have been saved!!!");
                        break;
                    case ConsoleKey.P:
                        isPause = !isPause;
                        break;
                    case ConsoleKey.Escape:
                        return null;
                }

                return false;
            }
            else return false;
        }

        static void RunGame(string filePath)
        {

            while (true)
            {

                if (KeyPressHandler(filePath) == null)
                    break;
                if (KeyPressHandler(filePath) == true)
                    break;

                if (!isPause)
                {
                    generation++;
                    Console.Clear();
                    Render();
                    board.Advance();
                    boardAnalysis.Board = board;
                    Thread.Sleep(100);
                }

            }
        }

        static void StabilityAnalyzer(string outputFilePath, double densityStep)
        {
            StringBuilder stringBuilder = new StringBuilder();

            string dirName = Path.Combine(Environment.CurrentDirectory, "pattern/");

            for(decimal density = 0.05m; density <= 1.0m; density+=(decimal)densityStep)
            {
                board = new Board(
                boardProperty.BoardWidth,
                boardProperty.BoardHeight,
                boardProperty.BoardCellSize,
                (double)density);

                boardAnalysis = new BoardAnalysis(board, dirName);

                for (int gen = 0; gen < 10000; gen++)
                {
                    if(boardAnalysis.IsStable())
                    {
                        stringBuilder.Append($"{(double)density} {boardAnalysis.timeStable}\n");
                        break;
                    }
                    board.Advance();
                }
            }

            File.WriteAllText(outputFilePath, stringBuilder.ToString());

        }

        static void Main(string[] args)
        {
            // string propertyPath = @"C:\Users\vniki\source\repos\mod-lab04-gameOfLife\mod-lab04-life\Life\Property.json";
            // string boardFilePath = @"C:\Users\vniki\source\repos\mod-lab04-gameOfLife\mod-lab04-life\Life\BoardData.txt";
            string projectDirectory = Environment.CurrentDirectory;
            string propertyPath = Path.Combine(projectDirectory, "Property.json");
            string boardFilePath = Path.Combine(projectDirectory, "BoardData.txt");
            string dirName = Path.Combine(Environment.CurrentDirectory, "pattern/");
            string dataPlotPath = Path.Combine(Environment.CurrentDirectory, "data.txt");
            boardProperty = JsonReader.PropertyReader(propertyPath);

            //board = DataStorage.LoadState(filePath);
            Reset(boardProperty, dirName);
            RunGame(boardFilePath);

            // StabilityAnalyzer(dataPlotPath, 0.05d);
        }
    }
}