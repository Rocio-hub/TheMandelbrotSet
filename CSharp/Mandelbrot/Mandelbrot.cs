using System.Diagnostics;
using System.Drawing;

namespace Mandelbrot1
{
    public class Mandelbrot
    {
        private static int TOTAL_WIDTH = 1080, TOTAL_HEIGHT = 1080, max = 30000, counter = 0;
        private static Bitmap bm, bm_temp;
        private static HSVtoRGB hsvToRgb;
        private static List<Square> squareMatrix = new List<Square>();
        private static Color[] COLOR_ARRAY;
        private static readonly SemaphoreSlim sem_isUsing = new SemaphoreSlim(1);

        public static void Main(string[] args)
        {
            bm = new Bitmap(TOTAL_WIDTH, TOTAL_HEIGHT);
            COLOR_ARRAY = new Color[max];
            hsvToRgb = new HSVtoRGB();

            DataMatrix();
            FillColorArray();
            EmptyCanvas(bm);

            ShowTimeInSeconds("seq", () => PaintMandelbrotSequential());
            ShowTimeInSeconds("par", () => PaintMandelbrotParallel());
            //ShowTimeInSeconds("task", () => PaintMandelbrotTask());
        }

        private static void EmptyCanvas(Bitmap bm)
        {
            for (int row = 0; row < squareMatrix.Count(); row++)
            {
                for (int col = 0; col < squareMatrix.Count(); col++)
                {
                    bm.SetPixel(col, row, Color.White);
                }
            }

            using (FileStream fileStream = new FileStream(@"D:\Github\MandelbrotWithCSharp\MandelbrotParallelCSharp.png", FileMode.Create))
            {
                bm.Save(fileStream, System.Drawing.Imaging.ImageFormat.Png);
            }
        }

        private static void ShowTimeInSeconds(string type, Action ac)
        {
            string temp;
            switch (type)
            {
                case "par":
                    temp = "Parallel";
                    break;
                case "seq":
                    temp = "Sequential";
                    break;
                default:
                    temp = "Tasks";
                    break;
            }
            Stopwatch sw = Stopwatch.StartNew();
            ac.Invoke();
            sw.Stop();
            Console.WriteLine(temp + " Time = {0:f5} seconds", sw.ElapsedMilliseconds / 1000d);
        }

        private static void PaintMandelbrotSequential()
        {
            for (int i = 0; i < squareMatrix.Count; i++)
            {
                PaintSquare(TOTAL_WIDTH, TOTAL_HEIGHT, max, squareMatrix[i].xStart, squareMatrix[i].xEnd, squareMatrix[i].yStart, squareMatrix[i].yEnd);

                using (FileStream fileStream = new FileStream(@"D:\Github\MandelbrotWithCSharp\MandelbrotSequentialCSharp.png", FileMode.Create))
                {
                    bm.Save(fileStream, System.Drawing.Imaging.ImageFormat.Png);
                }
            }
        }

        private static void PaintMandelbrotParallel()
        {
            Parallel.For(0, squareMatrix.Count, i =>
            {
                PaintSquare(TOTAL_WIDTH, TOTAL_HEIGHT, max, squareMatrix[i].xStart, squareMatrix[i].xEnd, squareMatrix[i].yStart, squareMatrix[i].yEnd);
                {
                    sem_isUsing.Wait();
                    using (FileStream fileStream = new FileStream(@"D:\Github\MandelbrotWithCSharp\MandelbrotParallelCSharp.png", FileMode.Create))
                    {
                        bm.Save(fileStream, System.Drawing.Imaging.ImageFormat.Png);
                    }
                    sem_isUsing.Release();
                }
            });
        }

        private static void PaintMandelbrotTask()
        {
            Task[] taskArray = new Task[squareMatrix.Count+1];
            for (int i = 0; i < squareMatrix.Count; i++)
            {
                Task t = Task.Run(() => PaintSquare(TOTAL_WIDTH, TOTAL_HEIGHT, max, squareMatrix[i].xStart, squareMatrix[i].xEnd, squareMatrix[i].yStart, squareMatrix[i].yEnd));
                {
                    sem_isUsing.Wait();
                    using (FileStream fileStream = new FileStream(@"D:\Github\MandelbrotWithCSharp\MandelbrotTaskCSharp.png", FileMode.Create))
                    {
                        bm.Save(fileStream, System.Drawing.Imaging.ImageFormat.Png);
                    }
                    sem_isUsing.Release();
                }
                t.Wait();
            }


        }

        /*  private static void PaintMandelbrotParallelB()
          {
              Parallel.ForEach(Partitioner.Create(0, squareMatrix.Count),
                  (range) =>
                  {
                      for (int i = range.Item1; i < range.Item2; i++)
                      {
                          PaintSquare(TOTAL_WIDTH, TOTAL_HEIGHT, max, squareMatrix[i].xStart, squareMatrix[i].xEnd, squareMatrix[i].yStart, squareMatrix[i].yEnd);
                          {
                              sem_isUsing.Wait();
                              using (FileStream fileStream = new FileStream(@"D:\Github\MandelbrotWithCSharp\MandelbrotParallelCSharp.png", FileMode.Create))
                              {
                                  bm.Save(fileStream, System.Drawing.Imaging.ImageFormat.Png);
                              }
                              sem_isUsing.Release();
                          }
                      }
                  });
          }*/

