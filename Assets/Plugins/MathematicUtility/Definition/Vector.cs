using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace MathematicUtility.Definition
{
    // --- float vector type --- //
    public struct Vectorf
    {
        // define the vectorf struct
        public float[] vectorf;
        public int size;
        public float this[int index]
        {
            get { return vectorf[index]; }
            set { vectorf[index] = value; }
        }

        // struct constructor
        public Vectorf(int num)
        {
            if(num == 0)
            {
                throw new ArgumentException("Can not create empty vectorf.", nameof(num));
            }
            this.vectorf = new float[num];
            this.size = this.vectorf.Length;
        }
        public Vectorf(float[] values)
        {
            this.vectorf = values;
            this.size = this.vectorf.Length;
            if(this.size == 0)
            {
                throw new ArgumentException("Can not create empty vectorf.", nameof(this.size));
            }
        }

        // define the struct method
        // set zeros method
        public static Vectorf Zeros(Vectorf a)
        {
            if(a.size == 0)
            {
                throw new ArgumentException("Can not set empty vectorf to zeros.", nameof(a));
            }

            // Vectorf vf = new Vectorf();
            Vectorf vf = new Vectorf(a.size);
            for(int i = 0; i < a.size; i++)
            {
                vf.vectorf[i] = 0.0f;
            }

            return vf;
        }
        public static Vectorf Zeros(int size)
        {
            if(size == 0)
            {
                throw new ArgumentException("Can not set empty vectorf to zeros.", nameof(size));
            }

            // Vectorf vf = new Vectorf();
            Vectorf vf = new Vectorf(size);
            for(int i = 0; i < size; i++)
            {
                vf.vectorf[i] = 0.0f;
            }

            return vf;
        }

        // set ones method
        public static Vectorf Ones(Vectorf a)
        {
            if(a.size == 0)
            {
                throw new ArgumentException("Can not set empty vectorf to ones.", nameof(a));
            }

            // Vectorf vf = new Vectorf();
            Vectorf vf = new Vectorf(a.size);
            for(int i = 0; i < a.size; i++)
            {
                vf.vectorf[i] = 1.0f;
            }

            return vf;
        }
        public static Vectorf Ones(int size)
        {
            if(size == 0)
            {
                throw new ArgumentException("Can not set empty vectorf to ones.", nameof(size));
            }

            // Vectorf vf = new Vectorf();
            Vectorf vf = new Vectorf(size);
            for(int i = 0; i < size; i++)
            {
                vf.vectorf[i] = 1.0f;
            }

            return vf;
        }

        // add method
        public static Vectorf operator +(Vectorf a, Vectorf b)
        {
            if(a.size != b.size)
            {
                throw new ArgumentException("Can not add two vectorf with different length.", nameof(a));
            }

            // Vectorf vf = new Vectorf();
            Vectorf vf = new Vectorf(a.size);
            for(int i = 0; i < a.size; i++)
            {
                vf.vectorf[i] = a.vectorf[i] + b.vectorf[i];
            }

            return vf;
        }
        public static Vectorf operator +(Vectorf a, float b)
        {
            if(b.GetType() != typeof(float))
            {
                throw new ArgumentException("Wrong number, the vectorf must add float type number.", nameof(b));
            }

            // Vectorf vf = new Vectorf();
            Vectorf vf = new Vectorf(a.size);
            for(int i = 0; i < a.size; i++)
            {
                vf.vectorf[i] = a.vectorf[i] + b;
            }

            return vf;
        }

        // subtract method
        public static Vectorf operator -(Vectorf a, Vectorf b)
        {
            if(a.size != b.size)
            {
                throw new ArgumentException("Can not subtract two vectorf with different length.", nameof(a));
            }

            // Vectorf vf = new Vectorf();
            Vectorf vf = new Vectorf(a.size);
            for(int i = 0; i < a.size; i++)
            {
                vf.vectorf[i] = a.vectorf[i] - b.vectorf[i];
            }

            return vf;
        }
        public static Vectorf operator -(Vectorf a, float b)
        {
            if(b.GetType() != typeof(float))
            {
                throw new ArgumentException("Wrong number, the vectorf must subtract float type number.", nameof(b));
            }

            // Vectorf vf = new Vectorf();
            Vectorf vf = new Vectorf(a.size);
            for(int i = 0; i < a.size; i++)
            {
                vf.vectorf[i] = a.vectorf[i] - b;
            }

            return vf;
        }

        // multiply method
        public static Vectorf operator *(Vectorf a, Vectorf b)
        {
            if(a.size != b.size)
            {
                throw new ArgumentException("Can not multiply two vectorf with different length.", nameof(a));
            }

            // Vectorf vf = new Vectorf();
            Vectorf vf = new Vectorf(a.size);
            for(int i = 0; i < a.size; i++)
            {
                vf.vectorf[i] = a.vectorf[i] * b.vectorf[i];
            }

            return vf;
        }
        public static Vectorf operator *(Vectorf a, float b)
        {
            if(b.GetType() != typeof(float))
            {
                throw new ArgumentException("Wrong number, the vectorf must multiply float type number.", nameof(b));
            }

            // Vectorf vf = new Vectorf();
            Vectorf vf = new Vectorf(a.size);
            for(int i = 0; i < a.size; i++)
            {
                vf.vectorf[i] = a.vectorf[i] * b;
            }

            return vf;
        }
        public static float Dot(Vectorf a, Vectorf b)
        {
            if(a.size != b.size)
            {
                throw new ArgumentException("Can not multiply two vectorf with different length.", nameof(a));
            }

            float sum = 0.0f;
            for(int i = 0; i < a.size; i++)
            {
                sum = sum + a.vectorf[i] * b.vectorf[i]; // a Dot b = dot product(a,b)
            }

            return sum;
        }

        // divide method
        public static Vectorf operator /(Vectorf a, Vectorf b)
        {
            if(a.size != b.size)
            {
                throw new ArgumentException("Can not divide two vectorf with different length.", nameof(a));
            }

            // Vectorf vf = new Vectorf();
            Vectorf vf = new Vectorf(a.size);
            for(int i = 0; i < a.size; i++)
            {
                vf.vectorf[i] = a.vectorf[i] / b.vectorf[i];
            }

            return vf;
        }
        public static Vectorf operator /(Vectorf a, float b)
        {
            if(b.GetType() != typeof(float))
            {
                throw new ArgumentException("Wrong number, the vectorf must divide float type number.", nameof(b));
            }

            // Vectorf vf = new Vectorf();
            Vectorf vf = new Vectorf(a.size);
            for(int i = 0; i < a.size; i++)
            {
                vf.vectorf[i] = a.vectorf[i] / b;
            }

            return vf;
        }
        public static float Div(Vectorf a, Vectorf b)
        {
            if(a.size != b.size)
            {
                throw new ArgumentException("Can not divide two vectorf with different length.", nameof(a));
            }

            float sum = 0.0f;
            for(int i = 0; i < a.size; i++)
            {
                sum = sum + a.vectorf[i] * b.vectorf[i]; // a/b = a*Inverse(b)
            }

            return sum;
        }
        
        // equal method
        public static bool operator ==(Vectorf a, Vectorf b)
        {
            bool status = false;
            if(a.size != b.size)
            {
                throw new ArgumentException("Can not compare two vectorf with different length.", nameof(a));
                return status;
            }
            for(int i = 0; i < a.size; i++)
            {
                if(a.vectorf[i] == b.vectorf[i])
                {
                    status = true;
                }
                else
                {
                    status = false;
                    break;
                }
            }

            return status;
        }

        // not equal method
        public static bool operator !=(Vectorf a, Vectorf b)
        {
            bool status = true;
            if(a.size != b.size)
            {
                throw new ArgumentException("Can not compare two vectorf with different length.", nameof(a));
                return status;
            }
            for(int i = 0; i < a.size; i++)
            {
                if(a.vectorf[i] == b.vectorf[i])
                {
                    status = false;
                }
                else
                {
                    status = true;
                    break;
                }
            }

            return status;
        }
    }

    // --- double vector type --- //
    public struct Vectord
    {
        // define the vector struct
        public double[] vectord;
        public int size;
        public double this[int index]
        {
            get { return vectord[index]; }
            set { vectord[index] = value; }
        }

        // struct constructor
        public Vectord(int num)
        {
            if(num == 0)
            {
                throw new ArgumentException("Can not create empty vectord.", nameof(num));
            }
            this.vectord = new double[num];
            this.size = this.vectord.Length;
        }
        public Vectord(double[] values)
        {
            this.vectord = values;
            this.size = this.vectord.Length;
            if(this.size == 0)
            {
                throw new ArgumentException("Can not create empty vectord.", nameof(this.size));
            }
        }

        // define the struct method
        // set zeros method
        public static Vectord Zeros(Vectord a)
        {
            if(a.size == 0)
            {
                throw new ArgumentException("Can not set empty vectord to zeros.", nameof(a));
            }

            // Vectord vd = new Vectord();
            Vectord vd = new Vectord(a.size);
            for(int i = 0; i < a.size; i++)
            {
                vd.vectord[i] = 0.0;
            }

            return vd;
        }
        public static Vectord Zeros(int size)
        {
            if(size == 0)
            {
                throw new ArgumentException("Can not set empty vectord to zeros.", nameof(size));
            }

            // Vectord vd = new Vectord();
            Vectord vd = new Vectord(size);
            for(int i = 0; i < size; i++)
            {
                vd.vectord[i] = 0.0;
            }

            return vd;
        }

        // set ones method
        public static Vectord Ones(Vectord a)
        {
            if(a.size == 0)
            {
                throw new ArgumentException("Can not set empty vectord to ones.", nameof(a));
            }

            // Vectord vd = new Vectord();
            Vectord vd = new Vectord(a.size);
            for(int i = 0; i < a.size; i++)
            {
                vd.vectord[i] = 1.0;
            }

            return vd;
        }
        public static Vectord Ones(int size)
        {
            if(size == 0)
            {
                throw new ArgumentException("Can not set empty vectord to ones.", nameof(size));
            }

            // Vectord vd = new Vectord();
            Vectord vd = new Vectord(size);
            for(int i = 0; i < size; i++)
            {
                vd.vectord[i] = 1.0;
            }

            return vd;
        }

        // add method
        public static Vectord operator +(Vectord a, Vectord b)
        {
            if(a.size != b.size)
            {
                throw new ArgumentException("Can not add two vectord with different length.", nameof(a));
            }

            // Vectord vd = new Vectord();
            Vectord vd = new Vectord(a.size);
            for(int i = 0; i < a.size; i++)
            {
                vd.vectord[i] = a.vectord[i] + b.vectord[i];
            }

            return vd;
        }
        public static Vectord operator +(Vectord a, double b)
        {
            if(b.GetType() != typeof(double))
            {
                throw new ArgumentException("Wrong number, the vectord must add double type number.", nameof(b));
            }

            // Vectord vd = new Vectord();
            Vectord vd = new Vectord(a.size);
            for(int i = 0; i < a.size; i++)
            {
                vd.vectord[i] = a.vectord[i] + b;
            }

            return vd;
        }

        // subtract method
        public static Vectord operator -(Vectord a, Vectord b)
        {
            if(a.size != b.size)
            {
                throw new ArgumentException("Can not subtract two vectord with different length.", nameof(a));
            }

            // Vectord vd = new Vectord();
            Vectord vd = new Vectord(a.size);
            for(int i = 0; i < a.size; i++)
            {
                vd.vectord[i] = a.vectord[i] - b.vectord[i];
            }

            return vd;
        }
        public static Vectord operator -(Vectord a, double b)
        {
            if(b.GetType() != typeof(double))
            {
                throw new ArgumentException("Wrong number, the vectord must subtract double type number.", nameof(b));
            }

            // Vectord vd = new Vectord();
            Vectord vd = new Vectord(a.size);
            for(int i = 0; i < a.size; i++)
            {
                vd.vectord[i] = a.vectord[i] - b;
            }

            return vd;
        }

        // multiply method
        public static Vectord operator *(Vectord a, Vectord b)
        {
            if(a.size != b.size)
            {
                throw new ArgumentException("Can not multiply two vectord with different length.", nameof(a));
            }

            // Vectord vd = new Vectord();
            Vectord vd = new Vectord(a.size);
            for(int i = 0; i < a.size; i++)
            {
                vd.vectord[i] = a.vectord[i] * b.vectord[i];
            }

            return vd;
        }
        public static Vectord operator *(Vectord a, double b)
        {
            if(b.GetType() != typeof(double))
            {
                throw new ArgumentException("Wrong number, the vectord must multiply double type number.", nameof(b));
            }

            // Vectord vd = new Vectord();
            Vectord vd = new Vectord(a.size);
            for(int i = 0; i < a.size; i++)
            {
                vd.vectord[i] = a.vectord[i] * b;
            }

            return vd;
        }
        public static double Dot(Vectord a, Vectord b)
        {
            if(a.size != b.size)
            {
                throw new ArgumentException("Can not multiply two vectord with different length.", nameof(a));
            }

            double sum = 0.0;
            for(int i = 0; i < a.size; i++)
            {
                sum = sum + a.vectord[i] * b.vectord[i]; // a Dot b = dot product(a,b)
            }

            return sum;
        }

        // divide method
        public static Vectord operator /(Vectord a, Vectord b)
        {
            if(a.size != b.size)
            {
                throw new ArgumentException("Can not divide two vectord with different length.", nameof(a));
            }

            // Vectord vd = new Vectord();
            Vectord vd = new Vectord(a.size);
            for(int i = 0; i < a.size; i++)
            {
                vd.vectord[i] = a.vectord[i] / b.vectord[i];
            }

            return vd;
        }
        public static Vectord operator /(Vectord a, double b)
        {
            if(b.GetType() != typeof(double))
            {
                throw new ArgumentException("Wrong number, the vectord must divide double type number.", nameof(b));
            }

            // Vectord vd = new Vectord();
            Vectord vd = new Vectord(a.size);
            for(int i = 0; i < a.size; i++)
            {
                vd.vectord[i] = a.vectord[i] / b;
            }

            return vd;
        }
        public static double Div(Vectord a, Vectord b)
        {
            if(a.size != b.size)
            {
                throw new ArgumentException("Can not divide two vectord with different length.", nameof(a));
            }

            double sum = 0.0;
            for(int i = 0; i < a.size; i++)
            {
                sum = sum + a.vectord[i] * b.vectord[i]; // a/b = a*Inverse(b)
            }

            return sum;
        }
        
        // equal method
        public static bool operator ==(Vectord a, Vectord b)
        {
            bool status = false;
            if(a.size != b.size)
            {
                throw new ArgumentException("Can not compare two vectord with different length.", nameof(a));
                return status;
            }
            for(int i = 0; i < a.size; i++)
            {
                if(a.vectord[i] == b.vectord[i])
                {
                    status = true;
                }
                else
                {
                    status = false;
                    break;
                }
            }

            return status;
        }

        // not equal method
        public static bool operator !=(Vectord a, Vectord b)
        {
            bool status = true;
            if(a.size != b.size)
            {
                throw new ArgumentException("Can not compare two vectord with different length.", nameof(a));
                return status;
            }
            for(int i = 0; i < a.size; i++)
            {
                if(a.vectord[i] == b.vectord[i])
                {
                    status = false;
                }
                else
                {
                    status = true;
                    break;
                }
            }

            return status;
        }
    }
}
