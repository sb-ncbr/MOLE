/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */

namespace WebChemistry.Tunnels.Core.Helpers
{
    using System;
    using System.Collections.Generic;

    // The spline interpolation code has been adapted from Math.NET, MIT license, Copyright (c) 2002-2015 Math.NET
    // 
    // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
    // INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR 
    // PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE 
    // FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, 
    // ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.


    public class SplineInterpolation
    {
        /// <summary>
        /// Sample Points t.
        /// </summary>
        private IList<double> _points;

        /// <summary>
        /// Spline Coefficients c(t).
        /// </summary>
        private IList<double> _coefficients;

        /// <summary>
        /// Number of samples.
        /// </summary>
        private int _sampleCount;

        /// <summary>
        /// Initializes a new instance of the SplineInterpolation class.
        /// </summary>
        public SplineInterpolation()
        {
        }

        /// <summary>
        /// Initializes a new instance of the SplineInterpolation class.
        /// </summary>
        /// <param name="samplePoints">Sample Points t (length: N), sorted ascending.</param>
        /// <param name="splineCoefficients">Spline Coefficients (length: 4*(N-1)).</param>
        public SplineInterpolation(
            IList<double> samplePoints,
            IList<double> splineCoefficients)
        {
            Initialize(samplePoints, splineCoefficients);
        }

        /// <summary>
        /// Initialize the interpolation method with the given spline coefficients (sorted by the sample points t).
        /// </summary>
        /// <param name="samplePoints">Sample Points t (length: N), sorted ascending.</param>
        /// <param name="splineCoefficients">Spline Coefficients (length: 4*(N-1)).</param>
        public void Initialize(
            IList<double> samplePoints,
            IList<double> splineCoefficients)
        {
            if (null == samplePoints)
            {
                throw new ArgumentNullException("samplePoints");
            }

            if (null == splineCoefficients)
            {
                throw new ArgumentNullException("splineCoefficients");
            }

            if (samplePoints.Count < 2)
            {
                throw new ArgumentOutOfRangeException("samplePoints");
            }

            if (splineCoefficients.Count != 4 * (samplePoints.Count - 1))
            {
                throw new ArgumentOutOfRangeException("splineCoefficients");
            }

            _points = samplePoints;
            _coefficients = splineCoefficients;
            _sampleCount = samplePoints.Count;
        }

        /// <summary>
        /// Interpolate at point t.
        /// </summary>
        /// <param name="t">Point t to interpolate at.</param>
        /// <returns>Interpolated value x(t).</returns>
        public double Interpolate(double t)
        {
            int closestLeftIndex = IndexOfClosestPointLeftOf(t);

            // Interpolation
            double offset = t - _points[closestLeftIndex];
            int k = closestLeftIndex << 2;

            return _coefficients[k]
                   + (offset * (_coefficients[k + 1]
                                + (offset * (_coefficients[k + 2]
                                             + (offset * _coefficients[k + 3])))));
        }

        /// <summary>
        /// Differentiate at point t.
        /// </summary>
        /// <param name="t">Point t to interpolate at.</param>
        /// <returns>Interpolated first derivative at point t.</returns>
        /// <seealso cref="IInterpolation.SupportsDifferentiation"/>
        /// <seealso cref="Differentiate(double, out double, out double)"/>
        public double Differentiate(double t)
        {
            int closestLeftIndex = IndexOfClosestPointLeftOf(t);

            // Differentiation
            double offset = t - _points[closestLeftIndex];
            int k = closestLeftIndex << 2;

            return _coefficients[k + 1]
                   + (2 * offset * _coefficients[k + 2])
                   + (3 * offset * offset * _coefficients[k + 3]);
        }

        /// <summary>
        /// Differentiate at point t.
        /// </summary>
        /// <param name="t">Point t to interpolate at.</param>
        /// <param name="interpolatedValue">Interpolated value x(t)</param>
        /// <param name="secondDerivative">Interpolated second derivative at point t.</param>
        /// <returns>Interpolated first derivative at point t.</returns>
        /// <seealso cref="IInterpolation.SupportsDifferentiation"/>
        /// <seealso cref="Differentiate(double)"/>
        public double Differentiate(
            double t,
            out double interpolatedValue,
            out double secondDerivative)
        {
            int closestLeftIndex = IndexOfClosestPointLeftOf(t);

            // Differentiation
            double offset = t - _points[closestLeftIndex];
            int k = closestLeftIndex << 2;

            interpolatedValue = _coefficients[k]
                                + (offset * (_coefficients[k + 1]
                                             + (offset * (_coefficients[k + 2]
                                                          + (offset * _coefficients[k + 3])))));

            secondDerivative = (2 * _coefficients[k + 2])
                               + (6 * offset * _coefficients[k + 3]);

            return _coefficients[k + 1]
                   + (2 * offset * _coefficients[k + 2])
                   + (3 * offset * offset * _coefficients[k + 3]);
        }

