using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Win32;

namespace FifteenPuzzle
{

    public class ScaleDifficultyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
            {
                int scaled = (int)Math.Round(d / 10.0);
                if (scaled < 1)
                    scaled = 1;

                return scaled.ToString("D3");
            }

            return "001";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public partial class MainWindow : Window
    {
        private Button[,] buttons = new Button[4, 4];
        private int[,] board = new int[4, 4];
        private int stepCount = 0;
        private bool isGameStarted = false;
        private bool isGameFinished = false;
        private Random random = new Random();
        private (int row, int col) emptyCell = (3, 3);

        private void SetControlsEnabled(bool isEnabled)
        {
            GenerateButton.IsEnabled = isEnabled;
            SolveButton.IsEnabled = isEnabled;
            OpenButton.IsEnabled = isEnabled;
            SaveButton.IsEnabled = isEnabled;
            DifficultySlider.IsEnabled = isEnabled;
        }

        public MainWindow()
        {
            InitializeComponent();
            InitializeBoard();
        }

        private void InitializeBoard()
        {
            board = new int[4, 4]
            {
                { 1,  2,  3,  4 },
                { 5,  6,  7,  8 },
                { 9, 10, 11, 12 },
                {13, 14, 15,  0 }
            };
            emptyCell = (3, 3);

            if (PuzzleGrid.Children.Count == 0)
            {
                for (int i = 0; i < 4; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        Button btn = new Button
                        {
                            Style = (Style)this.FindResource("TileStyle"),
                            Width = 80,
                            Height = 80,
                            Tag = (i, j)
                        };

                        btn.Click += Tile_Click;
                        buttons[i, j] = btn;
                        PuzzleGrid.Children.Add(btn);
                    }
                }
            }

            DrawBoard();
        }

