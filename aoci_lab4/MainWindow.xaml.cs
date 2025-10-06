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

        private Image<Bgr, byte> ApplyConvolution(Image<Bgr, byte> input, double[,] kernel)
        {
            int kernelSize = kernel.GetLength(0);
            int kernelRadius = kernelSize / 2;

            Image<Bgr, byte> output = input.Clone();

            for (int y = kernelRadius; y < input.Height - kernelRadius; y++)
            {
                for (int x = kernelRadius; x < input.Width - kernelRadius; x++)
                {
                    double sumR = 0, sumG = 0, sumB = 0;

                    for (int ky = -kernelRadius; ky <= kernelRadius; ky++)
                    {
                        for (int kx = -kernelRadius; kx <= kernelRadius; kx++)
                        {
                            Bgr neighborPixel = input[y + ky, x + kx];

                            double kernelValue = kernel[ky + kernelRadius, kx + kernelRadius];

                            sumR += neighborPixel.Red * kernelValue;
                            sumG += neighborPixel.Green * kernelValue;
                            sumB += neighborPixel.Blue * kernelValue;
                        }
                    }

                    output[y, x] = new Bgr(
                        (byte)Math.Max(0, Math.Min(255, sumB)),
                        (byte)Math.Max(0, Math.Min(255, sumG)),
                        (byte)Math.Max(0, Math.Min(255, sumR))
                    );
                }
            }

            return output;
        }

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
                            byte neighborPixel = input.Data[y + ky, x + kx, 0];
                            double kernelValue = kernel[ky + kernelRadius, kx + kernelRadius];
                            sum += neighborPixel * kernelValue;
                        }
                    }
                    output.Data[y, x, 0] = (float)sum;
                }
            }
            return output;
        }

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

        private void SobelX_Click(object sender, RoutedEventArgs e)
        {
            if (sourceImage == null) return;

            Image<Gray, byte> grayImage = sourceImage.Convert<Gray, byte>();
            Image<Bgr, byte> resultImage;

            double[,] kernelX = {
                { -1, 0, 1 },
                { -2, 0, 2 },
                { -1, 0, 1 }
            };

            Image<Gray, float> gradientX = ApplyConvolution(grayImage, kernelX);

            Image<Gray, float> magnitude = new Image<Gray, float>(grayImage.Size);

            for (int y = 0; y < magnitude.Height; y++)
            {
                for (int x = 0; x < magnitude.Width; x++)
                {
                    float gx = Math.Abs(gradientX.Data[y, x, 0]);
                    magnitude.Data[y, x, 0] = gx;
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

        private void SobelY_Click(object sender, RoutedEventArgs e)
        {
            if (sourceImage == null) return;

            Image<Gray, byte> grayImage = sourceImage.Convert<Gray, byte>();
            Image<Bgr, byte> resultImage;

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

        private void CustomFilter_Click(object sender, RoutedEventArgs e)
        {
            if (sourceImage == null) return;

            double[,] kernel = ReadCustomKernelFromUI();

            Image<Bgr, byte> bluredImage = ApplyConvolution(sourceImage, kernel);

            MainImage.Source = ToBitmapSource(bluredImage);
        }

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