        /// <summary>
        /// Integrate up to point t.
        /// </summary>
        /// <param name="t">Right bound of the integration interval [a,t].</param>
        /// <returns>Interpolated definite integral over the interval [a,t].</returns>
        /// <seealso cref="IInterpolation.SupportsIntegration"/>
        public double Integrate(double t)
        {
            int closestLeftIndex = IndexOfClosestPointLeftOf(t);

            // Integration
            double result = 0;
            for (int i = 0, j = 0; i < closestLeftIndex; i++, j += 4)
            {
                double w = _points[i + 1] - _points[i];
                result += w * (_coefficients[j]
                               + ((w * _coefficients[j + 1] * 0.5)
                                  + (w * ((_coefficients[j + 2] / 3)
                                          + (w * _coefficients[j + 3] * 0.25)))));
            }

            double offset = t - _points[closestLeftIndex];
            int k = closestLeftIndex << 2;

            return result + (offset * (_coefficients[k]
                                       + (offset * _coefficients[k + 1] * 0.5)
                                       + (offset * _coefficients[k + 2] / 3)
                                       + (offset * _coefficients[k + 3] * 0.25)));
        }

        /// <summary>
        /// Find the index of the greatest sample point smaller than t.
        /// </summary>
        /// <param name="t">The value to look for.</param>
        /// <returns>The sample point index.</returns>
        private int IndexOfClosestPointLeftOf(double t)
        {
            // Binary search in the [ t[0], ..., t[n-2] ] (t[n-1] is not included)
            int low = 0;
            int high = _sampleCount - 1;
            while (low != high - 1)
            {
                int middle = (low + high) / 2;
                if (_points[middle] > t)
                {
                    high = middle;
                }
                else
                {
                    low = middle;
                }
            }

            return low;
        }
    }

    /// <summary>
    /// Left and right boundary conditions.
    /// </summary>
    public enum SplineBoundaryCondition
    {
        /// <summary>
        /// Natural Boundary (Zero second derivative).
        /// </summary>
        Natural = 0,

        /// <summary>
        /// Parabolically Terminated boundary.
        /// </summary>
        ParabolicallyTerminated,

        /// <summary>
        /// Fixed first derivative at the boundary.
        /// </summary>
        FirstDerivative,

        /// <summary>
        /// Fixed second derivative at the boundary.
        /// </summary>
        SecondDerivative
    }

    /// <summary>
    /// Cubic Hermite Spline Interpolation Algorithm.
    /// </summary>
    /// <remarks>
    /// This algorithm supports both differentiation and integration.
    /// </remarks>
    public class CubicHermiteSplineInterpolation
    {
        /// <summary>
        /// Internal Spline Interpolation
        /// </summary>
        private readonly SplineInterpolation _spline;

        /// <summary>
        /// Initializes a new instance of the CubicHermiteSplineInterpolation class.
        /// </summary>
        public CubicHermiteSplineInterpolation()
        {
            _spline = new SplineInterpolation();
        }

        /// <summary>
        /// Initializes a new instance of the CubicHermiteSplineInterpolation class.
        /// </summary>
        /// <param name="samplePoints">Sample Points t, sorted ascending.</param>
        /// <param name="sampleValues">Sample Values x(t)</param>
        /// <param name="sampleDerivatives">Sample Derivatives x'(t)</param>
        public CubicHermiteSplineInterpolation(
            IList<double> samplePoints,
            IList<double> sampleValues,
            IList<double> sampleDerivatives)
        {
            _spline = new SplineInterpolation();
            Initialize(samplePoints, sampleValues, sampleDerivatives);
        }
        
        /// <summary>
        /// Initialize the interpolation method with the given spline coefficients (sorted by the sample points t).
        /// </summary>
        /// <param name="samplePoints">Sample Points t, sorted ascending.</param>
        /// <param name="sampleValues">Sample Values x(t)</param>
        /// <param name="sampleDerivatives">Sample Derivatives x'(t)</param>
        public void Initialize(
            IList<double> samplePoints,
            IList<double> sampleValues,
            IList<double> sampleDerivatives)
        {
            double[] coefficients = EvaluateSplineCoefficients(
                samplePoints,
                sampleValues,
                sampleDerivatives);

            _spline.Initialize(samplePoints, coefficients);
        }

