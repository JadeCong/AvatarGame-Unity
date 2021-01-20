using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace MathematicUtility.Definition
{
    // float array type //
    public struct Arrayf
    {
        // define the array struct(3 dimensions)
        public float[,,] arrayf;
        public int[] dims;
        // public int lens;
        public float this[int idxi, int idxj, int idxk]
        {
            get { return arrayf[idxi,idxj,idxk]; }
            set { arrayf[idxi,idxj,idxk] = value; }
        }

        // struct constructor
        public Arrayf(int[] dim)
        {
            for(int i = 0; i < dim.Length; i++)
            {
                if(dim[i] == 0)
                {
                    throw new ArgumentException("Can not create arrayf with zero element in some dimension.", nameof(dim));
                }
            }
            this.arrayf = new float[dim[0],dim[1],dim[2]];
            this.dims = dim;
        }
        public Arrayf(float[,,] values) : this()
        {
            this.arrayf = values;
            for(int i = 0; i < values.Length; i++)
            {
                this.dims[i] = this.arrayf.GetLength(i);
                if(this.dims[i] == 0)
                {
                    throw new ArgumentException("Can not create arrayf with zero element in some dimension.", nameof(this.dims));
                }
            }
        }

        // define the struct method
        // set zeros method
        public static Arrayf Zeros(Arrayf a)
        {
            for(int m = 0; m < a.dims.Length; m++)
            {
                if(a.dims[m] == 0)
                {
                    throw new ArgumentException("Can not set arrayf to zeros in some dimension.", nameof(a.dims));
                }
            }

            Arrayf af = new Arrayf();
            for(int i = 0; i < a.dims[0]; i++)
            {
                for(int j = 0; j < a.dims[1]; j++)
                {
                    for(int k = 0; k < a.dims[2]; k++)
                    {
                        af.arrayf[i,j,k] = 0.0f;
                    }
                }
            }

            return af;
        }

        // set ones method
        public static Arrayf Ones(Arrayf a)
        {
            for(int m = 0; m < a.dims.Length; m++)
            {
                if(a.dims[m] == 0)
                {
                    throw new ArgumentException("Can not set arrayf to zeros in some dimension.", nameof(a.dims));
                }
            }

            Arrayf af = new Arrayf();
            for(int i = 0; i < a.dims[0]; i++)
            {
                for(int j = 0; j < a.dims[1]; j++)
                {
                    for(int k = 0; k < a.dims[2]; k++)
                    {
                        af.arrayf[i,j,k] = 1.0f;
                    }
                }
            }

            return af;
        }

        // add method
        public static Arrayf operator +(Arrayf a, Arrayf b)
        {
            for(int m = 0; m < a.dims.Length; m++)
            {
                if(a.dims[m] != b.dims[m])
                {
                    throw new ArgumentException("Can not add two arrayf with different element in some dimension.", nameof(a));
                }
            }

            Arrayf af = new Arrayf();
            for(int i = 0; i < a.dims[0]; i++)
            {
                for(int j = 0; j < a.dims[1]; j++)
                {
                    for(int k = 0; k < a.dims[2]; k++)
                    {
                        af.arrayf[i,j,k] = a.arrayf[i,j,k] + b.arrayf[i,j,k];
                    }
                }
            }

            return af;
        }
        public static Arrayf operator +(Arrayf a, float b)
        {
            if(b.GetType() != typeof(float))
            {
                throw new ArgumentException("Wrong number, the arrayf must add float type number.", nameof(b));
            }

            Arrayf af = new Arrayf();
            for(int i = 0; i < a.dims[0]; i++)
            {
                for(int j = 0; j < a.dims[1]; j++)
                {
                    for(int k = 0; k < a.dims[2]; k++)
                    {
                        af.arrayf[i,j,k] = a.arrayf[i,j,k] + b;
                    }
                }
            }

            return af;
        }

        // subtract method
        public static Arrayf operator -(Arrayf a, Arrayf b)
        {
            for(int m = 0; m < a.dims.Length; m++)
            {
                if(a.dims[m] != b.dims[m])
                {
                    throw new ArgumentException("Can not subtract two arrayf with different element in some dimension.", nameof(a));
                }
            }

            Arrayf af = new Arrayf();
            for(int i = 0; i < a.dims[0]; i++)
            {
                for(int j = 0; j < a.dims[1]; j++)
                {
                    for(int k = 0; k < a.dims[2]; k++)
                    {
                        af.arrayf[i,j,k] = a.arrayf[i,j,k] - b.arrayf[i,j,k];
                    }
                }
            }

            return af;
        }
        public static Arrayf operator -(Arrayf a, float b)
        {
            if(b.GetType() != typeof(float))
            {
                throw new ArgumentException("Wrong number, the arrayf must subtract float type number.", nameof(b));
            }

            Arrayf af = new Arrayf();
            for(int i = 0; i < a.dims[0]; i++)
            {
                for(int j = 0; j < a.dims[1]; j++)
                {
                    for(int k = 0; k < a.dims[2]; k++)
                    {
                        af.arrayf[i,j,k] = a.arrayf[i,j,k] - b;
                    }
                }
            }

            return af;
        }

        // multiply method
        public static Arrayf operator *(Arrayf a, Arrayf b)
        {
            for(int m = 0; m < a.dims.Length; m++)
            {
                if(a.dims[m] != b.dims[m])
                {
                    throw new ArgumentException("Can not multiply two arrayf with different element in some dimension.", nameof(a));
                }
            }

            Arrayf af = new Arrayf();
            for(int i = 0; i < a.dims[0]; i++)
            {
                for(int j = 0; j < a.dims[1]; j++)
                {
                    for(int k = 0; k < a.dims[2]; k++)
                    {
                        af.arrayf[i,j,k] = a.arrayf[i,j,k] * b.arrayf[i,j,k];
                    }
                }
            }

            return af;
        }
        public static Arrayf operator *(Arrayf a, float b)
        {
            if(b.GetType() != typeof(float))
            {
                throw new ArgumentException("Wrong number, the arrayf must multiply float type number.", nameof(b));
            }

            Arrayf af = new Arrayf();
            for(int i = 0; i < a.dims[0]; i++)
            {
                for(int j = 0; j < a.dims[1]; j++)
                {
                    for(int k = 0; k < a.dims[2]; k++)
                    {
                        af.arrayf[i,j,k] = a.arrayf[i,j,k] * b;
                    }
                }
            }

            return af;
        }
        // TODO: mul
        public static Arrayf Mul(Arrayf a, Arrayf b)
        {
            Arrayf af = new Arrayf();
            
            return af;
        }

        // divide method
        public static Arrayf operator /(Arrayf a, Arrayf b)
        {
            for(int m = 0; m < a.dims.Length; m++)
            {
                if(a.dims[m] != b.dims[m])
                {
                    throw new ArgumentException("Can not divide two arrayf with different element in some dimension.", nameof(a));
                }
            }

            Arrayf af = new Arrayf();
            for(int i = 0; i < a.dims[0]; i++)
            {
                for(int j = 0; j < a.dims[1]; j++)
                {
                    for(int k = 0; k < a.dims[2]; k++)
                    {
                        af.arrayf[i,j,k] = a.arrayf[i,j,k] / b.arrayf[i,j,k];
                    }
                }
            }

            return af;
        }
        public static Arrayf operator /(Arrayf a, float b)
        {
            if(b.GetType() != typeof(float))
            {
                throw new ArgumentException("Wrong number, the arrayf must divide float type number.", nameof(b));
            }

            Arrayf af = new Arrayf();
            for(int i = 0; i < a.dims[0]; i++)
            {
                for(int j = 0; j < a.dims[1]; j++)
                {
                    for(int k = 0; k < a.dims[2]; k++)
                    {
                        af.arrayf[i,j,k] = a.arrayf[i,j,k] / b;
                    }
                }
            }

            return af;
        }
        // TODO: div
        public static Arrayf Div(Arrayf a, Arrayf b)
        {
            Arrayf af = new Arrayf();
            
            return af;
        }

        // TODO: transpose
        // get the transpose method
        public static Arrayf Transpose(Arrayf a, int[] b)
        {
            for(int m = 0; m < a.dims.Length; m++)
            {
                if(a.dims[m] == 0)
                {
                    throw new ArgumentException("Wrong arrayf, can not transpose arrayf with zero element in some dimension.", nameof(a));
                }
            }

            Arrayf af = new Arrayf();
            
            return af;
        }

        // TODO: inverse
        // get the inverse method
        public static Arrayf Inverse(Arrayf a)
        {
            Arrayf af = new Arrayf();
            
            return af;
        }

        // equal method
        public static bool operator ==(Arrayf a, Arrayf b)
        {
            bool status = false;
            for(int m = 0; m < a.dims.Length; m++)
            {
                if(a.dims[m] == b.dims[m])
                {
                    throw new ArgumentException("Can not compare two arrayf with different element in some dimension.", nameof(a));
                    return status;
                }
            }

            for(int i = 0; i < a.dims[0]; i++)
            {
                for(int j = 0; j < a.dims[1]; j++)
                {
                    for(int k = 0; k < a.dims[2]; k++)
                    {
                        if(a.arrayf[i,j,k] == b.arrayf[i,j,k])
                        {
                            status = true;
                        }
                        else
                        {
                            status = false;
                            goto label;
                        }
                    }
                }
            }
            label:;

            return status;
        }

        // not equal method
        public static bool operator !=(Arrayf a, Arrayf b)
        {
            bool status = true;
            for(int m = 0; m < a.dims.Length; m++)
            {
                if(a.dims[m] == b.dims[m])
                {
                    throw new ArgumentException("Can not compare two arrayf with different element in some dimension.", nameof(a));
                    return status;
                }
            }

            for(int i = 0; i < a.dims[0]; i++)
            {
                for(int j = 0; j < a.dims[1]; j++)
                {
                    for(int k = 0; k < a.dims[2]; k++)
                    {
                        if(a.arrayf[i,j,k] == b.arrayf[i,j,k])
                        {
                            status = false;
                        }
                        else
                        {
                            status = true;
                            goto label;
                        }
                    }
                }
            }
            label:;

            return status;
        }
    }

    // double array type //
    public struct Arrayd
    {
        // define the array struct(3 dimensions)
        public double[,,] arrayd;
        public int[] dims;
        // public int lens;
        public double this[int idxi, int idxj, int idxk]
        {
            get { return arrayd[idxi,idxj,idxk]; }
            set { arrayd[idxi,idxj,idxk] = value; }
        }

        // struct constructor
        public Arrayd(int[] dim)
        {
            for(int i = 0; i < dim.Length; i++)
            {
                if(dim[i] == 0)
                {
                    throw new ArgumentException("Can not create arrayd with zero element in some dimension.", nameof(dim));
                }
            }
            this.arrayd = new double[dim[0],dim[1],dim[2]];
            this.dims = dim;
        }
        public Arrayd(double[,,] values) : this()
        {
            this.arrayd = values;
            for(int i = 0; i < values.Length; i++)
            {
                this.dims[i] = this.arrayd.GetLength(i);
                if(this.dims[i] == 0)
                {
                    throw new ArgumentException("Can not create arrayd with zero element in some dimension.", nameof(this.dims));
                }
            }
        }

        // define the struct method
        // set zeros method
        public static Arrayd Zeros(Arrayd a)
        {
            for(int m = 0; m < a.dims.Length; m++)
            {
                if(a.dims[m] == 0)
                {
                    throw new ArgumentException("Can not set arrayd to zeros in some dimension.", nameof(a.dims));
                }
            }

            Arrayd ad = new Arrayd();
            for(int i = 0; i < a.dims[0]; i++)
            {
                for(int j = 0; j < a.dims[1]; j++)
                {
                    for(int k = 0; k < a.dims[2]; k++)
                    {
                        ad.arrayd[i,j,k] = 0.0f;
                    }
                }
            }

            return ad;
        }

        // set ones method
        public static Arrayd Ones(Arrayd a)
        {
            for(int m = 0; m < a.dims.Length; m++)
            {
                if(a.dims[m] == 0)
                {
                    throw new ArgumentException("Can not set arrayd to zeros in some dimension.", nameof(a.dims));
                }
            }

            Arrayd ad = new Arrayd();
            for(int i = 0; i < a.dims[0]; i++)
            {
                for(int j = 0; j < a.dims[1]; j++)
                {
                    for(int k = 0; k < a.dims[2]; k++)
                    {
                        ad.arrayd[i,j,k] = 1.0f;
                    }
                }
            }

            return ad;
        }

        // add method
        public static Arrayd operator +(Arrayd a, Arrayd b)
        {
            for(int m = 0; m < a.dims.Length; m++)
            {
                if(a.dims[m] != b.dims[m])
                {
                    throw new ArgumentException("Can not add two arrayd with different element in some dimension.", nameof(a));
                }
            }

            Arrayd ad = new Arrayd();
            for(int i = 0; i < a.dims[0]; i++)
            {
                for(int j = 0; j < a.dims[1]; j++)
                {
                    for(int k = 0; k < a.dims[2]; k++)
                    {
                        ad.arrayd[i,j,k] = a.arrayd[i,j,k] + b.arrayd[i,j,k];
                    }
                }
            }

            return ad;
        }
        public static Arrayd operator +(Arrayd a, double b)
        {
            if(b.GetType() != typeof(double))
            {
                throw new ArgumentException("Wrong number, the arrayd must add double type number.", nameof(b));
            }

            Arrayd ad = new Arrayd();
            for(int i = 0; i < a.dims[0]; i++)
            {
                for(int j = 0; j < a.dims[1]; j++)
                {
                    for(int k = 0; k < a.dims[2]; k++)
                    {
                        ad.arrayd[i,j,k] = a.arrayd[i,j,k] + b;
                    }
                }
            }

            return ad;
        }

        // subtract method
        public static Arrayd operator -(Arrayd a, Arrayd b)
        {
            for(int m = 0; m < a.dims.Length; m++)
            {
                if(a.dims[m] != b.dims[m])
                {
                    throw new ArgumentException("Can not subtract two arrayd with different element in some dimension.", nameof(a));
                }
            }

            Arrayd ad = new Arrayd();
            for(int i = 0; i < a.dims[0]; i++)
            {
                for(int j = 0; j < a.dims[1]; j++)
                {
                    for(int k = 0; k < a.dims[2]; k++)
                    {
                        ad.arrayd[i,j,k] = a.arrayd[i,j,k] - b.arrayd[i,j,k];
                    }
                }
            }

            return ad;
        }
        public static Arrayd operator -(Arrayd a, double b)
        {
            if(b.GetType() != typeof(double))
            {
                throw new ArgumentException("Wrong number, the arrayd must subtract double type number.", nameof(b));
            }

            Arrayd ad = new Arrayd();
            for(int i = 0; i < a.dims[0]; i++)
            {
                for(int j = 0; j < a.dims[1]; j++)
                {
                    for(int k = 0; k < a.dims[2]; k++)
                    {
                        ad.arrayd[i,j,k] = a.arrayd[i,j,k] - b;
                    }
                }
            }

            return ad;
        }

        // multiply method
        public static Arrayd operator *(Arrayd a, Arrayd b)
        {
            for(int m = 0; m < a.dims.Length; m++)
            {
                if(a.dims[m] != b.dims[m])
                {
                    throw new ArgumentException("Can not multiply two arrayd with different element in some dimension.", nameof(a));
                }
            }

            Arrayd ad = new Arrayd();
            for(int i = 0; i < a.dims[0]; i++)
            {
                for(int j = 0; j < a.dims[1]; j++)
                {
                    for(int k = 0; k < a.dims[2]; k++)
                    {
                        ad.arrayd[i,j,k] = a.arrayd[i,j,k] * b.arrayd[i,j,k];
                    }
                }
            }

            return ad;
        }
        public static Arrayd operator *(Arrayd a, double b)
        {
            if(b.GetType() != typeof(double))
            {
                throw new ArgumentException("Wrong number, the arrayd must multiply double type number.", nameof(b));
            }

            Arrayd ad = new Arrayd();
            for(int i = 0; i < a.dims[0]; i++)
            {
                for(int j = 0; j < a.dims[1]; j++)
                {
                    for(int k = 0; k < a.dims[2]; k++)
                    {
                        ad.arrayd[i,j,k] = a.arrayd[i,j,k] * b;
                    }
                }
            }

            return ad;
        }
        // TODO: mul
        public static Arrayd Mul(Arrayd a, Arrayd b)
        {
            Arrayd ad = new Arrayd();

            return ad;
        }

        // divide method
        public static Arrayd operator /(Arrayd a, Arrayd b)
        {
            for(int m = 0; m < a.dims.Length; m++)
            {
                if(a.dims[m] != b.dims[m])
                {
                    throw new ArgumentException("Can not divide two arrayd with different element in some dimension.", nameof(a));
                }
            }

            Arrayd ad = new Arrayd();
            for(int i = 0; i < a.dims[0]; i++)
            {
                for(int j = 0; j < a.dims[1]; j++)
                {
                    for(int k = 0; k < a.dims[2]; k++)
                    {
                        ad.arrayd[i,j,k] = a.arrayd[i,j,k] / b.arrayd[i,j,k];
                    }
                }
            }

            return ad;
        }
        public static Arrayd operator /(Arrayd a, double b)
        {
            if(b.GetType() != typeof(double))
            {
                throw new ArgumentException("Wrong number, the arrayd must divide double type number.", nameof(b));
            }

            Arrayd ad = new Arrayd();
            for(int i = 0; i < a.dims[0]; i++)
            {
                for(int j = 0; j < a.dims[1]; j++)
                {
                    for(int k = 0; k < a.dims[2]; k++)
                    {
                        ad.arrayd[i,j,k] = a.arrayd[i,j,k] / b;
                    }
                }
            }

            return ad;
        }
        // TODO: div
        public static Arrayd Div(Arrayd a, Arrayd b)
        {
            Arrayd ad = new Arrayd();

            return ad;
        }

        // TODO: transpose
        // get the transpose method
        public static Arrayd Transpose(Arrayd a, int[] b)
        {
            for(int m = 0; m < a.dims.Length; m++)
            {
                if(a.dims[m] == 0)
                {
                    throw new ArgumentException("Wrong arrayd, can not transpose arrayd with zero element in some dimension.", nameof(a));
                }
            }

            Arrayd ad = new Arrayd();
            
            return ad;
        }

        // TODO: inverse
        // get the inverse method
        public static Arrayd Inverse(Arrayd a)
        {
            Arrayd ad = new Arrayd();

            return ad;
        }

        // equal method
        public static bool operator ==(Arrayd a, Arrayd b)
        {
            bool status = false;
            for(int m = 0; m < a.dims.Length; m++)
            {
                if(a.dims[m] == b.dims[m])
                {
                    throw new ArgumentException("Can not compare two arrayd with different element in some dimension.", nameof(a));
                    return status;
                }
            }

            for(int i = 0; i < a.dims[0]; i++)
            {
                for(int j = 0; j < a.dims[1]; j++)
                {
                    for(int k = 0; k < a.dims[2]; k++)
                    {
                        if(a.arrayd[i,j,k] == b.arrayd[i,j,k])
                        {
                            status = true;
                        }
                        else
                        {
                            status = false;
                            goto label;
                        }
                    }
                }
            }
            label:;

            return status;
        }

        // not equal method
        public static bool operator !=(Arrayd a, Arrayd b)
        {
            bool status = true;
            for(int m = 0; m < a.dims.Length; m++)
            {
                if(a.dims[m] == b.dims[m])
                {
                    throw new ArgumentException("Can not compare two arrayd with different element in some dimension.", nameof(a));
                    return status;
                }
            }

            for(int i = 0; i < a.dims[0]; i++)
            {
                for(int j = 0; j < a.dims[1]; j++)
                {
                    for(int k = 0; k < a.dims[2]; k++)
                    {
                        if(a.arrayd[i,j,k] == b.arrayd[i,j,k])
                        {
                            status = false;
                        }
                        else
                        {
                            status = true;
                            goto label;
                        }
                    }
                }
            }
            label:;

            return status;
        }
    }
}
