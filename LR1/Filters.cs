using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;



namespace LR1
{


    abstract class Filters
    {


        public int Clamp(int value, int min, int max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }

        protected abstract Color calculateNewPixelColor(Bitmap sourceImage, int x, int y);


        virtual public  Bitmap processImage(Bitmap sourceImage, BackgroundWorker worker)
        {
            Bitmap resultImage = new Bitmap(sourceImage.Width, sourceImage.Height);

            for (int i = 0; i < sourceImage.Height; i++)
            {
                worker.ReportProgress((int)((float)i / resultImage.Width * 100));
                if (worker.CancellationPending)
                    return null;
                for (int j = 0; j < sourceImage.Width; j++)
                {
                    resultImage.SetPixel(i, j, calculateNewPixelColor(sourceImage, i, j));
                }
            }

            return resultImage;
        }
    }
    class InvertFilter : Filters // Точечный. Инверсия
    {
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            Color sourceColor = sourceImage.GetPixel(x, y);
            Color resultColor = Color.FromArgb(255 - sourceColor.R, 255 - sourceColor.G, 255 - sourceColor.B);
            return resultColor;
        }
    }

    class MatrixFilter : Filters //Матричный фильтр
    {
        protected float[,] kernel = null;
        protected MatrixFilter() { }
        public MatrixFilter(float[,] kernel)
        {
            this.kernel = kernel;
        }
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            int radiusX = kernel.GetLength(0) / 2;
            int radiusY = kernel.GetLength(1) / 2;
            float resultR = 0;
            float resultG = 0;
            float resultB = 0;
            for (int l = -radiusY; l <= radiusY; l++)
                for (int k = -radiusX; k <= radiusX; k++)
                {
                    int idX = Clamp(x + k, 0, sourceImage.Width - 1);
                    int idY = Clamp(y + l, 0, sourceImage.Height - 1);
                    Color neighborColor = sourceImage.GetPixel(idX, idY);
                    resultR += neighborColor.R * kernel[k + radiusX, l + radiusY];
                    resultG += neighborColor.G * kernel[k + radiusX, l + radiusY];
                    resultB += neighborColor.B * kernel[k + radiusX, l + radiusY];
                }
            return Color.FromArgb(
                Clamp((int)resultR, 0, 255),
                Clamp((int)resultG, 0, 255),
                Clamp((int)resultB, 0, 255));
        }
    }

    class BlurFilter : MatrixFilter // Матричный фильтр Размытия
    {
        public BlurFilter()
        {
            int sizeX = 3;
            int sizeY = 3;
            kernel = new float[sizeX, sizeY];
            for (int i = 0; i < sizeX; i++)
                for (int j = 0; j < sizeY; j++)
                    kernel[i, j] = 1.0f / (float)(sizeX * sizeY);
        }
    }
    class GaussianFilter : MatrixFilter // Матричный Фильтр Гаусса
    {
        public void createGaussianKernel(int radius, float sigma)
        {
            //определяем размер ядра 
            int size = 2 * radius + 1;
            //создаем ядро фильтра 
            kernel = new float[size, size];
            //коээфициент нормировки ядра 
            float norm = 0;
            //рассчитываем ядро линейного фильтра
            for (int i = -radius; i <= radius; i++)
                for (int j = -radius; j <= radius; j++)
                {
                    kernel[i + radius, j + radius] = (float)(Math.Exp(-(i * i + j * j) / (2 * sigma * sigma)));
                    norm += kernel[i + radius, j + radius];
                }
            //нормируем ядро
            for (int i = 0; i < size; i++)
                for (int j = 0; j < size; j++)
                    kernel[i, j] /= norm;
        }
        public GaussianFilter()
        {
            createGaussianKernel(3, 2);
        }
    }

    class GrayWorldFilter : Filters // Точечный 10.Серый мир
    {
        
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            Color sourceColor = sourceImage.GetPixel(x, y);

            Color resultColor = Color.FromArgb(255 - sourceColor.R, 255 - sourceColor.G, 255 - sourceColor.B);
            return resultColor;
        }

        public override Bitmap processImage(Bitmap sourceImage, BackgroundWorker worker)
        {
            Bitmap resultImage = new Bitmap(sourceImage.Width, sourceImage.Height);

            float R = 0, G = 0, B = 0;
            float Avg = 0;

            Color temp;
            for (int i = 0; i < sourceImage.Width; i++)
                for (int j = 0; j < sourceImage.Height; j++)
                {
                    temp = sourceImage.GetPixel(i, j);
                    R += temp.R; G += temp.G; B += temp.B;
                }
            R = (float)R / (sourceImage.Width * sourceImage.Height);
            G = (float)G / (sourceImage.Width * sourceImage.Height);
            B = (float)B / (sourceImage.Width * sourceImage.Height);
            Avg = (R + G + B) / 3;

            Color sourceColor, resultColor;

            for (int i = 0; i < sourceImage.Width; i++)
            {
                worker.ReportProgress((int)((float)i / resultImage.Width * 100));
                if (worker.CancellationPending)
                    return null;
                for (int j = 0; j < sourceImage.Height; j++)
                {
                    sourceColor = sourceImage.GetPixel(i, j);
                    resultColor = Color.FromArgb(Clamp((int)(sourceColor.R * Avg / R), 0, 255),
                                                Clamp((int)(sourceColor.G * Avg / G), 0, 255),
                                                  Clamp((int)(sourceColor.B * Avg / B), 0, 255));

                    resultImage.SetPixel(i, j, resultColor);
                }
            }
            return resultImage;
        }
    }

    class LinearStretchingHistogram : Filters // Точечный 10.Линейное растяжение гистограммы
    {
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            return sourceImage.GetPixel(x, y);
        }
        public override Bitmap processImage(Bitmap sourceImage, BackgroundWorker worker)
        {
            Bitmap result = new Bitmap(sourceImage.Width, sourceImage.Height);
            int minXR = 0, maxXR = 0, maxXG = 0, minXG = 0, maxXB = 0, minXB = 0;
            double progress = 0.0;

            for (int i = 0; i < sourceImage.Width; i++, progress += 0.5)
            {
                worker.ReportProgress((int)((float)progress / sourceImage.Width * 100));
                if (worker.CancellationPending)
                    return null;
                for (int j = 0; j < sourceImage.Height; j++)
                {
                    Color source = sourceImage.GetPixel(i, j);
                    if (minXR > source.R)
                        minXR = source.R;

                    if (maxXR < source.R)
                        maxXR = source.R;

                    if (minXG > source.G)
                        minXG = source.G;

                    if (maxXG < source.G)
                        maxXG = source.G;

                    if (minXB > source.B)
                        minXB = source.B;

                    if (maxXB < source.B)
                        maxXB = source.B;
                }
            }
            for (int i = 0 ; i < sourceImage.Width ; i++, progress += 0.5)
            {
                worker.ReportProgress((int)((float)progress / sourceImage.Width * 100));
                if (worker.CancellationPending)
                    return null;
                for (int j = 0 ; j < sourceImage.Height ; j++)
                {
                    int R = sourceImage.GetPixel(i, j).R;
                    int G = sourceImage.GetPixel(i, j).G;
                    int B = sourceImage.GetPixel(i, j).B;
                    result.SetPixel(i, j, Color.FromArgb(Clamp(Clamp(((255 * (R - minXR)) / (maxXR - minXR)), 0, 255) + R, 0, 255),
                                                         Clamp(Clamp(((255 * (G - minXR)) / (maxXG - minXG)), 0, 255) + G, 0, 255),
                                                         Clamp(Clamp(((255 * (B - minXR)) / (maxXB - minXB)), 0, 255) + B, 0, 255)));
                }
            }
            return result;
        }
    }

    class WavesFilter1 : Filters // Точечный 8.Волны1
    {
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            int k, l;
            k = Clamp((int)(x + 20 * Math.Sin(2 * Math.PI * y / 60)), 0, sourceImage.Width - 1);
            l = Clamp(y, 0, sourceImage.Height - 1);
            Color sourceColor = sourceImage.GetPixel(k, l);
            Color resultColor = Color.FromArgb(sourceColor.R, sourceColor.G, sourceColor.B);

            return resultColor;
        }
    }

    class WavesFilter2 : Filters // Точечный 8.Волны2
    {
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            int k, l;
            k = Clamp((int)(x + 20 * Math.Sin(2 * Math.PI * x / 30)), 0, sourceImage.Width - 1);
            l = Clamp(y, 0, sourceImage.Height - 1);
            Color sourceColor = sourceImage.GetPixel(k, l);
            Color resultColor = Color.FromArgb(sourceColor.R, sourceColor.G, sourceColor.B);

            return resultColor;
        }
    }


    class GlassFilter : Filters // Точечный 8.Эффект Стекла
    {
        private Random rand = new Random();
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            int k, l;
            k = Clamp((int)(x + (rand.NextDouble() - 0.5) * 10), 0, sourceImage.Width - 1);
            l = Clamp((int)(y + (rand.NextDouble() - 0.5) * 10), 0, sourceImage.Height - 1);
            Color sourceColor = sourceImage.GetPixel(k, l);
            Color resultColor = Color.FromArgb(sourceColor.R, sourceColor.G, sourceColor.B);

            return resultColor;
        }
    }
    class SharpnessFilter : MatrixFilter // Матричный 8.Резкость
    {
        public SharpnessFilter()
        {
            int sizeX = 3;
            int sizeY = 3;
            kernel = new float[sizeX, sizeY];
            for (int i = 0 ; i < sizeX ; i++)
                for (int j = 0 ; j < sizeY ; j++)
                {
                    if ((i == 1) && (j == 1))
                    {
                        kernel[i, j] = 9.0f;
                    }
                    else
                        kernel[i, j] = -1.0f;
                }
        }
    }

    class MotionBlurFilter : MatrixFilter // Матричный 8.MotionBlur
    {
        public MotionBlurFilter()
        {
            int sizeX = 9;
            int sizeY = 9;
            kernel = new float[sizeX, sizeY];
            for (int i = 0 ; i < sizeX ; i++)
                for (int j = 0 ; j < sizeY ; j++)
                {
                    if (i == j)
                        kernel[i, j] = 1.0f / (float)(sizeY);
                    else
                        kernel[i, j] = 0.0f;
                }
        }
    }

    class GrayScaleFilter : Filters // Точечный 7.Из цветного в полутоновый
    {
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            Color sourceColor = sourceImage.GetPixel(x, y);
            Color resultColor = Color.FromArgb(
                (int)(0.36 * sourceColor.R + 0.53 * sourceColor.G + 0.11 * sourceColor.B), 
                (int)(0.36 * sourceColor.R + 0.53 * sourceColor.G + 0.11 * sourceColor.B), 
                (int)(0.36 * sourceColor.R + 0.53 * sourceColor.G + 0.11 * sourceColor.B));

            return resultColor;
        }
    }

    class Sepia : Filters // Точечный 7.Сепия
    {
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            Color sourceColor = sourceImage.GetPixel(x, y);
            double intensity = sourceColor.R * 0.36 + sourceColor.G * 0.53 + sourceColor.B * 0.11;
            double k = 10;
            Color resultColor = Color.FromArgb(
                Clamp((int)(intensity + 2 * k), 0, 255), 
                Clamp((int)(intensity + 0.5 * k), 0, 255), 
                Clamp((int)(intensity - k), 0, 255));

            return resultColor;
        }
    }

    class Brightness : Filters // Точечный 7.Увеличивает яркость
    {
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            Color sourceColor = sourceImage.GetPixel(x, y);
            double k = 1.25;
            int R = Clamp((int)(sourceColor.R * k), 0, 255);
            int G = Clamp((int)(sourceColor.G * k), 0, 255);
            int B = Clamp((int)(sourceColor.B * k), 0, 255);
            Color resultColor = Color.FromArgb(R, G, B);
            return resultColor;
        }
    }

    class SobelFilter : MatrixFilter // Матричный 7.Фильтр Собеля 
    {
        protected float[,] kernelY = null;
        public SobelFilter()
        {
            kernel = new float[3, 3] { { -1, 0, 1 }, { -2, 0, 2 }, { -1, 0, 1 } };
            kernelY = new float[3, 3] { { -1, -2, -1 }, { 0, 0, 0 }, { 1, 2, 1 } };
        }

        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            int radiusX = kernel.GetLength(0) / 2;
            int radiusY = kernel.GetLength(1) / 2;
            float resultR = 0;
            float resultG = 0;
            float resultB = 0;
            for (int l = -radiusY; l <= radiusY; l++)
                for (int k = -radiusX; k <= radiusX; k++)
                {
                    int idX = Clamp(x + k, 0, sourceImage.Width - 1);
                    int idY = Clamp(y + l, 0, sourceImage.Height - 1);
                    Color neighborColor = sourceImage.GetPixel(idX, idY);
                    resultR += neighborColor.R * kernel[k + radiusX, l + radiusY] + neighborColor.R * kernelY[k + radiusX, l + radiusY];
                    resultG += neighborColor.G * kernel[k + radiusX, l + radiusY] + neighborColor.G * kernelY[k + radiusX, l + radiusY];
                    resultB += neighborColor.B * kernel[k + radiusX, l + radiusY] + neighborColor.B * kernelY[k + radiusX, l + radiusY];
                }
            resultR = Math.Abs(resultR);
            resultG = Math.Abs(resultG);
            resultB = Math.Abs(resultB);
            return Color.FromArgb(
                Clamp((int)resultR, 0, 255),
                Clamp((int)resultG, 0, 255),
                Clamp((int)resultB, 0, 255)
                );
        }

       
    }

    class SharpnessFilter5 : MatrixFilter // Матричный 7.Резкость
    {
        public SharpnessFilter5()
        {
            int sizeX = 3;
            int sizeY = 3;
            kernel = new float[sizeX, sizeY];
            for (int i = 0; i < sizeX; i++)
                for (int j = 0; j < sizeY; j++)
                {
                    if ((i + j) % 2 == 1)
                    {
                        kernel[i, j] = -1.0f;
                    }
                    else
                    {
                        if ((i == 1) && (j == 1))
                        {
                            kernel[i, j] = 5.0f;
                        }
                        else
                        {
                            kernel[i, j] = 0.0f;
                        }
                    }
                    
                }
        }
    }

    class EmbossingFilter : MatrixFilter // Матричный 8.Тиснение
    {
        public EmbossingFilter()
        {
            kernel = new float[3, 3] { { 0, 1, 0 }, { 1, 0, -1 }, { 0, -1, 0 } };
        }

        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {

            int radiusX = kernel.GetLength(0) / 2;
            int radiusY = kernel.GetLength(1) / 2;
            float resultR = 0;
            float resultG = 0;
            float resultB = 0;
            for (int l = -radiusY; l <= radiusY; l++)
                for (int k = -radiusX; k <= radiusX; k++)
                {
                    int idX = Clamp(x + k, 0, sourceImage.Width - 1);
                    int idY = Clamp(y + l, 0, sourceImage.Height - 1);
                    Color neighborColor = sourceImage.GetPixel(idX, idY);
                    resultR += (neighborColor.R * kernel[k + radiusX, l + radiusY]);
                    resultG += (neighborColor.G * kernel[k + radiusX, l + radiusY]);
                    resultB += (neighborColor.B * kernel[k + radiusX, l + radiusY]);
                }
            resultR = (resultR + 128);
            resultG = (resultG + 128);
            resultB = (resultB + 128);
            return Color.FromArgb(
                Clamp((int)resultR, 0, 255),
                Clamp((int)resultG, 0, 255),
                Clamp((int)resultB, 0, 255)
                );
        }
    }

    class TransferFilter : Filters // Точечный 8.Перенос
    {
        protected override Color calculateNewPixelColor(Bitmap sourseImage, int x, int y)
        {
            if (x + 50 > sourseImage.Width - 1)
                return Color.Transparent;
            else
            {
                Color resultColor = sourseImage.GetPixel(x + 50, y);
                return resultColor;
            }
        }
    }

    class TurnFilter : Filters //// Точечный 8.Поворот
    {
        protected override Color calculateNewPixelColor(Bitmap sourseImage, int x, int y)
        {
            int x0 = (int)(sourseImage.Width / 2), y0 = (int)(sourseImage.Height / 2);
            double w = Math.PI / 4.0;
            int _x = (int)((x - x0) * Math.Cos(w) - (y - y0) * Math.Sin(w) + x0);
            int _y = (int)((x - x0) * Math.Sin(w) + (y - y0) * Math.Cos(w) + y0);

            if ((_x < sourseImage.Width - 1) && (_y < sourseImage.Height - 1) && (_x > 0) && (_y > 0))
            {
                Color resultColor = sourseImage.GetPixel(_x, _y);
                return resultColor;
            }
            return Color.Transparent;
        }
    }

    class HighBord_SharOperator : MatrixFilter // Матричный 8.Выделение границ.Оператор Щарра
    {
        protected float[,] kernelY = null;
        public HighBord_SharOperator()
        {
            kernel = new float[3, 3] { { 3, 0, -3 }, { 10, 0, -10 }, { 3, 0, -3 } };
            kernelY = new float[3, 3] { { 3, 10, 3 }, { 0, 0, 0 }, { -3, -10, -3 } };
        }

        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            int radiusX = kernel.GetLength(0) / 2;
            int radiusY = kernel.GetLength(1) / 2;
            float resultR = 0;
            float resultG = 0;
            float resultB = 0;
            for (int l = -radiusY; l <= radiusY; l++)
                for (int k = -radiusX; k <= radiusX; k++)
                {
                    int idX = Clamp(x + k, 0, sourceImage.Width - 1);
                    int idY = Clamp(y + l, 0, sourceImage.Height - 1);
                    Color neighborColor = sourceImage.GetPixel(idX, idY);
                    resultR += neighborColor.R * kernel[k + radiusX, l + radiusY] + neighborColor.R * kernelY[k + radiusX, l + radiusY];
                    resultG += neighborColor.G * kernel[k + radiusX, l + radiusY] + neighborColor.G * kernelY[k + radiusX, l + radiusY];
                    resultB += neighborColor.B * kernel[k + radiusX, l + radiusY] + neighborColor.B * kernelY[k + radiusX, l + radiusY];
                }
            resultR = Math.Abs(resultR);
            resultG = Math.Abs(resultG);
            resultB = Math.Abs(resultB);
            return Color.FromArgb(
                Clamp((int)resultR, 0, 255),
                Clamp((int)resultG, 0, 255),
                Clamp((int)resultB, 0, 255)
                );
        }
    }

    class HighBord_PrewittOperator : MatrixFilter // Матричный 8.Выделение границ.Оператор Прюитта
    {
        protected float[,] kernelY = null;
        public HighBord_PrewittOperator()
        {
            kernel = new float[3, 3] { { -1, 0, 1 }, { -1, 0, 1 }, { -1, 0, 1 } };
            kernelY = new float[3, 3] { { -1, -1, -1 }, { 0, 0, 0 }, { 1, 1, 1 } };
        }

        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            int radiusX = kernel.GetLength(0) / 2;
            int radiusY = kernel.GetLength(1) / 2;
            float resultR = 0;
            float resultG = 0;
            float resultB = 0;
            for (int l = -radiusY; l <= radiusY; l++)
                for (int k = -radiusX; k <= radiusX; k++)
                {
                    int idX = Clamp(x + k, 0, sourceImage.Width - 1);
                    int idY = Clamp(y + l, 0, sourceImage.Height - 1);
                    Color neighborColor = sourceImage.GetPixel(idX, idY);
                    resultR += neighborColor.R * kernel[k + radiusX, l + radiusY] + neighborColor.R * kernelY[k + radiusX, l + radiusY];
                    resultG += neighborColor.G * kernel[k + radiusX, l + radiusY] + neighborColor.G * kernelY[k + radiusX, l + radiusY];
                    resultB += neighborColor.B * kernel[k + radiusX, l + radiusY] + neighborColor.B * kernelY[k + radiusX, l + radiusY];
                }
            resultR = Math.Abs(resultR);
            resultG = Math.Abs(resultG);
            resultB = Math.Abs(resultB);
            return Color.FromArgb(
                Clamp((int)resultR, 0, 255),
                Clamp((int)resultG, 0, 255),
                Clamp((int)resultB, 0, 255)
                );
        }
    }

    class MedianFilter : Filters // Точечный 10.Медианный фильтр
    {

        const int size = 3;
        protected override Color calculateNewPixelColor(Bitmap sourseImage, int x, int y)
        {
            Color sourceColor = sourseImage.GetPixel(x, y);
            int median = size * size / 2;

            int[] local_R = new int[9];
            int[] local_G = new int[9];
            int[] local_B = new int[9];

            int k = 0;
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                {
                    Color currColor = sourseImage.GetPixel(
                        Clamp(x + i, 0, sourseImage.Width - 1), 
                        Clamp(y + j, 0, sourseImage.Height - 1));
                    local_R[k] = (int)currColor.R; ;
                    local_G[k] = (int)currColor.G; ;
                    local_B[k] = (int)currColor.B; ;
                    k++;
                }
            Array.Sort(local_R);
            Array.Sort(local_G);
            Array.Sort(local_B);


            Color resultColor = Color.FromArgb(
                local_R[median], 
                local_G[median], 
                local_B[median]);
            return resultColor;
        }
    }

    class MaxFilter : Filters // Точечный 8.Фильтр "Максимум"
    {
        protected int avgR;
        protected int avgG;
        protected int avgB;
        protected int[] avg_r;
        protected int[] avg_g;
        protected int[] avg_b;
        protected const int size = 9;
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            avg_r = new int[size];
            avg_g = new int[size];
            avg_b = new int[size];
            int r = 0;
            int g = 0;
            int b = 0;
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                {
                    Color currColor = sourceImage.GetPixel(Clamp(x + i, 0, sourceImage.Width - 1), Clamp(y + j, 0, sourceImage.Height - 1));
                    avg_r[r++] = (int)currColor.R;
                    avg_g[g++] = (int)currColor.G;
                    avg_b[b++] = (int)currColor.B;
                }
            Color sourceColor = sourceImage.GetPixel(x, y);
            avgR = Sort(avg_r);
            avgG = Sort(avg_g);
            avgB = Sort(avg_b);
            Color resultColor = Color.FromArgb(Clamp(avgR, 0, 255), Clamp(avgG, 0, 255), Clamp(avgB, 0, 255));

            return resultColor;
        }

        protected int Sort(int[] arr)
        {
            for (int i = 0; i < size - 1; i++)
                for (int j = i + 1; j < size; j++)
                {
                    if (arr[i] > arr[j])
                    {
                        int tmp = arr[i];
                        arr[i] = arr[j];
                        arr[j] = tmp;
                    }
                }

            return arr[size - 1];
        }
    }

    class PerfectReflectorFilter : Filters // Точечный 10. Идеальный отражатель
    {
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            return sourceImage.GetPixel(x, y);
        }
        public override Bitmap processImage(Bitmap sourceImage, BackgroundWorker worker)
        {
            Bitmap result = new Bitmap(sourceImage.Width, sourceImage.Height);
            double Rmax = 0, Gmax = 0, Bmax = 0;
            double progress = 0.0;

            for (int i = 0; i < sourceImage.Width; i++, progress += 0.5)
            {
                worker.ReportProgress((int)((float)progress / sourceImage.Width * 100));
                if (worker.CancellationPending)
                    return null;
                for (int j = 0; j < sourceImage.Height; j++)
                {
                    if (sourceImage.GetPixel(i, j).R > Rmax)
                        Rmax = sourceImage.GetPixel(i, j).R;
                    if (sourceImage.GetPixel(i, j).G > Gmax)
                        Gmax = sourceImage.GetPixel(i, j).G;
                    if (sourceImage.GetPixel(i, j).B > Bmax)
                        Bmax = sourceImage.GetPixel(i, j).B;
                }
            }

            for (int i = 0; i < sourceImage.Width; i++, progress += 0.5)
            {
                worker.ReportProgress((int)((float)progress / sourceImage.Width * 100));
                if (worker.CancellationPending)
                    return null;
                for (int j = 0; j < sourceImage.Height; j++)
                {
                    int newR = Clamp((int)(calculateNewPixelColor(sourceImage, i, j).R * 255 / Rmax), 0, 255);
                    int newG = Clamp((int)(calculateNewPixelColor(sourceImage, i, j).G * 255 / Gmax), 0, 255);
                    int newB = Clamp((int)(calculateNewPixelColor(sourceImage, i, j).B * 255 / Bmax), 0, 255);
                    result.SetPixel(i, j, Color.FromArgb(newR, newG, newB));
                }
            }
            return result;
        }
    }

    class ReferenceColorCorrection : Filters // Точечный 10. Коррекция с опорным цветом
    {
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            return sourceImage.GetPixel(x, y);
        }

        public override Bitmap processImage(Bitmap sourceImage, BackgroundWorker worker)
        {
            Bitmap resultImage = new Bitmap(sourceImage.Width, sourceImage.Height);

            double oldR = 124, oldG = 149, oldB = 171;  

            for (int i = 0; i < sourceImage.Width; i++)
            {
                worker.ReportProgress((int)((float)i / sourceImage.Width * 100));
                if (worker.CancellationPending)
                    return null;
                for (int j = 0; j < sourceImage.Height; j++)
                {
                    int newR = Clamp((int)(calculateNewPixelColor(sourceImage, i, j).R * (255 - Math.Abs(oldR - calculateNewPixelColor(sourceImage, i, j).R)) / oldR), 0, 255);
                    int newG = Clamp((int)(calculateNewPixelColor(sourceImage, i, j).G * (255 - Math.Abs(oldG - calculateNewPixelColor(sourceImage, i, j).G)) / oldG), 0, 255);
                    int newB = Clamp((int)(calculateNewPixelColor(sourceImage, i, j).B * (255 - Math.Abs(oldB - calculateNewPixelColor(sourceImage, i, j).B)) / oldB), 0, 255);
                    resultImage.SetPixel(i, j, Color.FromArgb(newR, newG, newB));
                }
            }
            return resultImage;
        }
    }

    class Morfology : Filters //Класс Математической морфологии
    {
        protected bool isDilation;
        protected int[,] mask = null;
        protected Morfology()
        {
            mask = new int[,] { { 0, 1, 0 }, { 1, 1, 1 }, { 0, 1, 0 } };
        }
        public Morfology(int[,] mask)
        {
            this.mask = mask;
        }
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            int radiusX = mask.GetLength(0) / 2;
            int radiusY = mask.GetLength(1) / 2;
            int minR = 255; int minG = 255; int minB = 255;
            int maxR = 0; int maxG = 0; int maxB = 0;
            for (int i = -radiusX; i <= radiusX; i++)
                for (int j = -radiusY; j <= radiusY; j++)
                {
                    if (isDilation)
                    {
                        if ((mask[i + radiusX, j + radiusY] != 0) && (sourceImage.GetPixel(x + i, y + j).R > maxR))
                            maxR = sourceImage.GetPixel(x + i, y + j).R;
                        if ((mask[i + radiusX, j + radiusY] != 0) && (sourceImage.GetPixel(x + i, y + j).G > maxG))
                            maxG = sourceImage.GetPixel(x + i, y + j).G;
                        if ((mask[i + radiusX, j + radiusY] != 0) && (sourceImage.GetPixel(x + i, y + j).B > maxB))
                            maxB = sourceImage.GetPixel(x + i, y + j).B;
                    }
                    else
                    {
                        if ((mask[i + radiusX, j + radiusY] != 0) && (sourceImage.GetPixel(x + i, y + j).R < minR))
                            minR = sourceImage.GetPixel(x + i, y + j).R;
                        if ((mask[i + radiusX, j + radiusY] != 0) && (sourceImage.GetPixel(x + i, y + j).G < minG))
                            minG = sourceImage.GetPixel(x + i, y + j).G;
                        if ((mask[i + radiusX, j + radiusY] != 0) && (sourceImage.GetPixel(x + i, y + j).B < minB))
                            minB = sourceImage.GetPixel(x + i, y + j).B;
                    }
                }
            if (isDilation)
                return Color.FromArgb(maxR, maxG, maxB);
            else
                return Color.FromArgb(minR, minG, minB);
        }
        public override Bitmap processImage(Bitmap sourceImage, BackgroundWorker worker)
        {
            int radiusX = mask.GetLength(0) / 2;
            int radiusY = mask.GetLength(1) / 2;
            Bitmap resultImage = new Bitmap(sourceImage.Width, sourceImage.Height);
            for (int i = radiusX; i < sourceImage.Width - radiusX; i++)
            {
                worker.ReportProgress((int)((float)i / resultImage.Width * 100));
                if (worker.CancellationPending)
                    return null;
                for (int j = radiusY; j < sourceImage.Height - radiusY; j++)
                    resultImage.SetPixel(i, j, calculateNewPixelColor(sourceImage, i, j));
            }
            return resultImage;
        }
    }

    class Dilation : Morfology // 10 Расширение
    {
        public Dilation()
        {
            isDilation = true;
        }
        public Dilation(int[,] mask)
        {
            this.mask = mask;
            isDilation = true;
        }
    }

    class Erosion : Morfology // 10 Сужение
    {
        public Erosion()
        {
            isDilation = false;
        }
        public Erosion(int[,] mask)
        {
            this.mask = mask;
            isDilation = false;
        }
    }

    class Opening : Morfology // 10 Раскрытие
    {
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            return sourceImage.GetPixel(x, y);
        }
        public override Bitmap processImage(Bitmap sourceImage, BackgroundWorker worker)
        {
            Bitmap resultImage = new Bitmap(sourceImage.Width, sourceImage.Height);
            Filters filter1 = new Erosion(mask);
            Bitmap result = filter1.processImage(sourceImage, worker);
            Filters filter2 = new Dilation(mask);
            result = filter2.processImage(result, worker);
            return result;
        }
    }

    class Closing : Morfology // 10 Закрытие
    {
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            return sourceImage.GetPixel(x, y);
        }
        public override Bitmap processImage(Bitmap sourceImage, BackgroundWorker worker)
        {
            Bitmap resultImage = new Bitmap(sourceImage.Width, sourceImage.Height);
            Filters filter1 = new Dilation(mask);
            Bitmap result = filter1.processImage(sourceImage, worker);
            Filters filter2 = new Erosion(mask);
            result = filter2.processImage(result, worker);
            return result;
        }
    }

    class TopHat : Morfology // 10 св-во морфологических операций - Top Hat
    {
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            return sourceImage.GetPixel(x, y);
        }
        public override Bitmap processImage(Bitmap sourceImage, BackgroundWorker worker)
        {
            Bitmap result = new Bitmap(sourceImage.Width, sourceImage.Height);
            Filters filter1 = new Erosion(mask);
            Bitmap result1 = filter1.processImage(sourceImage, worker);
            for (int i = 0; i < sourceImage.Width; i++)
            {
                worker.ReportProgress((int)((float)i / sourceImage.Width * 100));
                if (worker.CancellationPending)
                    return null;
                for (int j = 0; j < sourceImage.Height; j++)
                {
                    int newR = Clamp(sourceImage.GetPixel(i, j).R - result1.GetPixel(i, j).R, 0, 255);
                    int newG = Clamp(sourceImage.GetPixel(i, j).G - result1.GetPixel(i, j).G, 0, 255);
                    int newB = Clamp(sourceImage.GetPixel(i, j).B - result1.GetPixel(i, j).B, 0, 255);
                    result.SetPixel(i, j, Color.FromArgb(newR, newG, newB));
                }
            }
            return result;
        }
    }

    class BlackHat : Morfology // 10 св-во морфологических операций - Black Hat
    {
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            return sourceImage.GetPixel(x, y);
        }
        public override Bitmap processImage(Bitmap sourceImage, BackgroundWorker worker)
        {
            Bitmap result = new Bitmap(sourceImage.Width, sourceImage.Height);
            Filters filter1 = new Dilation(mask);
            Bitmap result1 = filter1.processImage(sourceImage, worker);
            for (int i = 0; i < sourceImage.Width; i++)
            {
                worker.ReportProgress((int)((float)i / sourceImage.Width * 100));
                if (worker.CancellationPending)
                    return null;
                for (int j = 0; j < sourceImage.Height; j++)
                {
                    int newR = Clamp(result1.GetPixel(i, j).R - sourceImage.GetPixel(i, j).R, 0, 255);
                    int newG = Clamp(result1.GetPixel(i, j).G - sourceImage.GetPixel(i, j).G, 0, 255);
                    int newB = Clamp(result1.GetPixel(i, j).B - sourceImage.GetPixel(i, j).B, 0, 255);
                    result.SetPixel(i, j, Color.FromArgb(newR, newG, newB));
                }
            }
            return result;
        }
    }

    class Grad : Morfology // 10 св-во морфологических операций - Grad
    {
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            return sourceImage.GetPixel(x, y);
        }
        public override Bitmap processImage(Bitmap sourceImage, BackgroundWorker worker)
        {
            Bitmap result = new Bitmap(sourceImage.Width, sourceImage.Height);
            Filters filter1 = new Dilation(mask);
            Bitmap result1 = filter1.processImage(sourceImage, worker);
            Filters filter2 = new Erosion(mask);
            Bitmap result2 = filter2.processImage(sourceImage, worker);
            for (int i = 0; i < sourceImage.Width; i++)
            {
                worker.ReportProgress((int)((float)i / sourceImage.Width * 100));
                if (worker.CancellationPending)
                    return null;
                for (int j = 0; j < sourceImage.Height; j++)
                {
                    int newR = Clamp(result1.GetPixel(i, j).R - result2.GetPixel(i, j).R, 0, 255);
                    int newG = Clamp(result1.GetPixel(i, j).G - result2.GetPixel(i, j).G, 0, 255);
                    int newB = Clamp(result1.GetPixel(i, j).B - result2.GetPixel(i, j).B, 0, 255);
                    result.SetPixel(i, j, Color.FromArgb(newR, newG, newB));
                }
            }
            return result;
        }
    }

}