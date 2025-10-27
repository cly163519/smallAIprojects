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
    public enum Scene
    {
        Cloud,
        Rain,
        Wind
    }

    public partial class MainWindow : Window
    {
        private readonly Random _rnd = new();
        private readonly List<Ellipse> _clouds = new();
        private readonly List<Line> _rain = new();
        private readonly List<Path> _windStrokes = new();
        private DispatcherTimer? _rainTimer;
        private DispatcherTimer? _windTimer;
        private Scene _current = Scene.Cloud;
        private bool _layoutGirlPending; // Add this field for layout state

        // Remove duplicate field declarations since they're defined in XAML
        // These will be available through the partial class generated from XAML
        // private Canvas Root;
        // private Canvas CloudLayer;
        // private Image Girl;
        // private ScaleTransform GirlScale;

        // Fix opacity property warning
        new public static readonly DependencyProperty OpacityProperty = 
            UIElement.OpacityProperty;

        public MainWindow()
        {
            InitializeComponent();
            ValidateXamlStructure();
            
            SizeChanged += (_, __) => LayoutGirl();
            Loaded += (_, __) => 
            {
                InitClouds();
                EnterScene(Scene.Cloud, withIntro: true);
            };
            Closed += OnClosed;
        }

        private void ValidateXamlStructure()
        {
            // Initialize transform group with named transforms from XAML
            GirlScale = (ScaleTransform)FindName("GirlScale") ?? throw new InvalidOperationException("GirlScale not found");
            var girlTranslate = (TranslateTransform)FindName("GirlTranslate") ?? throw new InvalidOperationException("GirlTranslate not found");
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

        private void BtnCloud_OnClick(object sender, RoutedEventArgs e)
        {
            EnterScene(Scene.Cloud);
        }

        private void BtnRain_OnClick(object sender, RoutedEventArgs e)
        {
            EnterScene(Scene.Rain);
        }

        private void BtnWind_OnClick(object sender, RoutedEventArgs e)
        {
            EnterScene(Scene.Wind);
        }

        private void InitClouds()
        {
            if (CloudLayer == null || Root == null) return;
            
            CloudLayer.Children.Clear();
            _clouds.Clear();

            for (int i = 0; i < 6; i++)
            {
                var cloud = MakeCloud();
                _clouds.Add(cloud);
                CloudLayer.Children.Add(cloud);
                Canvas.SetLeft(cloud, _rnd.Next(0, (int)Root.ActualWidth));
                Canvas.SetTop(cloud, _rnd.Next(20, 100));
            }

            CompositionTarget.Rendering -= MoveClouds;
            CompositionTarget.Rendering += MoveClouds;
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
            _current = target;

            // Stop current animations
            StopAllAnimations();

            // Scene transitions
            switch (target)
            {
                case Scene.Cloud:
                    SkyToBluePink();
                    StopRain();
                    StopWind();
                    break;
                case Scene.Rain:
                    SkyToRainMuted();
                    StartRain();
                    StopWind();
                    break;
                case Scene.Wind:
                    SkyToWindDawn();
                    StopRain();
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

        private void StartGirlAnimations(bool withIntro)
        {
            if (Girl == null) return;

            var yAnim = new DoubleAnimationUsingKeyFrames 
            { 
                RepeatBehavior = RepeatBehavior.Forever,
                AutoReverse = true 
            };
            yAnim.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            yAnim.KeyFrames.Add(new EasingDoubleKeyFrame(-8, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(3))));
            
            var tt = EnsureGirlTranslate();
            tt?.BeginAnimation(TranslateTransform.YProperty, yAnim);
            
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

        private void OnClosed(object? sender, EventArgs e) // Fix nullability warning
        {
            CompositionTarget.Rendering -= MoveClouds;
            StopAllAnimations();
            StopRain();
            StopWind();
            _rainTimer?.Stop();
            _windTimer?.Stop();
        }

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
    }
}
