using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;


namespace NotHumanToday.Animated
{
    public partial class MainWindow : Window
    {
        private readonly Random _rnd = new();
        private readonly List<Ellipse> _clouds = new();
        private readonly List<Line> _rain = new();
        private readonly List<Path> _windStrokes = new();
        private DispatcherTimer? _rainTimer;
        private DispatcherTimer? _windTimer;
        private Scene _current = Scene.Cloud;
        private Canvas? Root;
        private Canvas? CloudLayer;
        private bool _layoutGirlPending;

        // Fix: Proper initialization
        private ScaleTransform GirlScale { get; set; } = new(1, 1);
        private UIElement? Girl { get; set; }

        private static readonly DependencyProperty OpacityProperty = UIElement.OpacityProperty;

        public MainWindow()
        {
            InitializeComponent();
            ValidateXamlStructure();
            
            SizeChanged += (_, __) => LayoutGirl();
            Loaded += OnLoaded;
            Closed += OnClosed;
        }

        private void ValidateXamlStructure()
        {
            Root = (Canvas)FindName("Root") ?? throw new InvalidOperationException("Root canvas not found");
            CloudLayer = (Canvas)FindName("CloudLayer") ?? throw new InvalidOperationException("CloudLayer not found");
            Girl = (UIElement)FindName("Girl") ?? throw new InvalidOperationException("Girl element not found");
            
            // Initialize GirlScale and transforms before using them
            var transformGroup = new TransformGroup();
            GirlScale = new ScaleTransform(1, 1);
            var translate = new TranslateTransform();
            transformGroup.Children.Add(GirlScale);
            transformGroup.Children.Add(translate);
            Girl.RenderTransform = transformGroup;
            
            if (Girl is FrameworkElement girlElement)
            {
                girlElement.SizeChanged += (_, __) => LayoutGirl();

                // Ensure we also center once the image pixels become available.
                if (girlElement is Image girlImage)
                {
                    girlImage.ImageOpened += (_, __) => LayoutGirl();
                }
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            LayoutGirl();
            InitClouds();
            EnterScene(Scene.Cloud, withIntro: true);
        }

        private void OnClosed(object sender, EventArgs e)
        {
            CompositionTarget.Rendering -= MoveClouds;
            _rainTimer?.Stop();
            _windTimer?.Stop();
            _rainTimer = null;
            _windTimer = null;
        }

        private void InitClouds()
        {
            if (CloudLayer == null || Root == null) return;
            
            CloudLayer.Children.Clear();
            _clouds.Clear();

            // Create initial clouds with safe width calculation
            var safeWidth = Math.Max(1, (int)Root.ActualWidth);
            for (int i = 0; i < 6; i++)
            {
                var cloud = MakeCloud();
                _clouds.Add(cloud);
                CloudLayer.Children.Add(cloud);
                Canvas.SetLeft(cloud, _rnd.Next(0, safeWidth));
                Canvas.SetTop(cloud, _rnd.Next(20, 100));
            }

            CompositionTarget.Rendering -= MoveClouds;
            CompositionTarget.Rendering += MoveClouds;
        }

        private void MoveClouds(object? sender, EventArgs e)
        {
            if (Root == null || Root.ActualWidth <= 0) return;

            foreach (var cloud in _clouds.ToList())
            {
                if (cloud == null) continue;
                
                double x = Canvas.GetLeft(cloud);
                x -= 0.2;
                
                if (x + cloud.ActualWidth < -100)
                    x = Root.ActualWidth;
                    
                Canvas.SetLeft(cloud, x);
            }
        }

        private void EnterScene(Scene target, bool withIntro = false)
        {
            if (_current == target && !withIntro) return;
            
            // Stop current animations and effects first
            StopAllAnimations();
            StopAllEffects();
            
            // Then change scene and start new effects
            _current = target;
            switch (target)
            {
                case Scene.Cloud:
                    SkyToBluePink();
                    break;
                case Scene.Rain:
                    SkyToRainMuted();
                    StartRain();
                    break;
                case Scene.Wind:
                    SkyToWindDawn();
                    StartWind();
                    break;
            }

            StartGirlAnimations(withIntro);
            ShowCaption(true);
        }

        private void StopAllAnimations()
        {
            if (Girl == null) return;
            
            Girl.BeginAnimation(OpacityProperty, null);
            var tt = EnsureGirlTranslate();
            tt?.BeginAnimation(TranslateTransform.YProperty, null);
            GirlScale?.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        }

        private void StopAllEffects()
        {
            StopRain();
            StopWind();
        }

        private void StartGirlAnimations(bool withIntro)
        {
            if (Girl == null || GirlScale == null) return;

            var tt = EnsureGirlTranslate();
            if (tt == null) return;

            // Clear any existing animations first
            tt.BeginAnimation(TranslateTransform.YProperty, null);
            Girl.BeginAnimation(OpacityProperty, null);
            GirlScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);

            // Then start new animations
            var yAnim = new DoubleAnimationUsingKeyFrames 
            { 
                RepeatBehavior = RepeatBehavior.Forever,
                AutoReverse = true 
            };
            yAnim.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            yAnim.KeyFrames.Add(new EasingDoubleKeyFrame(-8, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(3))));
            
