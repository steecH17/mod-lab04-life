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
        private bool IsAliveNext;
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

        public int CountLiveCells()
        {
            int count = 0;
            for (int x = 0; x < Columns; x++)
                for (int y = 0; y < Rows; y++)
                    if (Cells[x, y].IsAlive)
                        count++;
            return count;
        }

        public int CountCombinations()
        {
            bool[,] visited = new bool[Columns, Rows];
            int combinationCount = 0;

            for (int x = 0; x < Columns; x++)
            {
                for (int y = 0; y < Rows; y++)
                {
                    if (Cells[x, y].IsAlive && !visited[x, y])
                    {
                        BFS(x, y, visited);
                        combinationCount++;
                    }
                }
            }

            return combinationCount;
        }

        private void BFS(int startX, int startY, bool[,] visited)
        {
            Queue<(int x, int y)> queue = new Queue<(int x, int y)>();
            queue.Enqueue((startX, startY));
            visited[startX, startY] = true;

            while (queue.Count > 0)
            {
                var (x, y) = queue.Dequeue();

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;

                        int nx = x + dx;
                        int ny = y + dy;

                        if (nx >= 0 && nx < Columns && ny >= 0 && ny < Rows)
                        {
                            if (Cells[nx, ny].IsAlive && !visited[nx, ny])
                            {
                                visited[nx, ny] = true;
                                queue.Enqueue((nx, ny));
                            }
                        }
                    }
                }
            }
        }
    }

    public class DataStorage
    {
        public static void SaveState(string filePath, Board board)
        {
            using StreamWriter streamWriter = new StreamWriter(filePath);
            Cell[,] cells = board.Cells;

            for (int col = 0; col < board.Columns; col++)
            {
                for (int row = 0; row < board.Rows; row++)
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
        static Board board;
        static int generation = 0;
        static private void Reset(BoardProperty boardProperty)
        {
            board = new Board(
                boardProperty.BoardWidth,
                boardProperty.BoardHeight,
                boardProperty.BoardCellSize,
                boardProperty.LiveDensity);
        }
        static void Render()
        {
            for (int row = 0; row < board.Rows; row++)
            {
                for (int col = 0; col < board.Columns; col++)
                {
                    var cell = board.Cells[col, row];
                    if (cell.IsAlive)
                    {
                        Console.Write('*');
                    }
                    else
                    {
                        Console.Write(' ');
                    }
                }
                Console.Write('\n');
            }
            Console.WriteLine($"Generation:{generation}\n");
            Console.WriteLine($"Count live cells:{board.CountLiveCells()}\n");
            Console.WriteLine($"Count live cells:{board.CountCombinations()}\n");
        }

        static bool KeyPressHandler(string filePath)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(intercept: true);

                switch (key.Key)
                {
                    case ConsoleKey.S:
                        DataStorage.SaveState(filePath, board);
                        Console.WriteLine("File Have benn saved!!");
                        break;
                }

                return true;
            }
            else return false;
        }

        static void Main(string[] args)
        {
            string projectDirectory = Directory.GetCurrentDirectory();
            string solutionDirectory = Directory.GetParent(projectDirectory).Parent.Parent.FullName;
            string propertyPath = @"C:\Users\vniki\source\repos\mod-lab04-gameOfLife\mod-lab04-life\Life\Property.json";
            //string propertyPath = Path.Combine(solutionDirectory, "Property.json");
            string boardFilePath = @"C:\Users\vniki\source\repos\mod-lab04-gameOfLife\mod-lab04-life\Life\BoardData.txt";
            //string boardFilePath = Path.Combine(solutionDirectory, "BoardData.txt");
            BoardProperty boardProperty = JsonReader.PropertyReader(propertyPath);

            board = DataStorage.LoadState(boardFilePath);
            //Reset(boardProperty);
            while (true)
            {
                generation++;
                if (KeyPressHandler(boardFilePath))
                    break;
                Console.Clear();
                Render();
                board.Advance();
                Thread.Sleep(5000);

            }
        }
    }
}