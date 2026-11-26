using System;

namespace Phidgets2Prosim
{
    // Pure curve math shared by the live voltage input and the editor's Preview.
    internal static class VoltageInterpolator
    {
        public static double Interpolate(double x, double[] inPts, double[] outPts, InterpolationMode mode, double curvePower)
        {
            if (inPts == null || outPts == null || outPts.Length < 2 || inPts.Length != outPts.Length)
                throw new ArgumentException("InputPoints/OutputPoints must be non-null, equal length, and have at least 2 points.");

            // Output is always 0-255: at or below the min input -> first output, at or above the max -> last output
            if (x <= inPts[0]) return Clamp(outPts[0]);
            if (x >= inPts[inPts.Length - 1]) return Clamp(outPts[outPts.Length - 1]);

            double y;
            switch (mode)
            {
                case InterpolationMode.Curve: y = Piecewise(x, inPts, outPts, curvePower); break;
                case InterpolationMode.Spline: y = Spline(x, inPts, outPts); break;
                default: y = Piecewise(x, inPts, outPts, 1.0); break;
            }
            return Clamp(y);   // splines can overshoot between points
        }

        private static double Clamp(double y) => Math.Max(0, Math.Min(255, y));

        // Linear when power == 1, otherwise the segment parameter is raised to the power.
        private static double Piecewise(double x, double[] xs, double[] ys, double power)
        {
            for (int i = 0; i < xs.Length - 1; i++)
            {
                if (x >= xs[i] && x <= xs[i + 1])
                {
                    double t = (x - xs[i]) / (xs[i + 1] - xs[i]);
                    if (power != 1.0) t = Math.Pow(t, power);
                    return ys[i] + (ys[i + 1] - ys[i]) * t;
                }
            }
            return x <= xs[0] ? ys[0] : ys[ys.Length - 1];
        }

        // Natural cubic spline.
        private static double Spline(double x, double[] xs, double[] ys)
        {
            int n = xs.Length;
            var b = new double[n - 1];
            var c = new double[n];
            var d = new double[n - 1];
            var h = new double[n - 1];
            for (int i = 0; i < n - 1; i++) h[i] = xs[i + 1] - xs[i];

            var alpha = new double[n - 1];
            for (int i = 1; i < n - 1; i++)
                alpha[i] = (3 / h[i]) * (ys[i + 1] - ys[i]) - (3 / h[i - 1]) * (ys[i] - ys[i - 1]);

            var l = new double[n];
            var mu = new double[n];
            var z = new double[n];
            l[0] = 1;
            for (int i = 1; i < n - 1; i++)
            {
                l[i] = 2 * (xs[i + 1] - xs[i - 1]) - h[i - 1] * mu[i - 1];
                mu[i] = h[i] / l[i];
                z[i] = (alpha[i] - h[i - 1] * z[i - 1]) / l[i];
            }
            l[n - 1] = 1;
            for (int j = n - 2; j >= 0; j--)
            {
                c[j] = z[j] - mu[j] * c[j + 1];
                b[j] = (ys[j + 1] - ys[j]) / h[j] - h[j] * (c[j + 1] + 2 * c[j]) / 3;
                d[j] = (c[j + 1] - c[j]) / (3 * h[j]);
            }

            int k;
            for (k = 0; k < n - 1; k++)
                if (x >= xs[k] && x <= xs[k + 1]) break;
            if (k >= n - 1) k = n - 2;

            double dx = x - xs[k];
            return ys[k] + b[k] * dx + c[k] * dx * dx + d[k] * dx * dx * dx;
        }
    }
}