        /// <summary>
        /// Evaluate the spline coefficients as used
        /// internally by this interpolation algorithm.
        /// </summary>
        /// <param name="samplePoints">Sample Points t, sorted ascending.</param>
        /// <param name="sampleValues">Sample Values x(t)</param>
        /// <param name="sampleDerivatives">Sample Derivatives x'(t)</param>
        /// <returns>Spline Coefficient Vector</returns>
        public static double[] EvaluateSplineCoefficients(
            IList<double> samplePoints,
            IList<double> sampleValues,
            IList<double> sampleDerivatives)
        {
            if (null == samplePoints)
            {
                throw new ArgumentNullException("samplePoints");
            }

            if (null == sampleValues)
            {
                throw new ArgumentNullException("sampleValues");
            }

            if (null == sampleDerivatives)
            {
                throw new ArgumentNullException("sampleDerivatives");
            }

            if (samplePoints.Count < 2)
            {
                throw new ArgumentOutOfRangeException("samplePoints");
            }

            if (samplePoints.Count != sampleValues.Count
                || samplePoints.Count != sampleDerivatives.Count)
            {
                throw new ArgumentException("Needs to be same length");
            }

            double[] coefficients = new double[4 * (samplePoints.Count - 1)];

            for (int i = 0, j = 0; i < samplePoints.Count - 1; i++, j += 4)
            {
                double delta = samplePoints[i + 1] - samplePoints[i];
                double delta2 = delta * delta;
                double delta3 = delta * delta2;
                coefficients[j] = sampleValues[i];
                coefficients[j + 1] = sampleDerivatives[i];
                coefficients[j + 2] = ((3 * (sampleValues[i + 1] - sampleValues[i])) - (2 * sampleDerivatives[i] * delta) - (sampleDerivatives[i + 1] * delta)) / delta2;
                coefficients[j + 3] = ((2 * (sampleValues[i] - sampleValues[i + 1])) + (sampleDerivatives[i] * delta) + (sampleDerivatives[i + 1] * delta)) / delta3;
            }

            return coefficients;
        }

        /// <summary>
        /// Interpolate at point t.
        /// </summary>
        /// <param name="t">Point t to interpolate at.</param>
        /// <returns>Interpolated value x(t).</returns>
        public double Interpolate(double t)
        {
            return _spline.Interpolate(t);
        }

        /// <summary>
        /// Differentiate at point t.
        /// </summary>
        /// <param name="t">Point t to interpolate at.</param>
        /// <returns>Interpolated first derivative at point t.</returns>
        /// <seealso cref="IInterpolation.SupportsDifferentiation"/>
        /// <seealso cref="Differentiate(double, out double, out double)"/>
        public double Differentiate(double t)
        {
            return _spline.Differentiate(t);
        }

        /// <summary>
        /// Differentiate at point t.
        /// </summary>
        /// <param name="t">Point t to interpolate at.</param>
        /// <param name="interpolatedValue">Interpolated value x(t)</param>
        /// <param name="secondDerivative">Interpolated second derivative at point t.</param>
        /// <returns>Interpolated first derivative at point t.</returns>
        /// <seealso cref="IInterpolation.SupportsDifferentiation"/>
        /// <seealso cref="Differentiate(double)"/>
        public double Differentiate(
            double t,
            out double interpolatedValue,
            out double secondDerivative)
        {
            return _spline.Differentiate(t, out interpolatedValue, out secondDerivative);
        }

        /// <summary>
        /// Integrate up to point t.
        /// </summary>
        /// <param name="t">Right bound of the integration interval [a,t].</param>
        /// <returns>Interpolated definite integral over the interval [a,t].</returns>
        /// <seealso cref="IInterpolation.SupportsIntegration"/>
        public double Integrate(double t)
        {
            return _spline.Integrate(t);
        }
    }

    /// <summary>
    /// Cubic Spline Interpolation Algorithm with continuous first and second derivatives.
    /// </summary>
    /// <remarks>
    /// This algorithm supports both differentiation and integration.
    /// </remarks>
    public class CubicSplineInterpolation
    {
        /// <summary>
        /// Internal Spline Interpolation
        /// </summary>
        private readonly CubicHermiteSplineInterpolation _spline;