        private void DrawBoard()
        {
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    buttons[i, j].Content = board[i, j] == 0 ? "" : board[i, j].ToString();
                }
            }
        }
        private bool IsSolved()
        {
            int expected = 1;

            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    if (board[i, j] == 0)
                        continue;

                    if (board[i, j] != expected)
                        return false;

                    expected++;
                }
            }

            return true;
        }

        private async void Tile_Click(object sender, RoutedEventArgs e)
        {
            if (!isGameStarted || isGameFinished)
                return;

            Button clickedButton = sender as Button;
            (int i, int j) = ((int, int))clickedButton.Tag;
            (int emptyRow, int emptyCol) = emptyCell;

            if ((Math.Abs(i - emptyRow) == 1 && j == emptyCol) || (Math.Abs(j - emptyCol) == 1 && i == emptyRow))
            {
                Button tile = buttons[i, j];
                double tileSize = tile.Width;
                double spacing = 8;
                double dx = (emptyCol - j) * (tileSize + spacing);
                double dy = (emptyRow - i) * (tileSize + spacing);

                TranslateTransform transform = new TranslateTransform();
                tile.RenderTransform = transform;

                var animX = new DoubleAnimation(0, dx, TimeSpan.FromMilliseconds(150));
                var animY = new DoubleAnimation(0, dy, TimeSpan.FromMilliseconds(150));

                transform.BeginAnimation(TranslateTransform.XProperty, animX);
                transform.BeginAnimation(TranslateTransform.YProperty, animY);

                await Task.Delay(160);

                board[emptyRow, emptyCol] = board[i, j];
                board[i, j] = 0;

                buttons[emptyRow, emptyCol].Content = tile.Content;
                tile.Content = "";
                tile.RenderTransform = null;

                emptyCell = (i, j);
                stepCount++;
                UpdateStepCounter();

                if (IsSolved())
                {
                    isGameFinished = true;
                    MessageBox.Show("Puzzle solved!", "Congratulations", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }



        private async void Generate_Click(object sender, RoutedEventArgs e)
        {
            SetControlsEnabled(false);

            isGameFinished = false;
            InitializeBoard();
            int shuffleMoves = (int)DifficultySlider.Value;
            await AnimateShuffle(shuffleMoves);
            stepCount = 0;
            UpdateStepCounter();
            isGameStarted = true;

            SetControlsEnabled(true);
        }
        private async void Solve_Click(object sender, RoutedEventArgs e)
        {
            if (isGameFinished || !isGameStarted)
                return;

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            SetControlsEnabled(false);
            var solver = new PuzzleSolver(board, emptyCell);
            var moves = solver.Solve();

            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            stopwatch.Stop();

            foreach (var move in moves)
            {
                await AnimateTileMove(move.row, move.col);
                await Task.Delay(100);
            }

            isGameFinished = true;
            SetControlsEnabled(true);
            MessageBox.Show($"Solved automatically in {moves.Count} moves.\nTime: {stopwatch.ElapsedMilliseconds} ms",
                            "Solver", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string[] lines = File.ReadAllLines(openFileDialog.FileName);

                    for (int i = 0; i < 4; i++)
                    {
                        string[] parts = lines[i].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        for (int j = 0; j < 4; j++)
                        {
                            board[i, j] = int.Parse(parts[j]);
                            if (board[i, j] == 0)
                                emptyCell = (i, j);
                        }
                    }

                    stepCount = int.Parse(lines[4]);
                    UpdateStepCounter();
                    DrawBoard();

                    isGameStarted = true;
                    isGameFinished = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error while reading the file:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    using (StreamWriter writer = new StreamWriter(saveFileDialog.FileName))
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            for (int j = 0; j < 4; j++)
                            {
                                writer.Write(board[i, j] + " ");
                            }
                            writer.WriteLine();
                        }

                        writer.WriteLine(stepCount);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error while saving the file:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void UpdateStepCounter()
        {
            StepCounterText.Text = stepCount.ToString();
        }
        private async Task AnimateShuffle(int moves)
        {
            int zeroRow = 3;
            int zeroCol = 3;

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            for (int i = 0; i < moves; i++)
            {
                List<(int dx, int dy)> directions = new List<(int, int)>
                {
                    (-1, 0),
                    (1, 0),
                    (0, -1),
                    (0, 1)
                };

                var validMoves = directions.FindAll(d =>
                {
                    int newRow = zeroRow + d.dx;
                    int newCol = zeroCol + d.dy;
                    return newRow >= 0 && newRow < 4 && newCol >= 0 && newCol < 4;
                });

                var move = validMoves[random.Next(validMoves.Count)];
                int newRowValid = zeroRow + move.dx;
                int newColValid = zeroCol + move.dy;

                (board[zeroRow, zeroCol], board[newRowValid, newColValid]) =
                    (board[newRowValid, newColValid], board[zeroRow, zeroCol]);

                zeroRow = newRowValid;
                zeroCol = newColValid;

                DrawBoard();

                double targetTime = ((i + 1) * 3000.0) / moves;
                double timeToWait = targetTime - stopwatch.Elapsed.TotalMilliseconds;

                if (timeToWait > 0)
                    await Task.Delay((int)timeToWait);
            }

            emptyCell = (zeroRow, zeroCol);
        }

        private async Task AnimateTileMove(int row, int col)
        {
            var tile = buttons[row, col];
            double tileSize = tile.Width;
            double spacing = 8;

            int dx = emptyCell.col - col;
            int dy = emptyCell.row - row;

            double animX = dx * (tileSize + spacing);
            double animY = dy * (tileSize + spacing);

            TranslateTransform transform = new TranslateTransform();
            tile.RenderTransform = transform;

            var animXDef = new DoubleAnimation(0, animX, TimeSpan.FromMilliseconds(150));
            var animYDef = new DoubleAnimation(0, animY, TimeSpan.FromMilliseconds(150));

            transform.BeginAnimation(TranslateTransform.XProperty, animXDef);
            transform.BeginAnimation(TranslateTransform.YProperty, animYDef);

            await Task.Delay(160);

            board[emptyCell.row, emptyCell.col] = board[row, col];
            board[row, col] = 0;

            buttons[emptyCell.row, emptyCell.col].Content = tile.Content;
            tile.Content = "";
            tile.RenderTransform = null;

            emptyCell = (row, col);
            stepCount++;
            UpdateStepCounter();
        }
    }
}
