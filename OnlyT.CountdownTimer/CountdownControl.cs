﻿using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace OnlyT.CountdownTimer
{
    public class CountdownControl : Control
    {
        private const int MinsToCountdown = 5;
        
        private Path _donut;
        private Path _pie;
        private Ellipse _secondsBall;
        private TextBlock _time;
        private bool _registeredNames;
        private double _canvasWidth;
        private double _canvasHeight;
        private int _outerCircleDiameter;
        private int _outerCircleRadius;
        private int _innerCircleRadius;
        private Point _centrePoint;
        private DateTime _start;        
        private readonly DispatcherTimer _timer;

        public event EventHandler TimeUpEvent;

        static CountdownControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(CountdownControl), 
                new FrameworkPropertyMetadata(typeof(CountdownControl)));
        }

        public CountdownControl()
        {
            _timer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromMilliseconds(20)
            };
            
            _timer.Tick += TimerFire;
        }

        public void Start(int secsElapsed)
        {
            _start = DateTime.UtcNow.AddSeconds(-secsElapsed);
            _timer.Start();
        }

        public void Stop()
        {
            Dispatcher.Invoke(() =>
            {
                _timer.Stop();

                Animations.FadeOut(this, new FrameworkElement[] { _donut, _secondsBall, _pie, _time },
                    (sender, args) =>
                    {
                        Visibility = Visibility.Hidden;
                    });
            });
        }

        private void RenderPieSliceAndBall(double angle, double secondsElapsed)
        {
            if (!Dispatcher.HasShutdownStarted)
            {
                _pie.Data = PieSlice.Get(angle, _centrePoint, _innerCircleRadius, _outerCircleRadius);

                Point ballPt = SecondsBall.GetPos(
                    _centrePoint,
                    (int)secondsElapsed % 60,
                    _secondsBall.Width / 2,
                    _innerCircleRadius);

                Canvas.SetLeft(_secondsBall, ballPt.X);
                Canvas.SetTop(_secondsBall, ballPt.Y);

                _time.Text = GetTimeText();
            }
        }

        private void TimerFire(object sender, EventArgs e)
        {
            _timer.Stop();

            if (_start != default(DateTime))
            {
                var secsInCountdown = MinsToCountdown * 60;
                var secondsElapsed = (DateTime.UtcNow - _start).TotalSeconds;
                var secondsLeft = secsInCountdown - secondsElapsed;

                if (secondsLeft >= 0)
                {
                    double angle = 360 - ((double)360 / secsInCountdown) * secondsLeft;
                    RenderPieSliceAndBall(angle, secondsElapsed);

                    if (!Dispatcher.HasShutdownStarted)
                    {
                        _timer.Start();
                    }
                }
                else
                {
                    RenderPieSliceAndBall(0, 0);

                    if (!Dispatcher.HasShutdownStarted)
                    {
                        OnTimeUpEvent();
                    }
                }
            }
            else
            {
                _timer.Start();
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (GetTemplateChild("CountdownCanvas") is Canvas canvas)
            {
                InitCanvas(canvas);
            }
        }

        private void InitCanvas(Canvas canvas)
        {
            AddGeometryToCanvas(canvas);
            canvas.Loaded += OnCanvasLoaded;
        }

        private void RegisterName(Canvas canvas, string name, object scopedElement)
        {
            if (canvas.FindName(name) == null)
            {
                RegisterName(name, scopedElement);
            }
        }

        private void RegisterNames(Canvas canvas)
        {
            if (!_registeredNames)
            {
                _registeredNames = true;

                RegisterName(canvas, _donut.Name, _donut);
                RegisterName(canvas, _pie.Name, _pie);
                RegisterName(canvas, _secondsBall.Name, _secondsBall);
                RegisterName(canvas, _time.Name, _time);
            }
        }

        private void OnCanvasLoaded(object sender, RoutedEventArgs e)
        {
            Canvas canvas = (Canvas) sender;
            
            RegisterNames(canvas);

            _canvasWidth = canvas.ActualWidth;
            _canvasHeight = canvas.ActualHeight;

            _time.FontSize = 12;
            _time.FontWeight = FontWeights.Bold;

            double outerRadiusFactor = 0.24;
            double innerRadiusFactor = 0.15;

            _outerCircleRadius = (int)(_canvasHeight * outerRadiusFactor);
            _outerCircleDiameter = _outerCircleRadius * 2;

            _innerCircleRadius = (int)(_canvasHeight * innerRadiusFactor);

            var totalWidthNeeded = 3.25 * _outerCircleDiameter;
            _centrePoint = CalcCentrePoint(totalWidthNeeded);

            _donut.Data = PieSlice.Get(0.1, _centrePoint, _innerCircleRadius, _outerCircleRadius);

            _secondsBall.Width = (double)_innerCircleRadius / 6;
            _secondsBall.Height = (double)_innerCircleRadius / 6;

            _time.Text = GetTimeText();

            Size sz = GetTextSize(useExtent: true);
            while (sz.Height < (3 * (double)_outerCircleDiameter) / 4)
            {
                _time.FontSize += 0.5;
                sz = GetTextSize(useExtent: true);
            }

            sz = GetTextSize(useExtent: true);

            Canvas.SetLeft(_time, _centrePoint.X - _outerCircleRadius + totalWidthNeeded - sz.Width);
            Canvas.SetTop(_time, _centrePoint.Y - sz.Height);

            RenderPieSliceAndBall(0.1, 0);

            Visibility = Visibility.Visible;
            Animations.FadeIn(this, new FrameworkElement[] { _donut, _secondsBall, _pie, _time });
        }

        Color ToColor(string htmlColor)
        {
            return (Color)ColorConverter.ConvertFromString(htmlColor);
        }

        private void AddGeometryToCanvas(Canvas canvas)
        {
            Color revealedColor = ToColor("#c0c5c1");
            Color externalRingColor = ToColor("#74546a");
            Color ballColor = ToColor("#eaf0ce");
            Color ringStrokeColor = ToColor("#473341");
            Color innerHighlightColor = Colors.White;

            var gs1 = new GradientStopCollection(2)
            {
                new GradientStop(innerHighlightColor, 0.7),
                new GradientStop(revealedColor, 1)
            };

            _donut = new Path
            {
                Fill = new RadialGradientBrush(gs1),
                Name = "Donut"
            };

            _pie = new Path
            {
                Stroke = new SolidColorBrush(ringStrokeColor),
                StrokeThickness = 1,
                Fill = new SolidColorBrush(externalRingColor),
                Name = "Pie"
            };

            _secondsBall = new Ellipse
            {
                Fill = new SolidColorBrush(ballColor),
                Name = "SecondsBall"
            };

            canvas.Children.Add(_donut);
            canvas.Children.Add(_pie);
            canvas.Children.Add(_secondsBall);
            
            Color textColor = ToColor("#eaf0ce");
            _time = new TextBlock { Foreground = new SolidColorBrush(textColor), Name = "TimeTxt" };
            canvas.Children.Add(_time);
        }

        private Point CalcCentrePoint(double totalWidthNeeded)
        {
            var margin = (_canvasWidth - totalWidthNeeded) / 2;
            return new Point(margin + _outerCircleRadius, _canvasHeight / 2);
        }

        private string GetTimeText(int? secsLeft = null)
        {
            double secondsLeft = secsLeft ?? 0;

            if (secsLeft == null)
            {
                if (_start == default(DateTime))
                {
                    secondsLeft = MinsToCountdown * 60;
                }
                else
                {
                    var secsInCountdown = MinsToCountdown * 60;
                    var secondsElapsed = (DateTime.UtcNow - _start).TotalSeconds;
                    secondsLeft = secsInCountdown - secondsElapsed + 1;
                }
            }

            int mins = (int)secondsLeft / 60;
            int secs = (int)(secondsLeft % 60);

            return $"{mins}:{secs:D2}";
        }

        private Size GetTextSize(bool useExtent)
        {
            var formattedText = new FormattedText(GetTimeText(), CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight, new Typeface(_time.FontFamily, _time.FontStyle, _time.FontWeight, FontStretches.Normal),
                _time.FontSize,
                Brushes.Black);

            return new Size(formattedText.Width, useExtent ? formattedText.Extent : formattedText.Height);
        }

        protected virtual void OnTimeUpEvent()
        {
            TimeUpEvent?.Invoke(this, EventArgs.Empty);
        }
    }
}