        private static void PaintSquare(int width, int height, int max, int initWidth, int finalWidth, int initHeight, int finalHeight)
        {
            ++counter;

            bm_temp = new Bitmap(TOTAL_WIDTH, TOTAL_HEIGHT);

            for (int row = initHeight; row < finalHeight; row++)
            {
                for (int col = initWidth; col < finalWidth; col++)
                {
                    double nReal = (col - width / 2.0) * 4.0 / width;
                    double nImaginary = (row - height / 2.0) * 4.0 / height;

                    double x = 0, y = 0;
                    int iteration = 0;

                    while (x * x + y * y < 4 && iteration < max)
                    {
                        double xTemp = x * x - y * y + nReal;
                        y = 2.0 * x * y + nImaginary;
                        x = xTemp;
                        iteration++;
                    }
                    if (iteration < max)
                    {
                        sem_isUsing.Wait();
                        bm.SetPixel(col, row, COLOR_ARRAY[iteration]);
                        bm_temp.SetPixel(col, row, COLOR_ARRAY[iteration]);
                        sem_isUsing.Release();
                    }
                    else
                    {
                        sem_isUsing.Wait();
                        bm.SetPixel(col, row, Color.Black);
                        bm_temp.SetPixel(col, row, Color.Black);
                        sem_isUsing.Release();
                    }
                }
            }
        }

        private static void FillColorArray()
        {
            for (int i = 0; i < max; i++)
            {
                COLOR_ARRAY[i] = hsvToRgb.ConvertHSVtoRGB(i / 256f, 1, i / (i + 8f));
            }
        }

        private static void DataMatrix()
        {
            squareMatrix.Add(new Square(0, TOTAL_WIDTH / 4, 0, TOTAL_HEIGHT / 4));
            squareMatrix.Add(new Square(TOTAL_WIDTH / 4, TOTAL_WIDTH / 2, 0, TOTAL_HEIGHT / 4));
            squareMatrix.Add(new Square(TOTAL_WIDTH / 2, 3 * TOTAL_WIDTH / 4, 0, TOTAL_HEIGHT / 4));
            squareMatrix.Add(new Square(3 * TOTAL_WIDTH / 4, TOTAL_WIDTH, 0, TOTAL_HEIGHT / 4));

            squareMatrix.Add(new Square(0, TOTAL_WIDTH / 4, TOTAL_HEIGHT / 4, TOTAL_HEIGHT / 2));
            squareMatrix.Add(new Square(TOTAL_WIDTH / 4, TOTAL_WIDTH / 2, TOTAL_HEIGHT / 4, TOTAL_HEIGHT / 2));
            squareMatrix.Add(new Square(TOTAL_WIDTH / 2, 3 * TOTAL_WIDTH / 4, TOTAL_HEIGHT / 4, TOTAL_HEIGHT / 2));
            squareMatrix.Add(new Square(3 * TOTAL_WIDTH / 4, TOTAL_WIDTH, TOTAL_HEIGHT / 4, TOTAL_HEIGHT / 2));

            squareMatrix.Add(new Square(0, TOTAL_WIDTH / 4, TOTAL_HEIGHT / 2, 3 * TOTAL_HEIGHT / 4));
            squareMatrix.Add(new Square(TOTAL_WIDTH / 4, TOTAL_WIDTH / 2, TOTAL_HEIGHT / 2, 3 * TOTAL_HEIGHT / 4));
            squareMatrix.Add(new Square(TOTAL_WIDTH / 2, 3 * TOTAL_WIDTH / 4, TOTAL_HEIGHT / 2, 3 * TOTAL_HEIGHT / 4));
            squareMatrix.Add(new Square(3 * TOTAL_WIDTH / 4, TOTAL_WIDTH, TOTAL_HEIGHT / 2, 3 * TOTAL_HEIGHT / 4));

            squareMatrix.Add(new Square(0, TOTAL_WIDTH / 4, 3 * TOTAL_HEIGHT / 4, TOTAL_HEIGHT));
            squareMatrix.Add(new Square(TOTAL_WIDTH / 4, TOTAL_WIDTH / 2, 3 * TOTAL_HEIGHT / 4, TOTAL_HEIGHT));
            squareMatrix.Add(new Square(TOTAL_WIDTH / 2, 3 * TOTAL_WIDTH / 4, 3 * TOTAL_HEIGHT / 4, TOTAL_HEIGHT));
            squareMatrix.Add(new Square(3 * TOTAL_WIDTH / 4, TOTAL_WIDTH, 3 * TOTAL_HEIGHT / 4, TOTAL_HEIGHT));
        }
    }
}
