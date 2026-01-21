using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Windows;
using System.Windows.Shapes;

namespace BoxAgr.BLL.UIElements
{
    public class TriangleWithExclamationMark : UIElement
    {
        private bool isAnimating = false;
        public double Height { get; set; }
        public double Width { get; set; }
        protected override void OnRender(DrawingContext drawingContext)
        {
            double width = Width;// RenderSize.Width;
            double height = Height;// RenderSize.Height;

            double triangleSize = Math.Min(width, height);
            double exclamationMarkSize = height * 0.5;

            Point triangleTop = new Point(width / 2, 0);
            Point triangleLeft = new Point(0, height);
            Point triangleRight = new Point(width, height);

            PathGeometry pathGeometry = new PathGeometry();
            PathFigure pathFigure = new PathFigure();

            pathFigure.StartPoint = triangleTop;
            pathFigure.Segments.Add(new LineSegment(triangleLeft, true));
            pathFigure.Segments.Add(new LineSegment(triangleRight, true));

            pathGeometry.Figures.Add(pathFigure);

            drawingContext.DrawGeometry(Brushes.Red, null, pathGeometry);

            Typeface typeface = new Typeface(new FontFamily("Arial"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);

            FormattedText formattedText = new FormattedText("!", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, exclamationMarkSize, Brushes.White, null, TextFormattingMode.Display);

            double exclamationMarkX = (width - formattedText.Width) / 2;
            double exclamationMarkY = (height - formattedText.Height) / 2;

            drawingContext.DrawText(formattedText, new Point(exclamationMarkX, exclamationMarkY));

            if (!isAnimating)
            {
                StartAnimation(height);
                isAnimating = true;
            }
        }

        private void StartAnimation(double height)
        {
            DoubleAnimation animation = new DoubleAnimation
            {
                From = height * 0.1,
                To = height * 0.2,
                Duration = TimeSpan.FromMilliseconds(500),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };

            RenderTransformOrigin = new Point(0.5, 0.5);
            ScaleTransform scaleTransform = new ScaleTransform();
            RenderTransform = scaleTransform;

            //return;
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
        }
    }

}