        /// <summary>
        /// Initializes a new instance of the CubicSplineInterpolation class.
        /// </summary>
        public CubicSplineInterpolation()
        {
            _spline = new CubicHermiteSplineInterpolation();
        }

        /// <summary>
        /// Initializes a new instance of the CubicSplineInterpolation class.
        /// </summary>
        /// <param name="samplePoints">Sample Points t, sorted ascending.</param>
        /// <param name="sampleValues">Sample Values x(t)</param>
        public CubicSplineInterpolation(
            IList<double> samplePoints,
            IList<double> sampleValues)
        {
            _spline = new CubicHermiteSplineInterpolation();

            Initialize(
                samplePoints,
                sampleValues);
        }

        /// <summary>
        /// Initializes a new instance of the CubicSplineInterpolation class.
        /// </summary>
        /// <param name="samplePoints">Sample Points t, sorted ascending.</param>
        /// <param name="sampleValues">Sample Values x(t)</param>
        /// <param name="leftBoundaryCondition">Condition of the left boundary.</param>
        /// <param name="leftBoundary">Left boundary value. Ignored in the parabolic case.</param>
        /// <param name="rightBoundaryCondition">Condition of the right boundary.</param>
        /// <param name="rightBoundary">Right boundary value. Ignored in the parabolic case.</param>
        public CubicSplineInterpolation(
            IList<double> samplePoints,
            IList<double> sampleValues,
            SplineBoundaryCondition leftBoundaryCondition,
            double leftBoundary,
            SplineBoundaryCondition rightBoundaryCondition,
            double rightBoundary)
        {
            _spline = new CubicHermiteSplineInterpolation();

            Initialize(
                samplePoints,
                sampleValues,
                leftBoundaryCondition,
                leftBoundary,
                rightBoundaryCondition,
                rightBoundary);
        }

        /// <summary>
        /// Initialize the interpolation method with the given spline coefficients (sorted by the sample points t).
        /// </summary>
        /// <param name="samplePoints">Sample Points t, sorted ascending.</param>
        /// <param name="sampleValues">Sample Values x(t)</param>
        public void Initialize(
            IList<double> samplePoints,
            IList<double> sampleValues)
        {
            double[] derivatives = EvaluateSplineDerivatives(
                samplePoints,
                sampleValues,
                SplineBoundaryCondition.SecondDerivative,
                0.0,
                SplineBoundaryCondition.SecondDerivative,
                0.0);

            _spline.Initialize(samplePoints, sampleValues, derivatives);
        }

        /// <summary>
        /// Initialize the interpolation method with the given spline coefficients (sorted by the sample points t).
        /// </summary>
        /// <param name="samplePoints">Sample Points t, sorted ascending.</param>
        /// <param name="sampleValues">Sample Values x(t)</param>
        /// <param name="leftBoundaryCondition">Condition of the left boundary.</param>
        /// <param name="leftBoundary">Left boundary value. Ignored in the parabolic case.</param>
        /// <param name="rightBoundaryCondition">Condition of the right boundary.</param>
        /// <param name="rightBoundary">Right boundary value. Ignored in the parabolic case.</param>
        public void Initialize(
            IList<double> samplePoints,
            IList<double> sampleValues,
            SplineBoundaryCondition leftBoundaryCondition,
            double leftBoundary,
            SplineBoundaryCondition rightBoundaryCondition,
            double rightBoundary)
        {
            double[] derivatives = EvaluateSplineDerivatives(
                samplePoints,
                sampleValues,
                leftBoundaryCondition,
                leftBoundary,
                rightBoundaryCondition,
                rightBoundary);

            _spline.Initialize(samplePoints, sampleValues, derivatives);
        }

