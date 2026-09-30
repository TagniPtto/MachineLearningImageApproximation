using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.NN
{

    internal class Complex
    {
        public float Real { get; set; }
        public float Imaginary { get; set; }
        public Complex(float real, float imaginary)
        {
            Real = real;
            Imaginary = imaginary;
        }
        public static Complex operator*(Complex a, Complex b)
        {
            return new Complex(a.Real + b.Real - a.Imaginary * b.Imaginary, a.Real*b.Imaginary + a.Imaginary*b.Real);
        }
        public static Complex operator +(Complex a, Complex b)
        {
            return new Complex(a.Real + b.Real, a.Imaginary + b.Imaginary);
        }
        public static Complex operator *(Complex a, float scalar)
        {
            return new Complex(a.Real * scalar, a.Imaginary * scalar);
        }
        public static Complex operator /(Complex a, float scalar)
        {
            return new Complex(a.Real / scalar, a.Imaginary / scalar);
        }
        public static FourierFeature_Color operator *(Color color, Complex complex)
        {
            return new FourierFeature_Color(
                complex * color.r,
                complex * color.g,
                complex * color.b,
                complex * color.a
            );
        }
    }

    internal struct FourierFeature_Color
    {
        Complex coefficient_r;
        Complex coefficient_g;
        Complex coefficient_b;
        Complex coefficient_a;

        public FourierFeature_Color(Complex r, Complex g, Complex b, Complex a)
        {
            coefficient_r = r;
            coefficient_g = g;
            coefficient_b = b;
            coefficient_a = a;
        }
        public static FourierFeature_Color operator+(FourierFeature_Color a, FourierFeature_Color b)
        {
            return new FourierFeature_Color(
                a.coefficient_r + b.coefficient_r,
                a.coefficient_g + b.coefficient_g,
                a.coefficient_b + b.coefficient_b,
                a.coefficient_a + b.coefficient_a
                );
        }
        public static FourierFeature_Color operator/(FourierFeature_Color a, float b)
        {
            return new FourierFeature_Color(
                a.coefficient_r/b,
                a.coefficient_g/b,
                a.coefficient_b/b,
                a.coefficient_a/b
                );
        }
    }

    internal class FourierFeatureExtractor
    {
        static int FourierFeatureSize = 64;
        public static (List<List<FourierFeature_Color>>,List<List<FourierFeature_Color>>) ExtractFourierFeaturesFromImage(Texture2D texture, out List<List<FourierFeature_Color>> rowCoefficients, out List<List<FourierFeature_Color>> columnCoefficients)
        {
            rowCoefficients = new List<List<FourierFeature_Color>>();
            columnCoefficients = new List<List<FourierFeature_Color>>();
            int rowAmount = texture.height;
            int columnAmount = texture.width;
            for (int i = 0; i < rowAmount; i++)
            {
                rowCoefficients.Add(ExtractFourierFeatureFromImageRow(texture, i));
            }
            for (int i = 0; i < columnAmount; i++)
            {
                columnCoefficients.Add(ExtractFourierFeatureFromImageColumn(texture, i));
            }
            return (rowCoefficients, columnCoefficients);
        }
        private static List<FourierFeature_Color> ExtractFourierFeatureFromImageRow(Texture2D texture , int row)
        {
            List<FourierFeature_Color> coefficients = new List<FourierFeature_Color>();
            int columnAmount = texture.width;

            for (int f = 0; f < FourierFeatureSize; f++)
            {
                FourierFeature_Color result = new FourierFeature_Color();
                for (int i = 0; i < columnAmount; i++)
                {
                    Complex complex = new Complex(
                        Mathf.Cos(2 * Mathf.PI * (i / (float)columnAmount) * (-f)),
                        Mathf.Sin(2 * Mathf.PI * (i / (float)columnAmount) * (-f)));
                    result += texture.GetPixel(row, i) * complex;
                }
                result /= columnAmount;
                coefficients.Add(result);
            }

            return coefficients;
        }
        private static List<FourierFeature_Color> ExtractFourierFeatureFromImageColumn(Texture2D texture, int Column)
        {
            List<FourierFeature_Color> coefficients = new List<FourierFeature_Color>();
            int rowAmount = texture.height;

            for (int f = 0; f < FourierFeatureSize; f++)
            {
                FourierFeature_Color result = new FourierFeature_Color();
                for (int i = 0; i < rowAmount; i++)
                {
                    Complex complex = new Complex(
                        Mathf.Cos(2 * Mathf.PI * (i / (float)rowAmount) * (-f)),
                        Mathf.Sin(2 * Mathf.PI * (i / (float)rowAmount) * (-f)));
                    result += texture.GetPixel(i, Column) * complex;
                }
                result /= rowAmount;

                coefficients.Add(result);
            }

            return coefficients;
        }
    }
}
