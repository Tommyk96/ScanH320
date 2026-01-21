using BoxAgr.BLL.Models;
using BoxAgr.BLL.UIElements;
using FSerialization;
using System;
using System.Collections.Generic;
//using System.Drawing;
//using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BoxAgr.BLL.Controllers
{
    public class MatrixController
    {
        public MatrixController() { }
        private static SolidColorBrush BadColor = new SolidColorBrush(Color.FromRgb(238, 95, 91));
        private static SolidColorBrush GoodColor = new SolidColorBrush(Color.FromRgb(0, 188, 140));
        private static SolidColorBrush NoReadColor = new SolidColorBrush(Color.FromRgb(56, 56, 56));
        private static SolidColorBrush ManualColor = new SolidColorBrush(Color.FromRgb(52, 152, 219));
        private static SolidColorBrush GridLineColor = new SolidColorBrush(Color.FromRgb(31, 31, 31));
        public static Canvas DrawGridWithImg(Unit[] units, int Width, int Height, int Rows, int Columns,string imgPath,bool imgShow,int angle)
        {

            // Создание объекта Canvas
            Canvas canvas = new Canvas();
            canvas.Width = Width;
            canvas.Height = Height;

            // Создаем RotateTransform и устанавливаем угол поворота на 90 градусов
            RotateTransform rotateTransform = new RotateTransform(angle);
            // Устанавливаем центр поворота в центр Canvas
            rotateTransform.CenterX = canvas.Width / 2;
            rotateTransform.CenterY = canvas.Height / 2;
            // Применяем трансформацию к Canvas
            //canvas.RenderTransform = rotateTransform;
            
            canvas.LayoutTransform = rotateTransform;

            //load and draw  picture
            if (imgShow && System.IO.File.Exists(imgPath))
            {
                Image img = new Image();
                img.Width = Width;
                img.Height = Height;
                img.Stretch = Stretch.UniformToFill;
                img.Source = new BitmapImage(new Uri(imgPath));
                canvas.Children.Add(img);
            }
            else
            {
                canvas.Width = Width;
                canvas.Height = Height;
            }

            //создать таблицу и елементы в ней
            Grid grid = DrawGrid(units, Width, Height, Rows, Columns, imgShow, canvas);
            canvas.Children.Add(grid);

          

            return canvas;
        }
        public static Grid DrawGrid(Unit[] units, int Width, int Height, int Rows, int Columns, bool drawGridLines = false, Canvas? image = null)
        {


            List<Unit>[,] cells = new List<Unit>[Columns, Rows];
            //добавленные вручную
            var manualUnits = units.Where(u => u.CodeState == FSerialization.CodeState.ManualAdd).ToList();

            // Инициализация массива cells
            for (int i = 0; i < Columns; i++)
            {
                for (int j = 0; j < Rows; j++)
                {
                    cells[i, j] = new List<Unit>();
                }
            }

            // Расчет ширины и высоты ячеек
            double cellWidth = Width / Columns;
            double cellHeight = Height / Rows;

            int columnIndex;
            int rowIndex;
            // Распределение кодов по ячейкам
            foreach (Unit unit in units)
            {
                //не добавлять в матрицу ручные коды. это будет сделано позднее
                if (unit.CodeState == FSerialization.CodeState.ManualAdd)
                    continue;

                columnIndex = Convert.ToInt32(Math.Floor(unit.X / cellWidth));
                rowIndex = Convert.ToInt32(Math.Floor(unit.Y / cellHeight));

                //если индекс ячейки вылезает за диапазон то сгенерировать сообщение ошибки
                if (rowIndex >= Rows || columnIndex >= Columns)
                    throw new Exception($"Код имеет позицию X {unit.X}, Y {unit.Y} за пределами допустимого диапазона матрици!\nУстановите короб так чтобы его левый верхний угол касался упора");

                cells[columnIndex, rowIndex].Add(unit);
            }

            //пройти по матрице и попытатся  перекинуть коды из ячеек где 2 кода в соседние пустые
            for (int j = 0; j < Rows; j++)
            {
                for (int i = 0; i < Columns; i++)
                {
                    //
                    if (cells[i, j].Count > 1)
                    {
                        //посмотреть есть ли слева пустая ячейка.
                        columnIndex = i - 1;
                        if (columnIndex >= 0)
                        {
                            if (cells[columnIndex, j].Count == 0)
                            {
                                //перекинуть код влево
                                //выбрать ктд с минимумом по X
                                var u = cells[i, j].OrderBy(x => x.X).First();
                                //перенести его в соседнюю ячейку
                                cells[columnIndex, j].Add(u);
                                //удалить из текущей.
                                cells[i, j].Remove(u);
                                continue;
                            }
                        }


                        //посмотреть если ли справа пустая ячейка
                        columnIndex = i + 1;
                        if (columnIndex < Columns)
                        {
                            if (cells[columnIndex, j].Count == 0)
                            {
                                //перекинуть код влево
                                //выбрать ктд с минимумом по X
                                var u = cells[i, j].OrderByDescending(x => x.X).First();
                                //перенести его в соседнюю ячейку
                                cells[columnIndex, j].Add(u);
                                //удалить из текущей.
                                cells[i, j].Remove(u);
                            }
                        }
                    }
                }
            }


            //создать таблицу
            Grid grid = new Grid() { ShowGridLines = !drawGridLines };
            grid.Width = Width;
            grid.Height = Height;

            for (int i = 0; i < Columns; i++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(cellWidth) });

                if (drawGridLines)
                {
                    GridSplitter mySimpleGridSplitter = new GridSplitter();
                    Grid.SetColumn(mySimpleGridSplitter, i);
                    Grid.SetRowSpan(mySimpleGridSplitter, Rows);
                    mySimpleGridSplitter.Background = GridLineColor;
                    mySimpleGridSplitter.HorizontalAlignment = HorizontalAlignment.Right;
                    mySimpleGridSplitter.VerticalAlignment = VerticalAlignment.Stretch;
                    mySimpleGridSplitter.Width = 5;
                    grid.Children.Add(mySimpleGridSplitter);
                }
            }

            for (int i = 0; i < Rows; i++)
            {
                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(cellHeight) });

                if (drawGridLines)
                {
                    GridSplitter mySimpleGridSplitter = new GridSplitter();
                    Grid.SetRow(mySimpleGridSplitter, i);
                    Grid.SetColumnSpan(mySimpleGridSplitter, Columns);
                    mySimpleGridSplitter.Background = GridLineColor;
                    mySimpleGridSplitter.HorizontalAlignment = HorizontalAlignment.Stretch;
                    mySimpleGridSplitter.VerticalAlignment = VerticalAlignment.Bottom;
                    mySimpleGridSplitter.Height = 5;
                    grid.Children.Add(mySimpleGridSplitter);
                }
            }


            //заполнить таблицу значениями
            bool showErrorBlock = false;
            if (units.Length > 0)
            {
                for (int j = 0; j < Rows; j++)
                {
                    for (int i = 0; i < Columns; i++)
                    {
                        showErrorBlock = false;
                        //пройтись по всем елементам ячейки и присаоить им цвет и другие атрибуты
                        foreach (Unit unit in cells[i, j])
                        {
                            //создать алерт
                            if (!drawGridLines && unit.CodeState != FSerialization.CodeState.Verify
                               && unit.CodeState != FSerialization.CodeState.ManualAdd)
                            {
                                InsertAlert(cellWidth, cellHeight, grid, j, i);
                                break;
                            } 
                            else if (drawGridLines
                                && unit.CodeState != FSerialization.CodeState.Verify
                                && unit.CodeState != FSerialization.CodeState.ManualAdd)
                            {
                                //draw triangle in image with x = unit.X and y= unit.Y
                                TriangleAlert allert = new();
                                allert.Width = 80;
                                allert.Height = 80;
                                Canvas.SetTop(allert, unit.Y - 40);
                                Canvas.SetLeft(allert, unit.X - 40);
                                continue;
                            }

                                  
                            if (!drawGridLines && cells[i, j].Count == 1 
                                && cells[i, j][0].CodeState == FSerialization.CodeState.Verify)
                            {
                                //Создание эллипс
                                DrawUnit(cells, cellWidth, cellHeight, grid, false, j, i, GoodColor); ;
                            }
                            else if (drawGridLines && unit.CodeState != FSerialization.CodeState.Verify)
                            {
                                ;// unit.Color = GoodColor;
                            }
                        }

                        //добавить в ячейку ручной код или норид или бад
                        if (!drawGridLines && cells[i, j].Count == 0)
                        {
                            //если есть код добавленный вручную расположить его на экране синим
                            if (manualUnits.FirstOrDefault() is Unit u)
                            {
                                //Создание эллипс
                                DrawUnit(cells, cellWidth, cellHeight, grid, false, j, i, ManualColor);
                                manualUnits.Remove(u);
                            }
                            else
                                DrawUnit(cells, cellWidth, cellHeight, grid, false, j, i, NoReadColor);
                        }
                        else if (!drawGridLines && cells[i, j].Count > 0)
                        {
                            //Создание эллипс
                            DrawUnit(cells, cellWidth, cellHeight, grid, true, j, i, BadColor);

                        }
                           
                        

                        //если выводится сетка то выводить метки по месту кода
                        if (drawGridLines && image is not null && cells[i, j] is not null)
                        {
                            foreach (Unit unit in cells[i, j])
                            {

                                if (unit.Points.Count == 4)
                                {
                                    Polygon polygon = CreatePolygon(unit.Points.ToArray());
                                    polygon.Fill = GetUnitColor(unit.CodeState);
                                    image.Children.Add(polygon);
                                }
                                else
                                {
                                    Rectangle rectangle = new Rectangle();
                                    rectangle.Width = unit.Width;
                                    rectangle.Height = unit.Width;
                                    rectangle.Fill = GetUnitColor(unit.CodeState);

                                    //Сканер HIK дает координату верхнего левого угла
                                    Canvas.SetLeft(rectangle, unit.X);
                                    Canvas.SetTop(rectangle, unit.Y);

                                    //сканер ДЛ дет координату центра. 
                                    //Canvas.SetLeft(rectangle, unit.X - rectangle.Width / 2);
                                    //Canvas.SetTop(rectangle, unit.Y - rectangle.Height / 2);

                                    image.Children.Add(rectangle);
                                }
                            }
                        }
                    }
                }
            }


            return grid;
        }

        public static Polygon CreatePolygon(UnitPoint[] points)
        {
            if (points.Length != 4)
            {
                throw new ArgumentException("Input array must contain exactly 4 UnitPoint elements.");
            }

            Polygon polygon = new Polygon
            {
                Stroke = Brushes.Black,
                StrokeThickness = 2,
                Fill = Brushes.Transparent
            };

            PointCollection pointCollection = new PointCollection();
            foreach (var point in points)
            {
                pointCollection.Add(new Point(point.X, point.Y));
            }

            polygon.Points = pointCollection;
            return polygon;
        }

        private static SolidColorBrush GetUnitColor(CodeState codeState)
        {
            return codeState switch { 
                CodeState.ManualAdd => ManualColor,
                CodeState.Verify => GoodColor,
                _=> BadColor
            };
        }

        private static void DrawUnit(List<Unit>[,] cells, double cellWidth, double cellHeight, Grid grid, bool showErrorBlock, int j, int i, SolidColorBrush fColor)
        {
            Ellipse ellipse = new Ellipse();
            ellipse.Width = (cellWidth > cellHeight ? cellHeight : cellWidth) * 0.9;
            ellipse.Height = ellipse.Width;
            Grid.SetColumn(ellipse, i);
            Grid.SetRow(ellipse, j);
            ellipse.Fill = fColor;
            grid.Children.Add(ellipse);

            if (showErrorBlock)
            {

                TextBlock label = new TextBlock();
                label.Text = cells[i, j].Count.ToString();
                label.HorizontalAlignment = HorizontalAlignment.Center;
                label.VerticalAlignment = VerticalAlignment.Center;
                int w = (int)(ellipse.Width * 0.8);

                label.FontSize = 10;// FindMaxFontSize(w,w, label.Text);
                label.Foreground = Brushes.White;
                label.FontWeight = FontWeights.Bold;

                Viewbox viewbox = new Viewbox() { Height = w, Width = w };
                viewbox.HorizontalAlignment = HorizontalAlignment.Center;
                viewbox.VerticalAlignment = VerticalAlignment.Center;
                viewbox.Child = label;

                Grid.SetColumn(viewbox, i);
                Grid.SetRow(viewbox, j);
                grid.Children.Add(viewbox);
            }
        }

        private static void InsertAlert(double cellWidth, double cellHeight, Grid grid, int j, int i)
        {
            TriangleAlert allert = new();
            allert.Width = (cellWidth > cellHeight ? cellHeight : cellWidth) * 0.9;
            allert.Height = allert.Width;

            Grid.SetColumn(allert, i);
            Grid.SetRow(allert, j);
            grid.Children.Add(allert);
        }

        // Метод для поиска максимального размера шрифта
        private static int FindMaxFontSize(int squareWidth, int squareHeight, string text)
        {
            int fontSize = 8; // Начальный размер шрифта

            while (true)
            {
                TextBlock textBlock = new TextBlock();
                textBlock.Text = text;
                textBlock.FontSize = fontSize;

                textBlock.Measure(new Size(squareWidth, squareHeight));

                if (textBlock.DesiredSize.Width > squareWidth || textBlock.DesiredSize.Height > squareHeight)
                {
                    fontSize--;
                    break;
                }

                fontSize++;
                if (fontSize > 200)
                    break;
            }

            return fontSize;
        }

        //public static Canvas DrawTable1213(Unit[] units, int Width, int Height, int Rows, int Columns)
        //{
        //    // Создание объекта Canvas
        //    Canvas canvas = new Canvas();
        //    canvas.Width = Width;
        //    canvas.Height = Height;

        //    //ничего не делаеть если массив пуст
        //    //if (units.Length < 1)
        //    //    return canvas;


        //    // Расчет ширины и высоты ячеек
        //    double cellWidth = Width / Columns;
        //    double cellHeight = Height / Rows;

        //    // Отрисовка таблицы
        //    for (int row = 0; row < Rows; row++)
        //    {
        //        for (int col = 0; col < Columns; col++)
        //        {
        //            // Создание и настройка прямоугольника ячейки
        //            Rectangle rect = new Rectangle();
        //            rect.StrokeThickness = 4;
        //            rect.Stroke = System.Windows.Media.Brushes.White;
        //            rect.Width = cellWidth;
        //            rect.Height = cellHeight;
        //            rect.SetValue(Canvas.TopProperty, row * cellHeight);
        //            rect.SetValue(Canvas.LeftProperty, col * cellWidth);

        //            // Добавление ячейки на Canvas
        //            canvas.Children.Add(rect);
        //        }
        //    }

        //    //создать матрицу
        //    Models.Matrix.Matrix matrix = new Models.Matrix.Matrix();
        //    matrix.Column = Columns;
        //    matrix.Row = Rows;
        //    matrix.CreateFromData(units);
        //    //matrix.Cells.Sort(new MatrixItemCellInLineComparer(Height));

        //    //matrix.Cells.Sort(new MatrixItemCellInColumnComparer(Width));

        //    //создать таблицу из массива
        //    //Unit[] units2 = AssignUnitsToCells(units, Rows, Columns);

        //    if (units.Length > 0)
        //    {
        //        //берем код с самым большьим значением х и считаем что это максимальный размер короба
        //        int w = units.Max(x => x.X);

        //        //раскидываем елементы по рядам. 
        //        Unit? lu = null;
        //        int rowId = 0;
        //        foreach (Unit unit in units)
        //        {
        //            if (lu is null || lu?.X > unit.X)
        //                rowId++;
        //            unit.CellRowId = rowId;
        //            unit.CellColumId = (int)(unit.X / cellWidth);

        //            lu = unit;
        //        }

        //        //matrix.Cells.Sort(new MatrixItemCellInColumnComparer(Width));
        //        //        public class Unit
        //        //{

        //        //    public int x { get; set; }
        //        //    public int y { get; set; }
        //        //}
        //        //Unit[] units = new Unit[5] 
        //        //    { 
        //        //        new Unit() { X = 10, Y = 20 },
        //        //        new Unit() { X = 20, Y = 10 },
        //        //        new Unit() { X = 30, Y = 20 },
        //        //        new Unit() { X = 10, Y = 10 },
        //        //        new Unit() { X = 20, Y = 20 },
        //        //    }




        //        int uSize = 40;
        //        // Отрисовка элементов из массива units
        //        foreach (Unit unit in units)
        //        {
        //            //для дебага отрисовываем точки как есть
        //            Rectangle rect = new Rectangle()
        //            {
        //                Width = uSize,
        //                Height = uSize,
        //                Fill = Brushes.Plum
        //            };

        //            Canvas.SetLeft(rect, unit.X);
        //            Canvas.SetTop(rect, unit.Y);
        //            canvas.Children.Add(rect);
        //            // Рассчет позиции элемента на таблице
        //            //int rowIndex = (int)(unit.Y / cellHeight);
        //            //int colIndex = (int)(unit.X / cellWidth);

        //            int cellRow = unit.CellRowId - 1;
        //            int cellCol = unit.CellColumId - 1;

        //            if (cellRow >= 0 && cellRow < Rows && cellCol >= 0 && cellCol < Columns)
        //            {
        //                // Создание и настройка эллипса (вместо эллипса можно использовать другой элемент)
        //                Ellipse ellipse = new Ellipse();
        //                ellipse.Width = (cellWidth > cellHeight ? cellHeight : cellWidth) - 10;
        //                ellipse.Height = ellipse.Width;
        //                ellipse.Fill = new SolidColorBrush(Color.FromRgb(238, 95, 91));
        //                ellipse.SetValue(Canvas.TopProperty, (cellRow * cellHeight) + 5);
        //                ellipse.SetValue(Canvas.LeftProperty, (cellRow * cellWidth) + (cellWidth / 2) - (ellipse.Width / 2));

        //                // Добавление элемента на Canvas
        //                canvas.Children.Add(ellipse);
        //            }
        //        }
        //    }
        //    return canvas;
        //}


        //public static Canvas DrawTable123(Unit[] units, int Width, int Height, int Rows, int Columns)
        //{
        //    Canvas canvas = new Canvas();
        //    canvas.Width = Width;
        //    canvas.Height = Height;
        //    int boxWidth = 30;
        //    double halfBoxWidth = boxWidth / 2.0d;

        //    foreach (Unit unit in units)
        //    {
        //        Rectangle rect = new Rectangle()
        //        {
        //            Width = boxWidth,
        //            Height = boxWidth,
        //            Fill = Brushes.AliceBlue
        //        };

        //        Canvas.SetLeft(rect, unit.X);
        //        Canvas.SetTop(rect, unit.Y);

        //        canvas.Children.Add(rect);
        //    }

        //    // Разделение элементов линиями по горизонтали
        //    for (int i = 0; i < units.Length - 1; i++)
        //    {
        //        Line line = new Line()
        //        {
        //            X1 = units[i].X + halfBoxWidth,
        //            Y1 = units[i].Y + halfBoxWidth,
        //            X2 = units[i + 1].X + halfBoxWidth,
        //            Y2 = units[i + 1].Y + halfBoxWidth,
        //            Stroke = Brushes.Red,
        //            StrokeThickness = 2
        //        };

        //        canvas.Children.Add(line);
        //    }

        //    // Разделение элементов линиями по вертикали
        //    for (int i = 0; i < units.Length - 1; i++)
        //    {
        //        Line line = new Line()
        //        {
        //            X1 = units[i].X + halfBoxWidth,
        //            Y1 = units[i].Y + halfBoxWidth,
        //            X2 = units[i].X + halfBoxWidth,
        //            Y2 = units[i + 1].Y + halfBoxWidth,
        //            Stroke = Brushes.Red,
        //            StrokeThickness = 2
        //        };

        //        canvas.Children.Add(line);
        //    }

        //    return canvas;
        //}
        //public static Canvas DrawTable7(Unit[] units, int Width, int Height, int Rows, int Columns)
        //{
        //    Canvas canvas = new Canvas();
        //    canvas.Width = Width;
        //    canvas.Height = Height;

        //    // Определяем ширину и высоту ячейки
        //    double cellWidth = 0;
        //    double cellHeight = 0;
        //    double uSize = 20;
        //    // Находим максимальную ширину и высоту квадрата
        //    foreach (Unit unit in units)
        //    {
        //        if (unit.X + uSize > cellWidth)
        //            cellWidth = unit.X + uSize;

        //        if (unit.Y + uSize > cellHeight)
        //            cellHeight = unit.Y + uSize;
        //    }

        //    // Добавляем небольшой отступ для ячеек
        //    cellWidth += 10;
        //    cellHeight += 10;

        //    for (int i = 0; i < units.Length; i++)
        //    {
        //        Unit unit = units[i];

        //        Rectangle rect = new Rectangle()
        //        {
        //            Width = uSize,
        //            Height = uSize,
        //            Fill = Brushes.Plum
        //        };

        //        Canvas.SetLeft(rect, unit.X);
        //        Canvas.SetTop(rect, unit.Y);

        //        canvas.Children.Add(rect);

        //        // Рисуем линии по горизонтали
        //        if (i < units.Length - 1)
        //        {
        //            Line lineHorizontal = new Line()
        //            {
        //                X1 = unit.X + uSize,
        //                Y1 = unit.Y + uSize / 2,
        //                X2 = units[i + 1].X,
        //                Y2 = unit.Y + uSize / 2,
        //                Stroke = Brushes.Red,
        //                StrokeThickness = 2
        //            };

        //            canvas.Children.Add(lineHorizontal);
        //        }

        //        // Рисуем линии по вертикали
        //        if (unit.X > 0)
        //        {
        //            Line lineVertical = new Line()
        //            {
        //                X1 = unit.X + uSize / 2,
        //                Y1 = unit.Y,
        //                X2 = unit.X + uSize / 2,
        //                Y2 = unit.Y - uSize,
        //                Stroke = Brushes.Red,
        //                StrokeThickness = 2
        //            };

        //            canvas.Children.Add(lineVertical);
        //        }
        //    }

        //    return canvas;
        //}

        //public static Canvas DrawTable0(Unit[] units, int Width, int Height, int Rows, int Columns)
        //{
        //    int maxY = 0;

        //    // Находим максимальную координату по Y
        //    foreach (var unit in units)
        //    {
        //        if (unit.Y > maxY)
        //            maxY = unit.Y;
        //    }

        //    // Группируем точки по рядам
        //    var groupedUnits = units.GroupBy(u => u.Y).ToList();

        //    // Определяем количество столбцов в таблице
        //    int columns = groupedUnits.Max(g => g.Count());

        //    // Создаем виртуальную таблицу
        //    int[,] table = new int[columns, maxY + 1];

        //    // Заполняем таблицу индексами ячеек, в которые попадают точки
        //    for (int row = 0; row < groupedUnits.Count; row++)
        //    {
        //        int column = 0;
        //        foreach (var unit in groupedUnits[row])
        //        {
        //            table[column, row] = 1; // Можно использовать любое значение, которое отображает попадание точки в ячейку
        //            column++;
        //        }
        //    }

        //    return null;
        //}


        //public static Unit[] AssignUnitsToCells(Unit[] units, int Rows, int Columns)
        //{
        //    // Создаем двумерный массив для хранения ячеек таблицы
        //    //Cell[,] cells = new Cell[Rows, Columns];

        //    //попытатся вычислить размер колонки
        //    //берем код с самым большьим значением х
        //    int w = units.Max(x => x.X);
        //    //предпологаем что данный код находится в последнем ряду.....

        //    List<Unit> u = new (units.Length);

        //    //раскидываем елементы по рядам. НЕ по колонкам! 
        //    Unit? lu = null;
        //    int rowId = 0;
        //    foreach (Unit unit in units)
        //    {
        //        if (lu is null || lu?.X > unit.X)
        //            rowId++;
        //        unit.CellRowId = rowId;
        //        lu = unit;
        //        u.Add(unit);
        //    }

        //    // Проходим по всем объектам Unit
        //    //foreach (Unit unit in units)
        //    //{
        //    //    // Вычисляем индексы ячейки на основе координат объекта
        //    //    int rowIndex = unit.Y / (int)(unit.Y / Rows);
        //    //    int columnIndex = unit.X / (int)(unit.X / Columns);

        //    //    // Создаем новую ячейку и присваиваем ей объект Unit
        //    //    unit.CellRowId = rowIndex;
        //    //    unit.CellColumId = columnIndex;
        //    //    //Cell cell = new Cell(unit);
        //    //    //cells[rowIndex, columnIndex] = cell;
        //    //    u.Add(unit);
        //    //}
        //    return u.ToArray();
        //}

        //public static Canvas DrawTable(Unit[] units, int Width, int Height, int Rows, int Columns)
        //{
        //    Canvas canvas = new Canvas();
        //    canvas.Width = Width;
        //    canvas.Height = Height;

        //    double cellWidth = Width / Columns;
        //    double cellHeight = Height / Rows;

        //    for (int row = 0; row < Rows; row++)
        //    {
        //        for (int col = 0; col < Columns; col++)
        //        {
        //            Rectangle rect = new Rectangle();
        //            rect.Width = cellWidth;
        //            rect.Height = cellHeight;
        //            rect.Stroke = Brushes.Black;
        //            rect.Fill = Brushes.White;

        //            Canvas.SetLeft(rect, col * cellWidth);
        //            Canvas.SetTop(rect, row * cellHeight);

        //            canvas.Children.Add(rect);
        //        }
        //    }

        //    foreach (Unit unit in units)
        //    {
        //        int cellX = (int)(unit.X / cellWidth);
        //        int cellY = (int)(unit.Y / cellHeight);

        //        if (cellX < Columns && cellY < Rows)
        //        {
        //            Ellipse circle = new Ellipse();
        //            circle.Width = (cellWidth > cellHeight ? cellHeight : cellWidth) - 10;
        //            circle.Height = circle.Width;
        //            circle.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
        //            circle.VerticalAlignment = System.Windows.VerticalAlignment.Center;

        //            switch (unit.CodeState)
        //            {
        //                case FSerialization.CodeState.Verify:
        //                    circle.Fill = Brushes.Green;
        //                    break;
        //                case FSerialization.CodeState.ProductRepit:
        //                    circle.Fill = Brushes.Blue;
        //                    break;
        //                default:
        //                    circle.Fill = Brushes.Red;
        //                    break;
        //            }

        //            Canvas.SetLeft(circle, cellX * cellWidth + (cellWidth / 2) - (circle.Width / 2));
        //            Canvas.SetTop(circle, (cellY * cellHeight) + 5);

        //            canvas.Children.Add(circle);
        //        }
        //    }

        //    return canvas;
        //}




        //public static Canvas DrawTable(Unit[] units, int Width, int Height, int Rows, int Columns)
        //{
        //    Canvas canvas = new Canvas();
        //    canvas.Width = Width;
        //    canvas.Height = Height;

        //    double cellWidth = Width / Columns;
        //    double cellHeight = Height / Rows;

        //    //создать матрицу
        //    Models.Matrix.Matrix matrix = new Models.Matrix.Matrix();
        //    matrix.Column = Columns;
        //    matrix.Row = Rows;

        //    //int height = 1280;
        //    //int width = 1024;

        //    //координаты короба
        //    //int centerBoxX = 531;
        //    //int centerBoxY = 598;
        //    //int widhtBox = 738;
        //    //int heightBox = 684;

        //    // посчитать размер коробки и создать эталонную матрицу
        //    //Matrix boxMatrix = new Matrix() {  };
        //    List<System.Windows.Rect> etalonMatrix = new List<System.Windows.Rect>();

        //    try
        //    {
        //        for (int row = 0; row < Rows; row++)
        //        {
        //            for (int col = 0; col < Columns; col++)
        //            {
        //                Rectangle rect = new Rectangle();
        //                rect.Width = cellWidth;
        //                rect.Height = cellHeight;
        //                rect.Stroke = Brushes.Black;
        //                rect.Fill = Brushes.White;

        //                Canvas.SetLeft(rect, col * cellWidth);
        //                Canvas.SetTop(rect, row * cellHeight);

        //                canvas.Children.Add(rect);
        //                etalonMatrix.Add(new System.Windows.Rect(col * cellWidth, row * cellHeight, cellWidth, cellHeight));
        //            }
        //        }


        //        matrix.Cells.Clear();
        //        //распарсить данные 
        //        matrix.CreateFromData(units);
        //        //отсортировать по х

        //        matrix.Cells.Sort(new MatrixItemCellInLineComparer(Height, Width));


        //        double h = Width / matrix.Row;
        //        double w = Height / matrix.Column;



        //        #region Создать матрицу короба
        //        //    try
        //        //    {
        //        //        etalonMatrix.Clear();
        //        //        int colOffset = Properties.Settings.Default.CodeLocBoxWidht / Properties.Settings.Default.CodeLocBoxColum;
        //        //        int rowOffset = Properties.Settings.Default.CodeLocBoxHeight / Properties.Settings.Default.CodeLocBoxRow;

        //        //        int colPos = Properties.Settings.Default.CodeLocBoxCentrX - (Properties.Settings.Default.CodeLocBoxWidht / 2);
        //        //        int rowPos = Properties.Settings.Default.CodeLocBoxCentrY - (Properties.Settings.Default.CodeLocBoxHeight / 2);
        //        //        int cRowPos = 0;
        //        //        //for debug


        //        //        for (int col = 1; col <= Properties.Settings.Default.CodeLocBoxColum; col++)
        //        //        {
        //        //            cRowPos = rowPos;

        //        //            for (int row = 1; row <= Properties.Settings.Default.CodeLocBoxRow; row++)
        //        //            {
        //        //                Rectangle r = new Rectangle(colPos, cRowPos, colOffset, rowOffset);
        //        //                etalonMatrix.Add(r);
        //        //                cRowPos = cRowPos + rowOffset;
        //        //            }
        //        //            colPos += colOffset;
        //        //        }
        //        //    }
        //        //    catch (Exception e)
        //        //    {

        //        //    }

        //        ////отрисовать матрицу
        //        //for (int a = 0; a < etalonMatrix.Count; a++)
        //        //    graphics.DrawRectangles(redPen, etalonMatrix.ToArray());

        //        #endregion

        //        #region Раскидать канисты по матрице
        //        Array.Sort(units, new UnitXYComparer(Width, Height));

        //        for (int i = 0; i < units.Length; i++)
        //        {
        //            //найти для данного кода ячейку
        //            for (int ri = 0; ri < etalonMatrix.Count; ri++)
        //            {
        //                if (etalonMatrix[ri].Contains(units[i].X, units[i].Y))
        //                {
        //                    Ellipse circle = new Ellipse();
        //                    circle.Width = (cellWidth > cellHeight ? cellHeight : cellWidth) - 10;
        //                    circle.Height = circle.Width;
        //                    circle.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
        //                    circle.VerticalAlignment = System.Windows.VerticalAlignment.Center;

        //                    switch (units[i].CodeState)
        //                    {
        //                        case FSerialization.CodeState.Verify:
        //                            circle.Fill = Brushes.Green;
        //                            break;
        //                        case FSerialization.CodeState.ProductRepit:
        //                            circle.Fill = Brushes.Blue;
        //                            break;
        //                        default:
        //                            circle.Fill = Brushes.Red;
        //                            break;
        //                    }
        //                    Canvas.SetLeft(circle, etalonMatrix[ri].Left * cellWidth + (cellWidth / 2) - (circle.Width / 2) );
        //                    Canvas.SetTop(circle, (etalonMatrix[ri].Top * cellHeight) + 5);
        //                    //Canvas.SetLeft(circle, cellX * cellWidth + (cellWidth / 2) - (circle.Width / 2));
        //                    //Canvas.SetTop(circle, (cellY * cellHeight) + 5);

        //                    canvas.Children.Add(circle);
        //                }
        //            }
        //        }
        //        #endregion

        //    }
        //    catch (Exception ex) { ex.ToString(); }

        //    return canvas;
        //}
    }
}