            tt.BeginAnimation(TranslateTransform.YProperty, yAnim);
            
            if (withIntro)
            {
                Girl.Opacity = 0;
                GirlScale.ScaleY = 1.2;
                Girl.BeginAnimation(OpacityProperty, DA(1, 1.5));
                GirlScale.BeginAnimation(ScaleTransform.ScaleYProperty, DA(1, 1.5));
            }
            else
            {
                Girl.BeginAnimation(OpacityProperty, DA(0.95, 0.8));
                GirlScale.BeginAnimation(ScaleTransform.ScaleYProperty, DA(1.0, 0.1));
            }
        }

        #region Buttons
        private void BtnCloud_OnClick(object sender, RoutedEventArgs e) => EnterScene(Scene.Cloud);
        private void BtnRain_OnClick(object sender, RoutedEventArgs e) => EnterScene(Scene.Rain);
        private void BtnWind_OnClick(object sender, RoutedEventArgs e) => EnterScene(Scene.Wind);

        private void BtnPlay_OnClick(object sender, RoutedEventArgs e)
        {
            // 自动播放：Cloud -> Rain -> Wind -> Cloud...
            var seq = new[] { Scene.Cloud, Scene.Rain, Scene.Wind };
            int idx = Array.IndexOf(seq, _current);
            idx = (idx + 1) % seq.Length;
            EnterScene(seq[idx]);
        }
        #endregion

