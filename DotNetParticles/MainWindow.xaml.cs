using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using MaterialDesignThemes.Wpf;

namespace DotNetParticles
{
    public partial class MainWindow : Window
    {
        private Random _random = new Random();
        private List<Particle> _particles = new List<Particle>();
        private Cell[,] _grid;
        private int _cellSize = 100;
        private int _numParticles = 50;
        private Brush _particleColors = Brushes.LimeGreen;
        private Brush _particleLines = Brushes.Ivory;
        private Color _particleGlow = Colors.MediumPurple;
        private double _lineThickness = 0.2;
        private double _connectionDistance = 100;
        private double _particleSpeedMultiplier = 0.75;
        private Point _mousePosition;
        private double _MousePushFactor = 1.5;
        private Point _lastMousePosition;

        public static Vector Lerp(Vector a, Vector b, double t)
        {
            return a + (b - a) * t;
        }

        public MainWindow()
        {
            InitializeComponent();

            int gridWidth = (int)Math.Ceiling(Width / _cellSize);
            int gridHeight = (int)Math.Ceiling(Height / _cellSize);
            _grid = new Cell[gridWidth, gridHeight];

            for (int i = 0; i < gridWidth; i++)
            {
                for (int j = 0; j < gridHeight; j++)
                {
                    _grid[i, j] = new Cell();
                }
            }

            ParticleCanvas.MouseMove += ParticleCanvas_MouseMove;
            CompositionTarget.Rendering += UpdateParticles;
            CreateParticles();
        }

        private void CreateParticles()
        {
            // Check if the window has been initialized and the Width and Height are not zero
            if (Width == 0 || Height == 0)
            {
                Debug.WriteLine("Error: Width or Height is 0");
                return;
            }

            for (int i = 0; i < _numParticles; i++)
            {
                Vector direction = new Vector(_random.NextDouble() * 2 - 1, _random.NextDouble() * 2 - 1);
                direction.Normalize();  // Ensure it's a unit vector

                Particle particle = new Particle()
                {
                    Position = new Point(_random.Next((int)Width), _random.Next((int)Height)),
                    Direction = new Vector(_random.NextDouble() * 2 - 1, _random.NextDouble() * 2 - 1),
                    Speed = _random.NextDouble() * 1.5 + 0.5
                };

                particle.Direction = particle.Direction;  // Store the original direction

                _particles.Add(particle);

                int cellX = _cellSize > 0 ? (int)(particle.Position.X / _cellSize) : 0;
                int cellY = _cellSize > 0 ? (int)(particle.Position.Y / _cellSize) : 0;
                _grid[cellX, cellY].Particles.Add(particle);

                Ellipse ellipse = new Ellipse()
                {
                    Width = 5,
                    Height = 5,
                    Fill = _particleColors,
                    Effect = new DropShadowEffect
                    {
                        Color = _particleGlow,
                        BlurRadius = 5,
                        ShadowDepth = 0
                    }
                };

                Canvas.SetLeft(ellipse, particle.Position.X);
                Canvas.SetTop(ellipse, particle.Position.Y);

                ParticleCanvas.Children.Add(ellipse);
                particle.Shape = ellipse;
            }
        }

        private List<Particle> GetNearbyParticles(Particle particle, Cell[,] grid)
        {
            int cellX = (int)particle.Position.X / _cellSize;
            int cellY = (int)particle.Position.Y / _cellSize;

            List<Particle> nearbyParticles = new List<Particle>();

            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    int newX = cellX + i;
                    int newY = cellY + j;

                    // Check for out-of-bound grid indices
                    if (newX < 0 || newX >= grid.GetLength(0) || newY < 0 || newY >= grid.GetLength(1))
                    {
                        continue;
                    }

                    nearbyParticles.AddRange(grid[newX, newY].Particles);
                }
            }

