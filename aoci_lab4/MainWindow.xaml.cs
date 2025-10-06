using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Emgu.CV.Structure;
using Emgu.CV;
using System.Globalization;
using System.Windows.Controls;

namespace aoci_lab4
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Image<Bgr, byte> sourceImage;

        public MainWindow()
        {
            InitializeComponent();
        }

        public BitmapSource ToBitmapSource(Image<Bgr, byte> image)
        {
            var mat = image.Mat;

            return BitmapSource.Create(
                mat.Width,
                mat.Height,
                96d,
                96d,
                PixelFormats.Bgr24,
                null,
                mat.DataPointer,
                mat.Step * mat.Height,
                mat.Step);
        }
        public Image<Bgr, byte> ToEmguImage(BitmapSource source)
        {
            if (source == null) return null;

            FormatConvertedBitmap safeSource = new FormatConvertedBitmap();
            safeSource.BeginInit();
            safeSource.Source = source;
            safeSource.DestinationFormat = PixelFormats.Bgr24;
            safeSource.EndInit();

            Image<Bgr, byte> resultImage = new Image<Bgr, byte>(safeSource.PixelWidth, safeSource.PixelHeight);
            var mat = resultImage.Mat;

            safeSource.CopyPixels(
                new System.Windows.Int32Rect(0, 0, safeSource.PixelWidth, safeSource.PixelHeight),
                mat.DataPointer,
                mat.Step * mat.Height,
                mat.Step);

            return resultImage;
        }

        private void LoadImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Файлы изображений (*.jpg, *.jpeg, *.png)|*.jpg;*.jpeg;*.png";
            if (openFileDialog.ShowDialog() == true)
            {
                sourceImage = new Image<Bgr, byte>(openFileDialog.FileName);

                MainImage.Source = ToBitmapSource(sourceImage);
            }
        }

        private void SaveImage_Click(object sender, RoutedEventArgs e)
        {
            BitmapSource currentWpfImage = MainImage.Source as BitmapSource;
            if (currentWpfImage == null)
            {
                MessageBox.Show("Отсутсвует изображение");
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "PNG Image (*.png)|*.png|JPEG Image (*.jpg)|*.jpg|Bitmap Image (*.bmp)|*.bmp";

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    Image<Bgr, byte> imageToSave = ToEmguImage(currentWpfImage);
                    imageToSave.Save(saveFileDialog.FileName);

                    MessageBox.Show($"Изображение успешно сохранено в {saveFileDialog.FileName}");
                }
                catch (Exception ex)
                {

                    MessageBox.Show($"Ошибка! Не могу сохранить файл. Подробности: {ex.Message}");
                }
            }
        }

        private void UpdateImage_Click(object sender, RoutedEventArgs e)
        {
            BitmapSource currentWpfImage = MainImage.Source as BitmapSource;

            if (currentWpfImage == null)
            {
                MessageBox.Show("Изображение отсутсвует");
                return;
            }

            sourceImage = ToEmguImage(currentWpfImage);
            MessageBox.Show("Изменения применены. Теперь это новый оригинал.");
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            if (sourceImage == null) return;
            MainImage.Source = ToBitmapSource(sourceImage);
        }


        //--- Функция Свертки ---

        //Функция применяет операцию свертки к цветному изображению.
        //Свертка — это процесс, где новый цвет каждого пикселя вычисляется как взвешенная сумма цветов его соседей.
        //Веса задаются ядром (матрицей (kernel)). Это основа для множества эффектов: размытия, повышения резкости, выделения границ и т.д.
        private Image<Bgr, byte> ApplyConvolution(Image<Bgr, byte> input, double[,] kernel)
        {
            int kernelSize = kernel.GetLength(0); //Размер ядра, в нашем случае 3 для матрицы 3x3.
            int kernelRadius = kernelSize / 2; //Радиус ядра, в нашем случае 1 для ядра 3x3.

            //Мы создаем клон, потому что для расчета каждого нового пикселя нужны ОРИГИНАЛЬНЫЕ значения соседних пикселей
            Image<Bgr, byte> output = input.Clone();

            //Основной цикл проходит по всем пикселям, которые могут быть центром ядра, не выходя за границы изображения. Поэтому мы "пропускаем" края.
            for (int y = kernelRadius; y < input.Height - kernelRadius; y++)
            {
                for (int x = kernelRadius; x < input.Width - kernelRadius; x++)
                {
                    //Сумма для каждого цветового канала.
                    double sumR = 0, sumG = 0, sumB = 0;

                    //Внутренние циклы проходят по "окну" соседей, размер которого соответствует размеру ядра.
                    for (int ky = -kernelRadius; ky <= kernelRadius; ky++)
                    {
                        for (int kx = -kernelRadius; kx <= kernelRadius; kx++)
                        {
                            //Получаем цвет соседнего пикселя.
                            Bgr neighborPixel = input[y + ky, x + kx];

                            //Получаем соответствующее значение (вес) из ядра.
                            double kernelValue = kernel[ky + kernelRadius, kx + kernelRadius];

                            //Умножаем цвет соседа на вес из ядра и прибавляем к общей сумме.
                            sumR += neighborPixel.Red * kernelValue;
                            sumG += neighborPixel.Green * kernelValue;
                            sumB += neighborPixel.Blue * kernelValue;
                        }
                    }

                    //Записываем результат в выходное изображение.
                    output[y, x] = new Bgr(
                        (byte)Math.Max(0, Math.Min(255, sumB)),
                        (byte)Math.Max(0, Math.Min(255, sumG)),
                        (byte)Math.Max(0, Math.Min(255, sumR))
                    );
                }
            }

            return output;
        }

        //Перегруженная версия ApplyConvolution для изображений в градациях серого.
        private Image<Gray, float> ApplyConvolution(Image<Gray, byte> input, double[,] kernel)
        {
            int kernelSize = kernel.GetLength(0);
            int kernelRadius = kernelSize / 2;

            Image<Gray, float> output = new Image<Gray, float>(input.Size);

            for (int y = kernelRadius; y < input.Height - kernelRadius; y++)
            {
                for (int x = kernelRadius; x < input.Width - kernelRadius; x++)
                {
                    double sum = 0;

                    for (int ky = -kernelRadius; ky <= kernelRadius; ky++)
                    {
                        for (int kx = -kernelRadius; kx <= kernelRadius; kx++)
                        {
                            //Прямой доступ к данным изображения. `[y, x, 0]` - 0 означает первый (и единственный) канал т.к. изображение в градациях серого.
                            byte neighborPixel = input.Data[y + ky, x + kx, 0];
                            double kernelValue = kernel[ky + kernelRadius, kx + kernelRadius];
                            sum += neighborPixel * kernelValue;
                        }
                    }

                    //Здесь мы НЕ зажимаем значение в [0, 255], а сохраняем полный результат для дальнейшей обработки (например, нормализации).
                    output.Data[y, x, 0] = (float)sum;
                }
            }
            return output;
        }

        //--- Примеры фильтров на основе свертки ---


        //Применяет простое размытие (Box Blur).
        //Ядро состоит из одинаковых значений. Это равносильно замене каждого пикселя на среднее арифметическое его и 8-ми соседей.
        //Дает "квадратный" эффект размытия. Сумма всех элементов ядра равна 1, чтобы сохранить общую яркость изображения.
        private void BoxBlur_Click(object sender, RoutedEventArgs e)
        {
            if (sourceImage == null) return;

            double[,] kernel = {
                { 1.0/9, 1.0/9, 1.0/9 },
                { 1.0/9, 1.0/9, 1.0/9 },
                { 1.0/9, 1.0/9, 1.0/9 }
            };

            Image<Bgr, byte> bluredImage = ApplyConvolution(sourceImage, kernel);

            MainImage.Source = ToBitmapSource(bluredImage);
        }

        //Применяет размытие по Гауссу.
        //Это более качественное размытие. Веса в ядре распределены по функции Гаусса: центральный пиксель имеет наибольший вес,
        //а соседи — тем меньший, чем дальше они от центра. Дает более плавный и естественный результат. Сумма ядра также равна 1.
        private void GaussianBlur_Click(object sender, RoutedEventArgs e)
        {
            if (sourceImage == null) return;

            double[,] kernel = {
                { 1.0/16, 2.0/16, 1.0/16 },
                { 2.0/16, 4.0/16, 2.0/16 },
                { 1.0/16, 2.0/16, 1.0/16 }
            };

            Image<Bgr, byte> gaussianBluredImage  = ApplyConvolution(sourceImage, kernel);

            MainImage.Source = ToBitmapSource(gaussianBluredImage);

        }

        //Применяет фильтр повышения резкости (Sharpen).
        //Ядро работает по принципу "нерезкого маскирования": центральный пиксель усиливается, а его размытая версия (представленная отрицательными весами) вычитается.
        //Это увеличивает локальный контраст на границах объектов. Сумма ядра (9 - 8) равна 1.
        private void Sharpen_Click(object sender, RoutedEventArgs e)
        {
            if (sourceImage == null) return;

            double[,] kernel = {
                { -1, -1, -1 },
                { -1,  9, -1 },
                { -1, -1, -1 }
            };

            Image<Bgr, byte> sharpenImage = ApplyConvolution(sourceImage, kernel);

            MainImage.Source = ToBitmapSource(sharpenImage);
        }

        //--- Выделение границ (Оператор Собеля) ---
        // Выделяет вертикальные границы с помощью оператора Собеля.

        private void SobelX_Click(object sender, RoutedEventArgs e)
        {
            if (sourceImage == null) return;

            // Выделение границ работает с яркостью, а не с цветом. Конвертируем в серое.
            Image<Gray, byte> grayImage = sourceImage.Convert<Gray, byte>();
            Image<Bgr, byte> resultImage;

            //Ядро Собеля для оси X. Оно реагирует на изменения яркости по горизонтали (вертикальные линии).
            double[,] kernelX = {
                { -1, 0, 1 },
                { -2, 0, 2 },
                { -1, 0, 1 }
            };

            //Применяем свертку. Результат будет в float-изображении (градиент).
            Image<Gray, float> gradientX = ApplyConvolution(grayImage, kernelX);

            Image<Gray, float> magnitude = new Image<Gray, float>(grayImage.Size);

            //Нормализация для отображения.
            //Градиент содержит отрицательные (<0) и большие значения (>255), которые нельзя просто показать.
            //Мы растягиваем диапазон значений градиента на обрабатываемый диапазон [0, 255].
            for (int y = 0; y < magnitude.Height; y++)
            {
                for (int x = 0; x < magnitude.Width; x++)
                {
                    float gx = Math.Abs(gradientX.Data[y, x, 0]);
                    magnitude.Data[y, x, 0] = gx;
                }
            }

            //Находим максимальное значение градиента на изображении.
            double maxVal = 0;

            for (int y = 0; y < magnitude.Height; y++)
            {
                for (int x = 0; x < magnitude.Width; x++)
                {
                    if (magnitude.Data[y, x, 0] > maxVal)
                    {
                        maxVal = magnitude.Data[y, x, 0];
                    }
                }
            }

            //Масштабируем изображение так, чтобы maxVal стал 255.
            Image<Gray, byte> normalizedMagnitude = magnitude.ConvertScale<byte>(255.0 / maxVal, 0);
            resultImage = normalizedMagnitude.Convert<Bgr, byte>();

            MainImage.Source = ToBitmapSource(resultImage);
        }

        //Выделяет горизонтальные границы с помощью оператора Собеля.
        private void SobelY_Click(object sender, RoutedEventArgs e)
        {
            if (sourceImage == null) return;

            Image<Gray, byte> grayImage = sourceImage.Convert<Gray, byte>();
            Image<Bgr, byte> resultImage;

            //Ядро Собеля для оси Y. Оно реагирует на изменения яркости по вертикали (горизонтальные линии).
            double[,] kernelY = {
                { -1, -2, -1 },
                {  0,  0,  0 },
                {  1,  2,  1 }
            };

            Image<Gray, float> gradientY = ApplyConvolution(grayImage, kernelY);

            Image<Gray, float> magnitude = new Image<Gray, float>(grayImage.Size);

            for (int y = 0; y < magnitude.Height; y++)
            {
                for (int x = 0; x < magnitude.Width; x++)
                {
                    float gy = Math.Abs(gradientY.Data[y, x, 0]);
                    magnitude.Data[y, x, 0] = gy;
                }
            }

            double maxVal = 0;

            for (int y = 0; y < magnitude.Height; y++)
            {
                for (int x = 0; x < magnitude.Width; x++)
                {
                    if (magnitude.Data[y, x, 0] > maxVal)
                    {
                        maxVal = magnitude.Data[y, x, 0];
                    }
                }
            }

            Image<Gray, byte> normalizedMagnitude = magnitude.ConvertScale<byte>(255.0 / maxVal, 0);
            resultImage = normalizedMagnitude.Convert<Bgr, byte>();
            MainImage.Source = ToBitmapSource(resultImage);
        }

        //Выделяет все границы, комбинируя градиенты по X и Y.
        private void SobelXY_Click(object sender, RoutedEventArgs e)
        {
            if (sourceImage == null) return;

            Image<Gray, byte> grayImage = sourceImage.Convert<Gray, byte>();
            Image<Bgr, byte> resultImage;

            double[,] kernelX = {
                { -1, 0, 1 },
                { -2, 0, 2 },
                { -1, 0, 1 }
            };

            double[,] kernelY = {
                { -1, -2, -1 },
                {  0,  0,  0 },
                {  1,  2,  1 }
            };

            Image<Gray, float> gradientX = ApplyConvolution(grayImage, kernelX);
            Image<Gray, float> gradientY = ApplyConvolution(grayImage, kernelY);

            Image<Gray, float> magnitude = new Image<Gray, float>(grayImage.Size);

            for (int y = 0; y < magnitude.Height; y++)
            {
                for (int x = 0; x < magnitude.Width; x++)
                {
                    float gx = Math.Abs(gradientX.Data[y, x, 0]);
                    float gy = Math.Abs(gradientY.Data[y, x, 0]);
                    //Результат складывается.
                    magnitude.Data[y, x, 0] = gx + gy;
                }
            }

            double maxVal = 0;

            for (int y = 0; y < magnitude.Height; y++)
            {
                for (int x = 0; x < magnitude.Width; x++)
                {
                    if (magnitude.Data[y, x, 0] > maxVal)
                    {
                        maxVal = magnitude.Data[y, x, 0];
                    }
                }
            }

            Image<Gray, byte> normalizedMagnitude = magnitude.ConvertScale<byte>(255.0 / maxVal, 0);
            resultImage = normalizedMagnitude.Convert<Bgr, byte>();
            MainImage.Source = ToBitmapSource(resultImage);
        }

        //--- Настраиваемый фильтр ---
        //Применяет фильтр, заданный пользователем в UI.
        private void CustomFilter_Click(object sender, RoutedEventArgs e)
        {
            if (sourceImage == null) return;

            double[,] kernel = ReadCustomKernelFromUI();

            Image<Bgr, byte> bluredImage = ApplyConvolution(sourceImage, kernel);

            MainImage.Source = ToBitmapSource(bluredImage);
        }

        //Считывает матрицу 3x3 из TextBox в Grid.
        private double[,] ReadCustomKernelFromUI()
        {
            double[,] customKernel = new double[3, 3];

            for (int i = 0; i < KernelGrid.Children.Count; i++)
            {
                if (KernelGrid.Children[i] is TextBox textBox)
                {
                    int row = i / 3;
                    int col = i % 3;

                    if (double.TryParse(textBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double value))
                    {
                        customKernel[row, col] = value;
                    }
                    else
                    {
                        customKernel[row, col] = 0;
                        MessageBox.Show($"Ошибка: в ячейке [{row + 1}, {col + 1}] неверное значение: '{textBox.Text}'. Установлено значение 0.");
                    }
                }
            }

            return customKernel;
        }
    }
}
