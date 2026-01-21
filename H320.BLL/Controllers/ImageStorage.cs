using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using Util;
using System.Windows;
using System.Windows.Shapes;

namespace BoxAgr.BLL.Controllers
{
    public static class ImageStorage
    {
        public static string FindLatestBmpFile(string directoryPath, int horizon)
        {
            try
            {
                string[] bmpFiles = Directory.GetFiles(directoryPath, "*.bmp");

                if (bmpFiles.Length == 0)
                {
                    return string.Empty;
                }

                DateTime horizonTime = DateTime.Now.AddMilliseconds(-horizon);
                string? latestFile = bmpFiles
                    .Where(file => File.GetCreationTime(file) > horizonTime)
                    .OrderByDescending(file => File.GetCreationTime(file))
                    .FirstOrDefault();

                return latestFile ?? string.Empty;
            }
            catch (Exception ex)
            {
                Log.Write("ISTR", ex.ToString(), EventLogEntryType.Error);
                return string.Empty;
            }
        }

        //load image from bmp file to memory BitmapImage object and return him.
        //if file is not found, return null.
        public static BitmapImage? LoadImage(string filePath)
        {
            BitmapImage? image = null;

            try
            {
                image = new BitmapImage();
                image.BeginInit();
                image.UriSource = new Uri(filePath);
                image.EndInit();
            }
            catch (Exception ex)
            {
                Log.Write("ISTR", ex.ToString(), EventLogEntryType.Error);
            }
            return image;
        }

        public static Canvas? LoadImageWithSquares(string filePath, List<Point> codes)
        {
            try
            {
                // Загрузка изображения из файла
                BitmapImage bitmapImage = new BitmapImage(new Uri(filePath));

                // Создание объекта Canvas
                Canvas canvas = new Canvas();
                canvas.Width = bitmapImage.PixelWidth;
                canvas.Height = bitmapImage.PixelHeight;

                // Добавление загруженной картинки на Canvas
                Image image = new Image();
                image.Source = bitmapImage;
                canvas.Children.Add(image);

                // Отрисовка квадратиков
                foreach (Point point in codes)
                {
                    Rectangle rectangle = new Rectangle();
                    rectangle.Width = 20;
                    rectangle.Height = 20;
                    rectangle.Fill = Brushes.Green;
                    Canvas.SetLeft(rectangle, point.X - rectangle.Width / 2);
                    Canvas.SetTop(rectangle, point.Y - rectangle.Height / 2);
                    canvas.Children.Add(rectangle);
                }

                return canvas;
            }
            catch (Exception ex)
            {
                Log.Write(ex.ToString());
                return null;
            }
        }


        //LoadBmpImage loads a BMP image from the specified path into a Canvsas.
        public static Canvas? LoadBmpImage(string filePath)
        {
            try
            {
                BitmapImage bitmapImage = new BitmapImage(new Uri(filePath));
                Canvas canvas = new Canvas();
                canvas.Width = bitmapImage.PixelWidth;
                canvas.Height = bitmapImage.PixelHeight;
                Image image = new Image();
                image.Source = bitmapImage;
                Canvas.SetLeft(canvas, 0);
                Canvas.SetTop(canvas, 0);
                canvas.Children.Add(image);
                return canvas;
            } catch (Exception ex) 
            {
                Log.Write(ex.ToString());
            }
            return null;
        }
    }
}