            return nearbyParticles;
        }

        private void UpdateParticles(object sender, EventArgs e)
        {
            // Check if the grid has been initialized
            if (_grid == null)
            {
                Debug.WriteLine("Error: grid not initialized");
                return;
            }

            Cell[,] newGrid = new Cell[_grid.GetLength(0), _grid.GetLength(1)];

            for (int i = 0; i < _grid.GetLength(0); i++)
            {
                for (int j = 0; j < _grid.GetLength(1); j++)
                {
                    newGrid[i, j] = new Cell();
                }
            }

            foreach (Cell cell in _grid)
            {
                var particlesCopy = new List<Particle>(cell.Particles);

                foreach (Particle particle in particlesCopy)
                {
                    // Always calculate distance to mouse
                    double distanceToMouse = (particle.Position - _mousePosition).Length;

                    Vector towardsMouse = _mousePosition - particle.Position;
                    towardsMouse.Normalize();

                    // Get a clockwise tangent vector.
                    Vector tangent = new Vector(towardsMouse.Y, -towardsMouse.X) * _MousePushFactor;

                    Vector finalDirection;
                    if (distanceToMouse < _connectionDistance)
                    {
                        // Calculate a factor between 0 (mouse is far) and 1 (mouse is very close)
                        double lerpFactor = 1 - (distanceToMouse / _connectionDistance);
                        // Mix the tangent direction and the particle's original direction
                        finalDirection = Lerp(tangent, particle.Direction, lerpFactor);
                    }
                    else
                    {
                        finalDirection = particle.Direction;
                    }

                    particle.Direction = finalDirection;  // Update the particle's direction

                    // Update particle positions and handle canvas edges
                    particle.Position = Point.Add(particle.Position, Vector.Multiply(particle.Direction, particle.Speed * _particleSpeedMultiplier));

                    if (particle.Position.X < 0)
                        particle.Position = new Point(particle.Position.X + Width, particle.Position.Y);
                    else if (particle.Position.X > Width)
                        particle.Position = new Point(particle.Position.X - Width, particle.Position.Y);

                    if (particle.Position.Y < 0)
                        particle.Position = new Point(particle.Position.X, particle.Position.Y + Height);
                    else if (particle.Position.Y > Height)
                        particle.Position = new Point(particle.Position.X, particle.Position.Y - Height);

                    // Move particle
                    Canvas.SetLeft(particle.Shape, particle.Position.X);
                    Canvas.SetTop(particle.Shape, particle.Position.Y);

                    // Update colors
                    if (particle.Shape != null)
                    {
                        particle.Shape.Fill = _particleColors;
                        if (particle.Shape.Effect is DropShadowEffect dropShadowEffect)
                        {
                            dropShadowEffect.Color = _particleGlow;
                        }
                    }

                    int newCellX = (int)particle.Position.X / _cellSize;
                    int newCellY = (int)particle.Position.Y / _cellSize;

                    newCellX = (newCellX + newGrid.GetLength(0)) % newGrid.GetLength(0);
                    newCellY = (newCellY + newGrid.GetLength(1)) % newGrid.GetLength(1);
                    newGrid[newCellX, newCellY].Particles.Add(particle);

                    List<Particle> nearbyParticles = GetNearbyParticles(particle, newGrid);

                    List<Line> newLines = new List<Line>();

                    foreach (Particle otherParticle in nearbyParticles)
                    {
                        if (otherParticle == particle) continue;

                        double distance = Vector.Subtract((Vector)otherParticle.Position, (Vector)particle.Position).Length;

                        if (distance < _connectionDistance)
                        {
                            Line line;
                            if (particle.Lines.Count > 0)
                            {
                                line = particle.Lines[0];
                                particle.Lines.RemoveAt(0);
                            }
                            else
                            {
                                line = new Line()
                                {
                                    Stroke = _particleLines,
                                    StrokeThickness = _lineThickness
                                };
                                ParticleCanvas.Children.Add(line);
                            }

                            line.X1 = particle.Position.X;
                            line.Y1 = particle.Position.Y;
                            line.X2 = otherParticle.Position.X;
                            line.Y2 = otherParticle.Position.Y;

                            // Update line color
                            line.Stroke = _particleLines;

                            newLines.Add(line);
                        }
                    }

                    // Remove unused lines from canvas and list
                    foreach (Line oldLine in particle.Lines)
                    {
                        ParticleCanvas.Children.Remove(oldLine);
                    }

                    particle.Lines.Clear(); // Clear the list after removing lines from the canvas

                    particle.Lines = newLines;
                }
            }

            _grid = newGrid;
        }

        private void ParticleCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            Point newPosition = e.GetPosition(ParticleCanvas);
            if ((_lastMousePosition - newPosition).Length > 5) // Change 5 to whatever threshold you find suitable
            {
                _mousePosition = newPosition;
                _lastMousePosition = newPosition;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ForgotPassword_Click(object sender, RoutedEventArgs e)
        {
            // Your forgot password logic here
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void ParticleGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void Theme1_Click(object sender, RoutedEventArgs e)
        {
            IBaseTheme baseTheme = new MaterialDesignLightTheme();
            var paletteHelper = new PaletteHelper();

            Color primaryColor = (Color)ColorConverter.ConvertFromString("#673AB7"); 
            Color secondaryColor = (Color)ColorConverter.ConvertFromString("#FFC107"); 
            Color linesColor = (Color)ColorConverter.ConvertFromString("#9575CD"); 

            ITheme theme = Theme.Create(baseTheme, primaryColor, secondaryColor);
            paletteHelper.SetTheme(theme);

            // Change colors for particles, lines and shadows
            _particleColors = new SolidColorBrush(secondaryColor);
            _particleLines = new SolidColorBrush(linesColor);
            _particleGlow = secondaryColor;
        }

        private void Theme2_Click(object sender, RoutedEventArgs e)
        {
            IBaseTheme baseTheme = new MaterialDesignLightTheme();
            var paletteHelper = new PaletteHelper();

            Color primaryColor = (Color)ColorConverter.ConvertFromString("#006400");
            Color secondaryColor = (Color)ColorConverter.ConvertFromString("#FFD700");
            Color particleColor = (Color)ColorConverter.ConvertFromString("#32CD32");
            Color linesColor = (Color)ColorConverter.ConvertFromString("#FFD700");

            ITheme theme = Theme.Create(baseTheme, primaryColor, secondaryColor);
            paletteHelper.SetTheme(theme);

            // Change colors for particles, lines and shadows
            _particleColors = new SolidColorBrush(particleColor);
            _particleLines = new SolidColorBrush(linesColor);
            _particleGlow = linesColor;
        }

        private void Theme3_Click(object sender, RoutedEventArgs e)
        {
            IBaseTheme baseTheme = new MaterialDesignLightTheme();
            var paletteHelper = new PaletteHelper();

            Color primaryColor = (Color)ColorConverter.ConvertFromString("#000000");
            Color secondaryColor = (Color)ColorConverter.ConvertFromString("#673AB7");
            Color particleColor = (Color)ColorConverter.ConvertFromString("#8A2BE2");
            Color linesColor = (Color)ColorConverter.ConvertFromString("#00FFFF");

            ITheme theme = Theme.Create(baseTheme, primaryColor, secondaryColor);
            paletteHelper.SetTheme(theme);

            // Change colors for particles, lines and shadows
            _particleColors = new SolidColorBrush(particleColor);
            _particleLines = new SolidColorBrush(linesColor);
            _particleGlow = linesColor;
        }

        private void Theme4_Click(object sender, RoutedEventArgs e)
        {
            IBaseTheme baseTheme = new MaterialDesignLightTheme();
            var paletteHelper = new PaletteHelper();

            Color primaryColor = (Color)ColorConverter.ConvertFromString("#000080");
            Color secondaryColor = (Color)ColorConverter.ConvertFromString("#FFA500");
            Color particleColor = (Color)ColorConverter.ConvertFromString("#FFA500");
            Color linesColor = (Color)ColorConverter.ConvertFromString("#FFC966");

            ITheme theme = Theme.Create(baseTheme, primaryColor, secondaryColor);
            paletteHelper.SetTheme(theme);

            // Change colors for particles, lines and shadows
            _particleColors = new SolidColorBrush(particleColor);
            _particleLines = new SolidColorBrush(linesColor);
            _particleGlow = linesColor;
        }
    }

    public class Particle
    {
        public Point Position { get; set; }
        public Vector Direction { get; set; }
        public double Speed { get; set; }
        public Ellipse? Shape { get; set; }
        public List<Particle> Connections { get; set; } = new List<Particle>();
        public List<Line> Lines { get; set; } = new List<Line>();
        public Vector ExternalForce { get; set; }
    }

    public class Cell
    {
        public List<Particle> Particles { get; set; } = new List<Particle>();
    }
}