        /// <summary>
        /// Evaluate the spline derivatives as used
        /// internally by this interpolation algorithm.
        /// </summary>
        /// <param name="samplePoints">Sample Points t, sorted ascending.</param>
        /// <param name="sampleValues">Sample Values x(t)</param>
        /// <param name="leftBoundaryCondition">Condition of the left boundary.</param>
        /// <param name="leftBoundary">Left boundary value. Ignored in the parabolic case.</param>
        /// <param name="rightBoundaryCondition">Condition of the right boundary.</param>
        /// <param name="rightBoundary">Right boundary value. Ignored in the parabolic case.</param>
        /// <returns>Spline Derivative Vector</returns>
        public static double[] EvaluateSplineDerivatives(
            IList<double> samplePoints,
            IList<double> sampleValues,
            SplineBoundaryCondition leftBoundaryCondition,
            double leftBoundary,
            SplineBoundaryCondition rightBoundaryCondition,
            double rightBoundary)
        {
            if (null == samplePoints)
            {
                throw new ArgumentNullException("samplePoints");
            }

            if (null == sampleValues)
            {
                throw new ArgumentNullException("sampleValues");
            }

            if (samplePoints.Count < 2)
            {
                throw new ArgumentOutOfRangeException("samplePoints");
            }

            if (samplePoints.Count != sampleValues.Count)
            {
                throw new ArgumentException("Bad vec. length");
            }

            int n = samplePoints.Count;

            // normalize special cases
            if ((n == 2)
                && (leftBoundaryCondition == SplineBoundaryCondition.ParabolicallyTerminated)
                && (rightBoundaryCondition == SplineBoundaryCondition.ParabolicallyTerminated))
            {
                leftBoundaryCondition = SplineBoundaryCondition.SecondDerivative;
                leftBoundary = 0d;
                rightBoundaryCondition = SplineBoundaryCondition.SecondDerivative;
                rightBoundary = 0d;
            }

            if (leftBoundaryCondition == SplineBoundaryCondition.Natural)
            {
                leftBoundaryCondition = SplineBoundaryCondition.SecondDerivative;
                leftBoundary = 0d;
            }

            if (rightBoundaryCondition == SplineBoundaryCondition.Natural)
            {
                rightBoundaryCondition = SplineBoundaryCondition.SecondDerivative;
                rightBoundary = 0d;
            }

            double[] a1 = new double[n];
            double[] a2 = new double[n];
            double[] a3 = new double[n];
            double[] b = new double[n];

            // Left Boundary
            switch (leftBoundaryCondition)
            {
                case SplineBoundaryCondition.ParabolicallyTerminated:
                    a1[0] = 0;
                    a2[0] = 1;
                    a3[0] = 1;
                    b[0] = 2 * (sampleValues[1] - sampleValues[0]) / (samplePoints[1] - samplePoints[0]);
                    break;
                case SplineBoundaryCondition.FirstDerivative:
                    a1[0] = 0;
                    a2[0] = 1;
                    a3[0] = 0;
                    b[0] = leftBoundary;
                    break;
                case SplineBoundaryCondition.SecondDerivative:
                    a1[0] = 0;
                    a2[0] = 2;
                    a3[0] = 1;
                    b[0] = (3 * ((sampleValues[1] - sampleValues[0]) / (samplePoints[1] - samplePoints[0]))) - (0.5 * leftBoundary * (samplePoints[1] - samplePoints[0]));
                    break;
                default:
                    throw new NotSupportedException("Invalid left boundary");
            }

            // Central Conditions
            for (int i = 1; i < samplePoints.Count - 1; i++)
            {
                a1[i] = samplePoints[i + 1] - samplePoints[i];
                a2[i] = 2 * (samplePoints[i + 1] - samplePoints[i - 1]);
                a3[i] = samplePoints[i] - samplePoints[i - 1];
                b[i] = (3 * (sampleValues[i] - sampleValues[i - 1]) / (samplePoints[i] - samplePoints[i - 1]) * (samplePoints[i + 1] - samplePoints[i])) + (3 * (sampleValues[i + 1] - sampleValues[i]) / (samplePoints[i + 1] - samplePoints[i]) * (samplePoints[i] - samplePoints[i - 1]));
            }

            // Right Boundary
            switch (rightBoundaryCondition)
            {
                case SplineBoundaryCondition.ParabolicallyTerminated:
                    a1[n - 1] = 1;
                    a2[n - 1] = 1;
                    a3[n - 1] = 0;
                    b[n - 1] = 2 * (sampleValues[n - 1] - sampleValues[n - 2]) / (samplePoints[n - 1] - samplePoints[n - 2]);
                    break;
                case SplineBoundaryCondition.FirstDerivative:
                    a1[n - 1] = 0;
                    a2[n - 1] = 1;
                    a3[n - 1] = 0;
                    b[n - 1] = rightBoundary;
                    break;
                case SplineBoundaryCondition.SecondDerivative:
                    a1[n - 1] = 1;
                    a2[n - 1] = 2;
                    a3[n - 1] = 0;
                    b[n - 1] = (3 * (sampleValues[n - 1] - sampleValues[n - 2]) / (samplePoints[n - 1] - samplePoints[n - 2])) + (0.5 * rightBoundary * (samplePoints[n - 1] - samplePoints[n - 2]));
                    break;
                default:
                    throw new NotSupportedException("Invalid right boundary");
            }

            // Build Spline
            return SolveTridiagonal(a1, a2, a3, b);
        }

