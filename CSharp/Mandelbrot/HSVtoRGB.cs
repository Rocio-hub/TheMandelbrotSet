using System.Drawing;

namespace Mandelbrot1
{
    public class HSVtoRGB
    {
        public HSVtoRGB() { }

        public Color ConvertHSVtoRGB(float h, float s, float v)
        {
            Func<float, int> f = delegate (float n)
            {
                float k = (n + h * 6) % 6;
                return (int)((v - v * s * Math.Max(0, Math.Min(Math.Min(k, 4 - k), 1))) * 255);
            };
            return Color.FromArgb(f(5), f(3), f(1));
        }
    }
}
