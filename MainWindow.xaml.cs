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
        private readonly List<(Ellipse Cloud, double Speed)> _cloudMeta = new();
        private readonly List<Line> _rain = new();
        private readonly List<Path> _windStrokes = new();
        private DispatcherTimer? _rainTimer;
        private DispatcherTimer? _windTimer;
        private Scene _current = Scene.Cloud;
        private bool _layoutGirlPending; // Add this field for layout state

        private readonly MediaPlayer _musicPlayer = new();
        private Effect? _originalGirlEffect;
        private bool _experienceStarted;
        private const int WindStrokeTarget = 120;

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
                LayoutGirl();
                PreparePreview();
            };
            Closed += OnClosed;
        }

        private void ValidateXamlStructure()
        {
            // Initialize transform group with named transforms from XAML
            GirlScale = (ScaleTransform)FindName("GirlScale") ?? throw new InvalidOperationException("GirlScale not found");
             _ = (TranslateTransform)FindName("GirlTranslate") ?? throw new InvalidOperationException("GirlTranslate not found");
            _originalGirlEffect = Girl?.Effect;
            SetButtonsEnabled(false);
            _musicPlayer.Volume = 0.6;
            _musicPlayer.MediaEnded += (_, __) =>
            {
                _musicPlayer.Position = TimeSpan.Zero;
                _musicPlayer.Play();
            };
            var musicUri = new Uri("pack://application:,,,/Assets/ambient.wav", UriKind.Absolute);
            _musicPlayer.Open(musicUri);
        }

        private void PreparePreview()
        {
            if (Root != null)
            {
                var sky = new LinearGradientBrush
                {
                    StartPoint = new Point(0.5, 0),
                    EndPoint = new Point(0.5, 1)
                };
                sky.GradientStops.Add(new GradientStop(Color.FromRgb(32, 41, 72), 0));
                sky.GradientStops.Add(new GradientStop(Color.FromRgb(14, 21, 32), 1));
                Root.Background = sky;
            }

            if (Caption != null)
            {
                Caption.Visibility = Visibility.Collapsed;
            }

            if (Girl != null)
            {
                Girl.Opacity = 0.85;
            }
        }

        private void SetButtonsEnabled(bool enabled)
        {
            if (BtnCloud != null) BtnCloud.IsEnabled = enabled;
            if (BtnRain != null) BtnRain.IsEnabled = enabled;
            if (BtnWind != null) BtnWind.IsEnabled = enabled;

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
                    caption.Text = "☁ Drifting Free · Cloud";
                    break;
                case Scene.Rain:
                    caption.Text = "🌧 Quiet Disappearance · Rain";
                    break;
                case Scene.Wind:
                    caption.Text = "🌫 Fragile Freedom · Wind";
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
            
            var sky = new LinearGradientBrush
            {
                StartPoint = new Point(0.5, 0),
                EndPoint = new Point(0.5, 1)
            };
            sky.GradientStops.Add(new GradientStop(Color.FromRgb(193, 226, 255), 0));
            sky.GradientStops.Add(new GradientStop(Color.FromRgb(241, 214, 232), 0.6));
            sky.GradientStops.Add(new GradientStop(Color.FromRgb(186, 213, 255), 1));
            Root.Background = sky;
        }

        private void SkyToRainMuted()
        {
            if (Root == null) return;
            
            var sky = new LinearGradientBrush
            {
                StartPoint = new Point(0.5, 0),
                EndPoint = new Point(0.5, 1)
            };
            sky.GradientStops.Add(new GradientStop(Color.FromRgb(128, 128, 128), 0));    // Gray
            sky.GradientStops.Add(new GradientStop(Color.FromRgb(169, 169, 169), 1));    // DarkGray
            Root.Background = sky;
        }

        private void SkyToWindDawn()
        {
            if (Root == null) return;
            
            var sky = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0.5),
                EndPoint = new Point(1, 0.5)
            };
            sky.GradientStops.Add(new GradientStop(Color.FromRgb(189, 216, 255), 0));
            sky.GradientStops.Add(new GradientStop(Color.FromRgb(255, 200, 214), 0.5));
            sky.GradientStops.Add(new GradientStop(Color.FromRgb(255, 168, 185), 1));
            Root.Background = sky;
        }

        private void StartWind()
        {
            if (_windTimer == null)
            {
                _windTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(60), DispatcherPriority.Render,
                    (_, _) => AddWindStroke(), Dispatcher);
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
            if (_windStrokes.Count >= WindStrokeTarget) return;
            var stroke = new Path
            {
                Stroke = new SolidColorBrush(Colors.White) { Opacity = 0.45 },
                StrokeThickness = 2,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeStartLineCap = PenLineCap.Round
            };

            var geometry = new PathGeometry();
            var figure = new PathFigure();
            var startY = _rnd.Next(20, (int)Math.Max(40, Root.ActualHeight - 40));
            figure.StartPoint = new Point(-80, startY);
            
            var segment = new BezierSegment
            {
                Point1 = new Point(figure.StartPoint.X + 40, figure.StartPoint.Y + _rnd.NextDouble() * 20 - 10),
                Point2 = new Point(figure.StartPoint.X + 120, figure.StartPoint.Y + _rnd.NextDouble() * 20 - 10),
                Point3 = new Point(figure.StartPoint.X + 180, figure.StartPoint.Y + _rnd.NextDouble() * 20 - 10)
            };

            figure.Segments.Add(segment);
            geometry.Figures.Add(figure);
            stroke.Data = geometry;

            Root.Children.Add(stroke);
            _windStrokes.Add(stroke);

            Canvas.SetTop(stroke, 0);
            var translate = new TranslateTransform();
            stroke.RenderTransform = translate;

            var travel = new DoubleAnimation
            {
                From = 0,
                To = Root.ActualWidth + 200,
                Duration = TimeSpan.FromSeconds(2.4),
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseOut }
            };

            var fadeIn = new DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = TimeSpan.FromSeconds(0.4)
            };

            var fadeOut = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                BeginTime = TimeSpan.FromSeconds(1.8),
                Duration = TimeSpan.FromSeconds(0.6)
            };

            fadeIn.Completed += (_, _) => stroke.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            fadeOut.Completed += (_, _) =>
            {
                Root.Children.Remove(stroke);
                _windStrokes.Remove(stroke);
            };

            stroke.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            translate.BeginAnimation(TranslateTransform.XProperty, travel);
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
        private void BtnPlay_OnClick(object sender, RoutedEventArgs e)
        {
            if (_experienceStarted)
            {
                return;
            }

            _experienceStarted = true;
            if (IntroOverlay != null)
            {
                IntroOverlay.Visibility = Visibility.Collapsed;
            }
            SetButtonsEnabled(true);
            if (Caption != null)
            {
                Caption.Visibility = Visibility.Visible;
            }
            InitClouds();
            EnterScene(Scene.Cloud, withIntro: true);
            StartMusic();
        }

        private void StartMusic()
        {
            _musicPlayer.Position = TimeSpan.Zero;
            _musicPlayer.Play();
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
            _cloudMeta.Clear();

            var speeds = new[] { 0.08, 0.12, 0.18 };
            foreach (var speed in speeds)
            {
                for (int i = 0; i < 4; i++)
                {
                    var cloud = MakeCloud(speed);
                    _clouds.Add(cloud);
                    _cloudMeta.Add((cloud, speed));
                    CloudLayer.Children.Add(cloud);
                    Canvas.SetLeft(cloud, _rnd.Next(0, Math.Max(1, (int)Root.ActualWidth)));
                    Canvas.SetTop(cloud, _rnd.Next(20, 140));
                }
            }

            CompositionTarget.Rendering -= MoveClouds;
            CompositionTarget.Rendering += MoveClouds;
        }

        private Ellipse MakeCloud(double speed)
        {
            return new Ellipse
            {
                Width = _rnd.Next(60, 120) * (0.8 + speed),
                Height = _rnd.Next(30, 60) * (0.7 + speed / 2),
                Fill = new SolidColorBrush(Colors.White) { Opacity = 0.75 }
            };
        }

        private void MoveClouds(object? sender, EventArgs e)
        {
            if (Root == null || Root.ActualWidth <= 0) return;

            foreach (var (cloud, speed) in _cloudMeta.ToList())
            {
                if (cloud == null) continue;
                
                double x = Canvas.GetLeft(cloud);
               x -= speed;

                if (x + cloud.ActualWidth < -150)
                    x = Root.ActualWidth + _rnd.Next(20, 140);
                    
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
                    ApplySceneStyling(Scene.Cloud);
                    break;
                case Scene.Rain:
                    SkyToRainMuted();
                    StartRain();
                    StopWind();
                    ApplySceneStyling(Scene.Rain);
                    break;
                case Scene.Wind:
                    SkyToWindDawn();
                    StopRain();
                    StartWind();
                    ApplySceneStyling(Scene.Wind);
                    break;
            }

            StartGirlAnimations(target, withIntro);
            ShowCaption(true);
        }

        private static Effect? CloneEffect(Effect? effect)
        {
            if (effect is Freezable freezable)
            {
                return (Effect)freezable.Clone();
            }

            return effect;
        }


        private void StopAllAnimations()
        {
            if (Girl == null) return;
            
            Girl.BeginAnimation(OpacityProperty, null);
            var tt = EnsureGirlTranslate();
            tt?.BeginAnimation(TranslateTransform.YProperty, null);
            GirlScale?.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            GirlScale?.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        }

        private void ApplySceneStyling(Scene scene)
        {
            if (Girl == null || GirlScale == null) return;

            Girl.Effect = CloneEffect(_originalGirlEffect);
            GirlScale.ScaleX = 1;
            GirlScale.ScaleY = 1;
            Girl.Opacity = 1;

            switch (scene)
            {
                case Scene.Cloud:
                    if (Girl.Effect is DropShadowEffect glow)
                    {
                        glow.BlurRadius = 18;
                        glow.Color = Color.FromRgb(255, 255, 255);
                        glow.Opacity = 0.75;
                    }
                    break;
                case Scene.Rain:
                    GirlScale.ScaleX = GirlScale.ScaleY = 0.92;
                    Girl.Opacity = 0.9;
                    if (Girl.Effect is DropShadowEffect rainGlow)
                    {
                        rainGlow.Color = Color.FromRgb(170, 196, 221);
                        rainGlow.BlurRadius = 12;
                        rainGlow.Opacity = 0.55;
                    }
                    break;
                case Scene.Wind:
                    GirlScale.ScaleX = GirlScale.ScaleY = 0.86;
                    Girl.Opacity = 0.88;
                    var blur = new BlurEffect
                    {
                        Radius = 2.2,
                        KernelType = KernelType.Gaussian,
                        RenderingBias = RenderingBias.Performance
                    };
                    if (_originalGirlEffect is DropShadowEffect baseGlow)
                    {
                        var group = new EffectGroup();
                        group.Children.Add((Effect)baseGlow.Clone());
                        group.Children.Add(blur);
                        Girl.Effect = group;
                    }
                    else
                    {
                        Girl.Effect = blur;
                    }
                    break;
            }
        }

        private void StartGirlAnimations(Scene scene, bool withIntro)
        {
            if (Girl == null) return;

            var yAnim = new DoubleAnimationUsingKeyFrames 
            { 
                RepeatBehavior = RepeatBehavior.Forever,
                AutoReverse = true 
            };
            yAnim.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            yAnim.KeyFrames.Add(new EasingDoubleKeyFrame(-8, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(3))));
            
            var duration = scene switch
            {
                Scene.Cloud => TimeSpan.FromSeconds(4),
                Scene.Rain => TimeSpan.FromSeconds(6),
                Scene.Wind => TimeSpan.FromSeconds(3),
                _ => TimeSpan.FromSeconds(4)
            };
            var offset = scene switch
            {
                Scene.Cloud => -10,
                Scene.Rain => -6,
                Scene.Wind => -14,
                _ => -8
            };
            yAnim.KeyFrames.Add(new EasingDoubleKeyFrame(offset, KeyTime.FromTimeSpan(duration)));

            var tt = EnsureGirlTranslate();
            tt?.BeginAnimation(TranslateTransform.YProperty, yAnim);
            
            if (withIntro)
            {
                Girl.Opacity = 0;
                GirlScale.ScaleY = 1.2;
                Girl.BeginAnimation(OpacityProperty, DA(1, 1.5));
                GirlScale.BeginAnimation(ScaleTransform.ScaleYProperty, DA(1, 1.5));
                GirlScale.BeginAnimation(ScaleTransform.ScaleYProperty, DA(1, 1.5));
            }
            else
            {
                Girl.BeginAnimation(OpacityProperty, DA(Girl.Opacity, 0.8));
                GirlScale.BeginAnimation(ScaleTransform.ScaleYProperty, DA(GirlScale.ScaleY, 0.1));
                GirlScale.BeginAnimation(ScaleTransform.ScaleXProperty, DA(GirlScale.ScaleX, 0.1));
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
            _musicPlayer.Stop();
            _musicPlayer.Close();
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
                Stroke = new SolidColorBrush(Color.FromRgb(190, 215, 236)) { Opacity = 0.45 },
                X1 = _rnd.Next(0, (int)Root.ActualWidth),
                Y1 = -10,
                StrokeThickness = 1.2
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