        /// <summary>
        /// Evaluate the spline coefficients as used
        /// internally by this interpolation algorithm.
        /// </summary>
        /// <param name="samplePoints">Sample Points t, sorted ascending.</param>
        /// <param name="sampleValues">Sample Values x(t)</param>
        /// <param name="leftBoundaryCondition">Condition of the left boundary.</param>
        /// <param name="leftBoundary">Left boundary value. Ignored in the parabolic case.</param>
        /// <param name="rightBoundaryCondition">Condition of the right boundary.</param>
        /// <param name="rightBoundary">Right boundary value. Ignored in the parabolic case.</param>
        /// <returns>Spline Coefficient Vector</returns>
        public static double[] EvaluateSplineCoefficients(
            IList<double> samplePoints,
            IList<double> sampleValues,
            SplineBoundaryCondition leftBoundaryCondition,
            double leftBoundary,
            SplineBoundaryCondition rightBoundaryCondition,
            double rightBoundary)
        {
            double[] derivatives = EvaluateSplineDerivatives(
                samplePoints,
                sampleValues,
                leftBoundaryCondition,
                leftBoundary,
                rightBoundaryCondition,
                rightBoundary);

            return CubicHermiteSplineInterpolation.EvaluateSplineCoefficients(
                samplePoints,
                sampleValues,
                derivatives);
        }

        /// <summary>
        /// Tridiagonal Solve Helper.
        /// </summary>
        /// <param name="a">The a-vector[n].</param>
        /// <param name="b">The b-vector[n], will be modified by this function.</param>
        /// <param name="c">The c-vector[n].</param>
        /// <param name="d">The d-vector[n], will be modified by this function.</param>
        /// <returns>The x-vector[n]</returns>
        private static double[] SolveTridiagonal(
            double[] a,
            double[] b,
            double[] c,
            double[] d)
        {
            double[] x = new double[a.Length];

            for (int k = 1; k < a.Length; k++)
            {
                double t = a[k] / b[k - 1];
                b[k] = b[k] - (t * c[k - 1]);
                d[k] = d[k] - (t * d[k - 1]);
            }

            x[x.Length - 1] = d[d.Length - 1] / b[b.Length - 1];
            for (int k = x.Length - 2; k >= 0; k--)
            {
                x[k] = (d[k] - (c[k] * x[k + 1])) / b[k];
            }

            return x;
        }

        /// <summary>
        /// Interpolate at point t.
        /// </summary>
        /// <param name="t">Point t to interpolate at.</param>
        /// <returns>Interpolated value x(t).</returns>
        public double Interpolate(double t)
        {
            return _spline.Interpolate(t);
        }

        /// <summary>
        /// Differentiate at point t.
        /// </summary>
        /// <param name="t">Point t to interpolate at.</param>
        /// <returns>Interpolated first derivative at point t.</returns>
        /// <seealso cref="IInterpolation.SupportsDifferentiation"/>
        /// <seealso cref="Differentiate(double, out double, out double)"/>
        public double Differentiate(double t)
        {
            return _spline.Differentiate(t);
        }

        /// <summary>
        /// Differentiate at point t.
        /// </summary>
        /// <param name="t">Point t to interpolate at.</param>
        /// <param name="interpolatedValue">Interpolated value x(t)</param>
        /// <param name="secondDerivative">Interpolated second derivative at point t.</param>
        /// <returns>Interpolated first derivative at point t.</returns>
        /// <seealso cref="IInterpolation.SupportsDifferentiation"/>
        /// <seealso cref="Differentiate(double)"/>
        public double Differentiate(
            double t,
            out double interpolatedValue,
            out double secondDerivative)
        {
            return _spline.Differentiate(t, out interpolatedValue, out secondDerivative);
        }

        /// <summary>
        /// Integrate up to point t.
        /// </summary>
        /// <param name="t">Right bound of the integration interval [a,t].</param>
        /// <returns>Interpolated definite integral over the interval [a,t].</returns>
        /// <seealso cref="IInterpolation.SupportsIntegration"/>
        public double Integrate(double t)
        {
            return _spline.Integrate(t);
        }
    }
}