        private void StartRain()
        {
            if (_rainTimer == null)
            {
                _rainTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(50), DispatcherPriority.Render,
                    (s, e) => AddRainDrop(), Dispatcher);
            }
            _rainTimer.Start();
        }

        private void StopRain()
        {
            _rainTimer?.Stop();
            _rain.Clear();
            if (Root != null)
            {
                foreach (var drop in Root.Children.OfType<Line>().ToList())
                {
                    Root.Children.Remove(drop);
                }
            }
        }

        private void AddRainDrop()
        {
            if (Root == null) return;

            var drop = new Line
            {
                Stroke = new SolidColorBrush(Colors.LightBlue) { Opacity = 0.4 },
                X1 = _rnd.Next(0, (int)Root.ActualWidth),
                Y1 = -10,
                StrokeThickness = 1
            };
            drop.X2 = drop.X1 - 3;
            drop.Y2 = drop.Y1 + 10;

            Root.Children.Add(drop);
            _rain.Add(drop);

            var anim = new DoubleAnimation
            {
                From = -10,
                To = Root.ActualHeight + 10,
                Duration = TimeSpan.FromSeconds(0.5)
            };
            anim.Completed += (s, e) =>
            {
                Root.Children.Remove(drop);
                _rain.Remove(drop);
            };

            drop.BeginAnimation(Canvas.TopProperty, anim);
        }

        private Ellipse MakeCloud()
        {
            return new Ellipse
            {
                Width = _rnd.Next(60, 120),
                Height = _rnd.Next(30, 60),
                Fill = new SolidColorBrush(Colors.White) { Opacity = 0.8 }
            };
        }

        private void LayoutGirl()
        {
            if (Girl == null || Root == null) return;
            if (Girl is not FrameworkElement girlElement) return;

            if (!TryGetSize(Root, out var rootWidth, out var rootHeight))
            {
                ScheduleLayoutGirl();
                return;
            }

            if (!TryGetSize(girlElement, out var width, out var height))
            {
                ScheduleLayoutGirl();
                return;
            }

            Canvas.SetLeft(girlElement, (rootWidth - width) / 2);
            Canvas.SetTop(girlElement, (rootHeight - height) / 2);
        }

        private bool TryGetSize(FrameworkElement element, out double width, out double height)
        {
            width = element.ActualWidth;
            height = element.ActualHeight;

            if (IsValidSize(width) && IsValidSize(height) && width > 0 && height > 0)
            {
                return true;
            }

            var renderSize = element.RenderSize;
            width = renderSize.Width;
            height = renderSize.Height;

            if (IsValidSize(width) && IsValidSize(height) && width > 0 && height > 0)
            {
                return true;
            }

            element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var desired = element.DesiredSize;
            width = desired.Width;
            height = desired.Height;

            return IsValidSize(width) && IsValidSize(height) && width > 0 && height > 0;
        }

        private static bool IsValidSize(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        private void ScheduleLayoutGirl()
        {
            if (_layoutGirlPending)
            {
                return;
            }

            _layoutGirlPending = true;
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
            {
                _layoutGirlPending = false;
                LayoutGirl();
            }));
        }

        private void ShowCaption(bool show)
        {
            // Since this is a new UI element, ensure it's properly initialized
            var caption = FindName("Caption") as TextBlock;
            if (caption == null) return;

            caption.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            switch (_current)
            {
                case Scene.Cloud:
                    caption.Text = "Cloudy Day";
                    break;
                case Scene.Rain:
                    caption.Text = "Rainy Day";
                    break;
                case Scene.Wind:
                    caption.Text = "Windy Day";
                    break;
            }
        }

        private DoubleAnimation DA(double to, double duration)
        {
            return new DoubleAnimation(to, TimeSpan.FromSeconds(duration));
        }

        private void SkyToBluePink()
        {
            if (Root == null) return;
            
            var sky = new RadialGradientBrush
            {
                Center = new Point(0.5, 0.5),
                RadiusX = 0.5,
                RadiusY = 0.5,
                GradientOrigin = new Point(0.5, 0.5)
            };
            sky.GradientStops.Add(new GradientStop(Color.FromRgb(255, 182, 193), 0));    // LightPink
            sky.GradientStops.Add(new GradientStop(Color.FromRgb(173, 216, 230), 1));    // LightBlue
            Root.Background = sky;
        }

        private void SkyToRainMuted()
        {
            if (Root == null) return;
            
            var sky = new RadialGradientBrush
            {
                Center = new Point(0.5, 0.5),
                RadiusX = 0.5,
                RadiusY = 0.5,
                GradientOrigin = new Point(0.5, 0.5)
            };
            sky.GradientStops.Add(new GradientStop(Color.FromRgb(128, 128, 128), 0));    // Gray
            sky.GradientStops.Add(new GradientStop(Color.FromRgb(169, 169, 169), 1));    // DarkGray
            Root.Background = sky;
        }

        private void SkyToWindDawn()
        {
            if (Root == null) return;
            
            var sky = new RadialGradientBrush
            {
                Center = new Point(0.5, 0.5),
                RadiusX = 0.5,
                RadiusY = 0.5,
                GradientOrigin = new Point(0.5, 0.5)
            };
            sky.GradientStops.Add(new GradientStop(Color.FromRgb(255, 165, 0), 0));      // Orange
            sky.GradientStops.Add(new GradientStop(Color.FromRgb(255, 140, 0), 1));      // DarkOrange
            Root.Background = sky;
        }

        private void StartWind()
        {
            if (_windTimer == null)
            {
                _windTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(50), DispatcherPriority.Render,
                    (s, e) => AddWindStroke(), Dispatcher);
            }
            _windTimer.Start();
        }

        private void StopWind()
        {
            _windTimer?.Stop();
            _windStrokes.Clear();
            if (Root != null)
            {
                foreach (var stroke in Root.Children.OfType<Path>().ToList())
                {
                    Root.Children.Remove(stroke);
                }
            }
        }

        private void AddWindStroke()
        {
            if (Root == null) return;

            var stroke = new Path
            {
                Stroke = new SolidColorBrush(Colors.White) { Opacity = 0.4 },
                StrokeThickness = 1
            };

            var geometry = new PathGeometry();
            var figure = new PathFigure();
            figure.StartPoint = new Point(_rnd.Next(0, (int)Root.ActualWidth), _rnd.Next(0, (int)Root.ActualHeight));
            
            var segment = new BezierSegment
            {
                Point1 = new Point(figure.StartPoint.X + 20, figure.StartPoint.Y + 5),
                Point2 = new Point(figure.StartPoint.X + 40, figure.StartPoint.Y - 5),
                Point3 = new Point(figure.StartPoint.X + 60, figure.StartPoint.Y)
            };

            figure.Segments.Add(segment);
            geometry.Figures.Add(figure);
            stroke.Data = geometry;

            Root.Children.Add(stroke);
            _windStrokes.Add(stroke);

            var anim = new DoubleAnimation(0, TimeSpan.FromSeconds(1));
            anim.Completed += (s, e) =>
            {
                Root.Children.Remove(stroke);
                _windStrokes.Remove(stroke);
            };

            stroke.BeginAnimation(OpacityProperty, anim);
        }

        private TranslateTransform? EnsureGirlTranslate()
        {
            if (Girl?.RenderTransform is not TransformGroup transformGroup)
                return null;

            var translate = transformGroup.Children.OfType<TranslateTransform>().FirstOrDefault();
            if (translate == null)
            {
                translate = new TranslateTransform();
                transformGroup.Children.Add(translate);
            }
            return translate;
        }
    }

    // Add missing enum
    public enum Scene
    {
        Cloud,
        Rain,
        Wind
    }
}
