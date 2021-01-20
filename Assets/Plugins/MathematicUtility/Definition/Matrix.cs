using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO; // for clone method
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;

namespace MathematicUtility.Definition
{
    // --- float matrix type --- //
    // [System.Serializable]
    public struct Matrixf
    {
        // define the matrix struct(two dimension)
        public float[,] matrixf;
        public int rows;
        public int columns;
        public float this[int idxi, int idxj]
        {
            get { return matrixf[idxi,idxj]; }
            set { matrixf[idxi,idxj] = value; }
        }

        // struct constructor
        public Matrixf(int row, int col)
        {
            if(row == 0 || col == 0)
            {
                throw new ArgumentException("Can not create one or zero dimension matrixf.", nameof(row));
            }
            this.matrixf = new float[row,col];
            this.rows = this.matrixf.GetLength(0);
            this.columns = this.matrixf.GetLength(1);
        }
        public Matrixf(float[,] values)
        {
            this.matrixf = values;
            this.rows = this.matrixf.GetLength(0);
            this.columns = this.matrixf.GetLength(1);
            if(this.rows == 0 || this.columns == 0)
            {
                throw new ArgumentException("Can not create one or zero dimension matrixf.", nameof(this.rows));
            }
        }

        // define the struct method
        // set zeros method
        public static Matrixf Zeros(Matrixf a)
        {
            if(a.rows == 0 || a.columns == 0)
            {
                throw new ArgumentException("Can not set one or zero dimension matrixf to zeros.", nameof(a));
            }

            // Matrixf mf = new Matrixf();
            Matrixf mf = new Matrixf(a.rows, a.columns);
            for(int i = 0; i < a.rows; i++)
            {
                for(int j = 0; j < a.columns; j++)
                {
                    mf.matrixf[i,j] = 0.0f;
                }
            }

            return mf;
        }
        public static Matrixf Zeros(int row, int column)
        {
            if(row == 0 || column == 0)
            {
                throw new ArgumentException("Can not set one or zero dimension matrixf to zeros.", nameof(row));
            }

            // Matrixf mf = new Matrixf();
            Matrixf mf = new Matrixf(row, column);
            for(int i = 0; i < row; i++)
            {
                for(int j = 0; j < column; j++)
                {
                    mf.matrixf[i,j] = 0.0f;
                }
            }
            
            return mf;
        }

        // set ones method
        public static Matrixf Ones(Matrixf a)
        {
            if(a.rows == 0 || a.columns == 0)
            {
                throw new ArgumentException("Can not set one or zero dimension matrixf to ones.", nameof(a));
            }

            // Matrixf mf = new Matrixf();
            Matrixf mf = new Matrixf(a.rows, a.columns);
            for(int i = 0; i < a.rows; i++)
            {
                for(int j = 0; j < a.columns; j++)
                {
                    mf.matrixf[i,j] = 1.0f;
                }
            }

            return mf;
        }
        public static Matrixf Ones(int row, int column)
        {
            if(row == 0 || column == 0)
            {
                throw new ArgumentException("Can not set one or zero dimension matrixf to ones.", nameof(row));
            }

            // Matrixf mf = new Matrixf();
            Matrixf mf = new Matrixf(row, column);
            for(int i = 0; i < row; i++)
            {
                for(int j = 0; j < column; j++)
                {
                    mf.matrixf[i,j] = 1.0f;
                }
            }
            
            return mf;
        }

        // set identity method
        public static Matrixf Identity(Matrixf a)
        {
            if(a.rows == 0 || a.columns == 0)
            {
                throw new ArgumentException("Can not set one or zero dimension matrixf to identity.", nameof(a));
            }
            
            // Matrixf mf = new Matrixf();
            Matrixf mf = new Matrixf(a.rows, a.columns);
            for(int i = 0; i < a.rows; i++)
            {
                for(int j = 0; j < a.columns; j++)
                {
                    if(i == j)
                    {
                        mf.matrixf[i,j] = 1.0f;
                    }
                    else
                    {
                        mf.matrixf[i,j] = 0.0f;
                    }
                }
            }

            return mf;
        }
        public static Matrixf Identity(int row, int column)
        {
            if(row == 0 || column == 0)
            {
                throw new ArgumentException("Can not set one or zero dimension matrixf to identity.", nameof(row));
            }

            // Matrixf mf = new Matrixf();
            Matrixf mf = new Matrixf(row, column);
            for(int i = 0; i < row; i++)
            {
                for(int j = 0; j < column; j++)
                {
                    if(i == j)
                    {
                        mf.matrixf[i,j] = 1.0f;
                    }
                    else
                    {
                        mf.matrixf[i,j] = 0.0f;
                    }
                }
            }

            return mf;
        }

        // add method
        public static Matrixf operator +(Matrixf a, Matrixf b)
        {
            if(a.rows != b.rows || a.columns != b.columns)
            {
                throw new ArgumentException("Can not add two matrixf with different rows or columns.", nameof(a));
            }

            // Matrixf mf = new Matrixf();
            Matrixf mf = new Matrixf(a.rows, a.columns);
            for(int i = 0; i < a.rows; i++)
            {
                for(int j = 0; j < a.columns; j++)
                {
                    mf.matrixf[i,j] = a.matrixf[i,j] + b.matrixf[i,j];
                }
            }

            return mf;
        }
        public static Matrixf operator +(Matrixf a, float b)
        {
            if(b.GetType() != typeof(float))
            {
                throw new ArgumentException("Wrong number, the matrixf must add float type number.", nameof(b));
            }

            // Matrixf mf = new Matrixf();
            Matrixf mf = new Matrixf(a.rows, a.columns);
            for(int i = 0; i < a.rows; i++)
            {
                for(int j = 0; j < a.columns; j++)
                {
                    mf.matrixf[i,j] = a.matrixf[i,j] + b;
                }
            }

            return mf;
        }

        // subtract method
        public static Matrixf operator -(Matrixf a, Matrixf b)
        {
            if(a.rows != b.rows || a.columns != b.columns)
            {
                throw new ArgumentException("Can not subtract two matrixf with different rows or columns.", nameof(a));
            }

            // Matrixf mf = new Matrixf();
            Matrixf mf = new Matrixf(a.rows, a.columns);
            for(int i = 0; i < a.rows; i++)
            {
                for(int j = 0; j < a.columns; j++)
                {
                    mf.matrixf[i,j] = a.matrixf[i,j] - b.matrixf[i,j];
                }
            }

            return mf;
        }
        public static Matrixf operator -(Matrixf a, float b)
        {
            if(b.GetType() != typeof(float))
            {
                throw new ArgumentException("Wrong number, the matrixf must subtract float type number.", nameof(b));
            }

            // Matrixf mf = new Matrixf();
            Matrixf mf = new Matrixf(a.rows, a.columns);
            for(int i = 0; i < a.rows; i++)
            {
                for(int j = 0; j < a.columns; j++)
                {
                    mf.matrixf[i,j] = a.matrixf[i,j] - b;
                }
            }

            return mf;
        }

        // multiply method
        public static Matrixf operator *(Matrixf a, Matrixf b)
        {
            if(a.rows != b.rows || a.columns != b.columns)
            {
                throw new ArgumentException("Can not multiply two matrixf with different rows or columns.", nameof(a));
            }

            // Matrixf mf = new Matrixf();
            Matrixf mf = new Matrixf(a.rows, a.columns);
            for(int i = 0; i < a.rows; i++)
            {
                for(int j = 0; j < a.columns; j++)
                {
                    mf.matrixf[i,j] = a.matrixf[i,j] * b.matrixf[i,j];
                }
            }

            return mf;
        }
        public static Matrixf operator *(Matrixf a, float b)
        {
            if(b.GetType() != typeof(float))
            {
                throw new ArgumentException("Wrong number, the matrixf must multiply float type number.", nameof(b));
            }

            // Matrixf mf = new Matrixf();
            Matrixf mf = new Matrixf(a.rows, a.columns);
            for(int i = 0; i < a.rows; i++)
            {
                for(int j = 0; j < a.columns; j++)
                {
                    mf.matrixf[i,j] = a.matrixf[i,j] * b;
                }
            }

            return mf;
        }
        public static Matrixf Mul(Matrixf a, Matrixf b)
        {
            if(a.columns != b.rows)
            {
                throw new ArgumentException("Wrong match, can not multiply two matrixf with different columns or rows.", nameof(a));
            }
            
            // Matrixf mf = new Matrixf();
            Matrixf mf = new Matrixf(a.rows, b.columns);
            float sum = 0.0f;
            for(int i = 0; i < a.rows; i++)
            {
                for(int j = 0; j < b.columns; j++)
                {
                    for(int k = 0; k < a.columns; k++)
                    {
                        sum = sum + a.matrixf[i,k] * b.matrixf[k,j]; // a Mul b = matrix product(a,b)
                    }  
                    mf.matrixf[i,j] = sum;
                    sum = 0.0f;
                }
            }

            return mf;
        }

        // divide method
        public static Matrixf operator /(Matrixf a, Matrixf b)
        {
            if(a.rows != b.rows || a.columns != b.columns)
            {
                throw new ArgumentException("Can not divide two matrixf with different rows or columns.", nameof(a));
            }

            // Matrixf mf = new Matrixf();
            Matrixf mf = new Matrixf(a.rows, a.columns);
            for(int i = 0; i < a.rows; i++)
            {
                for(int j = 0; j < a.columns; j++)
                {
                    mf.matrixf[i,j] = a.matrixf[i,j] / b.matrixf[i,j];
                }
            }

            return mf;
        }
        public static Matrixf operator /(Matrixf a, float b)
        {
            if(b.GetType() != typeof(float))
            {
                throw new ArgumentException("Wrong number, the matrixf must divide float type number.", nameof(b));
            }

            // Matrixf mf = new Matrixf();
            Matrixf mf = new Matrixf(a.rows, a.columns);
            for(int i = 0; i < a.rows; i++)
            {
                for(int j = 0; j < a.columns; j++)
                {
                    mf.matrixf[i,j] = a.matrixf[i,j] / b;
                }
            }

            return mf;
        }
        public static Matrixf Div(Matrixf a, Matrixf b)
        {
            if(b.rows != b.columns)
            {
                throw new ArgumentException("Can not divide the matrixf b because it's non-square matrixf.", nameof(b));
            }
            if(a.columns != b.rows)
            {
                throw new ArgumentException("Wrong match, can not divide two matrixf with different columns or rows.", nameof(a));
            }
            
            // Matrixf mf = new Matrixf();
            Matrixf mf = new Matrixf(a.rows, a.columns);
            Matrixf mInverse = Inverse(b);
            float sum = 0.0f;
            for(int i = 0; i < a.rows; i++)
            {
                for(int j = 0; j < mInverse.columns; j++)
                {
                    for(int k = 0; k < a.columns; k++)
                    {
                        sum = sum + a.matrixf[i,k] * mInverse.matrixf[k,j]; // a Div b = a Mul Inverse(b)
                    }  
                    mf.matrixf[i,j] = sum;
                    sum = 0.0f;
                }
            }

            return mf;
        }

        // get the transpose method
        public static Matrixf Transpose(Matrixf a)
        {
            if(a.rows == 0 || a.columns == 0)
            {
                throw new ArgumentException("Wrong matrixf, can not transpose one or zero dimension matrixf.", nameof(a));
            }

            // Matrixf mf = new Matrixf();
            Matrixf mf = new Matrixf(a.columns, a.rows); // define the transpose matrixf
            for(int i = 0; i < a.columns; i++)
            {
                for(int j = 0; j < a.rows; j++)
                {
                    mf.matrixf[i,j] = a.matrixf[j,i];
                }
            }

            return mf;
        }

        // get the inverse method
        public static Matrixf Inverse(Matrixf a)
        {
            if(a.rows != a.columns)
            {
                throw new ArgumentException("Can not get the inverse of the non-square matrixf.", nameof(a));
            }

            // construct the transform matrix
            int row = a.rows;
            int col = a.columns;
            Matrixf mTrans = new Matrixf(row, 2*col);
            Matrixf mTemp = new Matrixf(row, 2*col);
            Matrixf mf = new Matrixf(row, col);

            // initialize the transform matrix
            for(int i = 0; i < row; i++)
            {
                for(int j = 0; j < col; j++)
                {
                    mTrans.matrixf[i,j] = a.matrixf[i,j];
                    if(i == j)
                    {
                        mTrans.matrixf[i,j+col] = 1.0f;
                    }
                    else
                    {
                        mTrans.matrixf[i,j+row] = 0.0f;
                    }
                    
                }
            }

            // process the elementary row transformation
            int rankFlag = 0;
            float scaleTemp = 0.0f;
            for(int q = 0; q < col; q++)
            {
                // set the elements to ones in row order for every row of the matrixf
                for(int p = q; p < row; p++)
                {                  
                    if(mTrans.matrixf[p,q] != 0.0f)
                    {
                        if(mTrans.matrixf[p,q] != 1.0f)
                        {
                            scaleTemp = mTrans.matrixf[p,q];
                            for(int r = q; r < 2*col; r++)
                            {
                                mTrans.matrixf[p,r] = mTrans.matrixf[p,r] / scaleTemp;
                            }
                        }
                        // reset the scaleTemp
                        scaleTemp = 0.0f;
                    }
                    else
                    {
                        rankFlag++;
                        continue;
                    }
                }

                // check the rank of the matrixf
                if(rankFlag == (row-q))
                {
                    throw new ArgumentException("The matrixf can not get inverse because of its non-full rank.", nameof(rankFlag));
                }

                // check whether the main element equals zero and change it to one by exchanging the rows
                if(mTrans.matrixf[q,q] == 0.0f)
                {
                    for(int s = q+1; s < row; s++)
                    {
                        // check relative column elements for exchange
                        if(mTrans.matrixf[s,q] != 0.0f)
                        {
                            // mTemp = mTrans;
                            mTemp.matrixf = (float[,])Clone(mTrans.matrixf);
                            for(int t = 0; t < 2*col; t++)
                            {
                                mTrans.matrixf[q,t] = mTemp.matrixf[s,t];
                                mTrans.matrixf[s,t] = mTemp.matrixf[q,t];
                            }
                            break;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    // clear the mTemp matrixf buffer
                    mTemp = Zeros(mTemp);
                }
                
                // set the non-main element to zeros in relative column of the matrixf
                for(int u = q+1; u < row; u++)
                {
                    if(mTrans.matrixf[u,q] != 0.0f)
                    {
                        for(int v = 0; v < 2*col; v++)
                        {
                            mTrans.matrixf[u,v] = mTrans.matrixf[u,v] - mTrans.matrixf[q,v];
                        }
                        
                    }
                    else
                    {
                        continue;
                    }
                }

                // reset the rankFlag
                rankFlag = 0;
            }

            // set the non-main element to zeros in right-up area of the matrixf in column order
            for(int x = col-1; x > 0; x--) // check the columns from the last column to second column
            {
                // check the rows from second-to-last row to first row
                for(int y = x-1; y >= 0; y--)
                {
                    scaleTemp = mTrans.matrixf[y,x] / mTrans.matrixf[x,x];
                    for(int z = 0; z < 2*col; z++) // set the every element of the picked row
                    {
                        mTrans.matrixf[y,z] = mTrans.matrixf[y,z] - scaleTemp * mTrans.matrixf[x,z];
                    }
                    // reset the scaleTemp
                    scaleTemp = 0.0f;
                }
            }

            // get the inverse of the original matrixf a
            for(int m = 0; m < row; m++)
            {
                for(int n = 0; n < col; n++)
                {
                    mf.matrixf[m,n] = mTrans.matrixf[m,n+col];
                }
            }

            return mf;
        }

        // equal method
        public static bool operator ==(Matrixf a, Matrixf b)
        {
            bool status = false;
            if(a.rows != b.rows || a.columns != b.columns)
            {
                throw new ArgumentException("Can not compare two matrixf with different rows or columns.", nameof(a));
                return status;
            }
            for(int i = 0; i < a.rows; i++)
            {
                for(int j = 0; j < b.columns; j++)
                {
                    if(a.matrixf[i,j] == b.matrixf[i,j])
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
            label:;

            return status;
        }

        // not equal method
        public static bool operator !=(Matrixf a, Matrixf b)
        {
            bool status = true;
            if(a.rows != b.rows || a.columns != b.columns)
            {
                throw new ArgumentException("Can not compare two matrixf with different rows or columns.", nameof(a));
                return status;
            }
            for(int i = 0; i < a.rows; i++)
            {
                for(int j = 0; j < b.columns; j++)
                {
                    if(a.matrixf[i,j] == b.matrixf[i,j])
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
            label:;

            return status;
        }

        // clone method
        public static object Clone(object obj)
        {
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            
            bf.Serialize(ms, obj);
            ms.Position = 0;

            return (bf.Deserialize(ms));
        }
    }

    // --- double matrix type --- //
    // [System.Serializable]
    public struct Matrixd
    {
        // define the matrix struct(two dimension)
        public double[,] matrixd;
        public int rows;
        public int columns;
        public double this[int idxi, int idxj]
        {
            get { return matrixd[idxi,idxj]; }
            set { matrixd[idxi,idxj] = value; }
        }

        // struct constructor
        public Matrixd(int row, int col)
        {
            if(row == 0 || col == 0)
            {
                throw new ArgumentException("Can not create one or zero dimension matrixd.", nameof(row));
            }
            this.matrixd = new double[row,col];
            this.rows = this.matrixd.GetLength(0);
            this.columns = this.matrixd.GetLength(1);
        }
        public Matrixd(double[,] values)
        {
            this.matrixd = values;
            this.rows = this.matrixd.GetLength(0);
            this.columns = this.matrixd.GetLength(1);
            if(this.rows == 0 || this.columns == 0)
            {
                throw new ArgumentException("Can not create one or zero dimension matrixd.", nameof(this.rows));
            }
        }

        // define the struct method
        // set zeros method
        public static Matrixd Zeros(Matrixd a)
        {
            if(a.rows == 0 || a.columns == 0)
            {
                throw new ArgumentException("Can not set one or zero dimension matrixd to zeros.", nameof(a));
            }

            // Matrixd md = new Matrixd();
            Matrixd md = new Matrixd(a.rows, a.columns);
            for(int i = 0; i < a.rows; i++)
            {
                for(int j = 0; j < a.columns; j++)
                {
                    md.matrixd[i,j] = 0.0;
                }
            }

            return md;
        }
        public static Matrixd Zeros(int row, int column)
        {
            if(row == 0 || column == 0)
            {
                throw new ArgumentException("Can not set one or zero dimension matrixd to zeros.", nameof(row));
            }

            // Matrixd md = new Matrixd();
            Matrixd md = new Matrixd(row, column);
            for(int i = 0; i < row; i++)
            {
                for(int j = 0; j < column; j++)
                {
                    md.matrixd[i,j] = 0.0;
                }
            }
            
            return md;
        }

        // set ones method
        public static Matrixd Ones(Matrixd a)
        {
            if(a.rows == 0 || a.columns == 0)
            {
                throw new ArgumentException("Can not set one or zero dimension matrixd to ones.", nameof(a));
            }

            // Matrixd md = new Matrixd();
            Matrixd md = new Matrixd(a.rows, a.columns);
            for(int i = 0; i < a.rows; i++)
            {
                for(int j = 0; j < a.columns; j++)
                {
                    md.matrixd[i,j] = 1.0;
                }
            }

            return md;
        }
        public static Matrixd Ones(int row, int column)
        {
            if(row == 0 || column == 0)
            {
                throw new ArgumentException("Can not set one or zero dimension matrixd to ones.", nameof(row));
            }

            // Matrixd md = new Matrixd();
            Matrixd md = new Matrixd(row, column);
            for(int i = 0; i < row; i++)
            {
                for(int j = 0; j < column; j++)
                {
                    md.matrixd[i,j] = 1.0;
                }
            }
            
            return md;
        }

        // set identity method
        public static Matrixd Identity(Matrixd a)
        {
            if(a.rows == 0 || a.columns == 0)
            {
                throw new ArgumentException("Can not set one or zero dimension matrixd to identity.", nameof(a));
            }
            
            // Matrixd md = new Matrixd();
            Matrixd md = new Matrixd(a.rows, a.columns);
            for(int i = 0; i < a.rows; i++)
            {
                for(int j = 0; j < a.columns; j++)
                {
                    if(i == j)
                    {
                        md.matrixd[i,j] = 1.0;
                    }
                    else
                    {
                        md.matrixd[i,j] = 0.0;
                    }
                }
            }

            return md;
        }
        public static Matrixd Identity(int row, int column)
        {
            if(row == 0 || column == 0)
            {
                throw new ArgumentException("Can not set one or zero dimension matrixd to identity.", nameof(row));
            }

            // Matrixd md = new Matrixd();
            Matrixd md = new Matrixd(row, column);
            for(int i = 0; i < row; i++)
            {
                for(int j = 0; j < column; j++)
                {
                    if(i == j)
                    {
                        md.matrixd[i,j] = 1.0;
                    }
                    else
                    {
                        md.matrixd[i,j] = 0.0;
                    }
                }
            }

            return md;
        }

        // add method
        public static Matrixd operator +(Matrixd a, Matrixd b)
        {
            if(a.rows != b.rows || a.columns != b.columns)
            {
                throw new ArgumentException("Can not add two matrixd with different rows or columns.", nameof(a));
            }

            // Matrixd md = new Matrixd();
            Matrixd md = new Matrixd(a.rows, a.columns);
            for(int i = 0; i < a.rows; i++)
            {
                for(int j = 0; j < a.columns; j++)
                {
                    md.matrixd[i,j] = a.matrixd[i,j] + b.matrixd[i,j];
                }
            }

            return md;
        }
        public static Matrixd operator +(Matrixd a, double b)
        {
            if(b.GetType() != typeof(double))
            {
                throw new ArgumentException("Wrong number, the matrixd must add double type number.", nameof(b));
            }

            // Matrixd md = new Matrixd();
            Matrixd md = new Matrixd(a.rows, a.columns);
            for(int i = 0; i < a.rows; i++)
            {
                for(int j = 0; j < a.columns; j++)
                {
                    md.matrixd[i,j] = a.matrixd[i,j] + b;
                }
            }

            return md;
        }

        // subtract method
        public static Matrixd operator -(Matrixd a, Matrixd b)
        {
            if(a.rows != b.rows || a.columns != b.columns)
            {
                throw new ArgumentException("Can not subtract two matrixd with different rows or columns.", nameof(a));
            }

            // Matrixd md = new Matrixd();
            Matrixd md = new Matrixd(a.rows, a.columns);
            for(int i = 0; i < a.rows; i++)
            {
                for(int j = 0; j < a.columns; j++)
                {
                    md.matrixd[i,j] = a.matrixd[i,j] - b.matrixd[i,j];
                }
            }

            return md;
        }
        public static Matrixd operator -(Matrixd a, double b)
        {
            if(b.GetType() != typeof(double))
            {
                throw new ArgumentException("Wrong number, the matrixd must subtract double type number.", nameof(b));
            }

            // Matrixd md = new Matrixd();
            Matrixd md = new Matrixd(a.rows, a.columns);
            for(int i = 0; i < a.rows; i++)
            {
                for(int j = 0; j < a.columns; j++)
                {
                    md.matrixd[i,j] = a.matrixd[i,j] - b;
                }
            }

            return md;
        }

        // multiply method
        public static Matrixd operator *(Matrixd a, Matrixd b)
        {
            if(a.rows != b.rows || a.columns != b.columns)
            {
                throw new ArgumentException("Can not multiply two matrixd with different rows or columns.", nameof(a));
            }

            // Matrixd md = new Matrixd();
            Matrixd md = new Matrixd(a.rows, a.columns);
            for(int i = 0; i < a.rows; i++)
            {
                for(int j = 0; j < a.columns; j++)
                {
                    md.matrixd[i,j] = a.matrixd[i,j] * b.matrixd[i,j];
                }
            }

            return md;
        }
        public static Matrixd operator *(Matrixd a, double b)
        {
            if(b.GetType() != typeof(double))
            {
                throw new ArgumentException("Wrong number, the matrixd must multiply double type number.", nameof(b));
            }

            // Matrixd md = new Matrixd();
            Matrixd md = new Matrixd(a.rows, a.columns);
            for(int i = 0; i < a.rows; i++)
            {
                for(int j = 0; j < a.columns; j++)
                {
                    md.matrixd[i,j] = a.matrixd[i,j] * b;
                }
            }

            return md;
        }
        public static Matrixd Mul(Matrixd a, Matrixd b)
        {
            if(a.columns != b.rows)
            {
                throw new ArgumentException("Wrong match, can not multiply two matrixd with different columns or rows.", nameof(a));
            }
            
            // Matrixd md = new Matrixd();
            Matrixd md = new Matrixd(a.rows, b.columns);
            double sum = 0.0;
            for(int i = 0; i < a.rows; i++)
            {
                for(int j = 0; j < b.columns; j++)
                {
                    for(int k = 0; k < a.columns; k++)
                    {
                        sum = sum + a.matrixd[i,k] * b.matrixd[k,j]; // a Mul b = matrix product(a,b)
                    }  
                    md.matrixd[i,j] = sum;
                    sum = 0.0;
                }
            }

            return md;
        }

        // divide method
        public static Matrixd operator /(Matrixd a, Matrixd b)
        {
            if(a.rows != b.rows || a.columns != b.columns)
            {
                throw new ArgumentException("Can not divide two matrixd with different rows or columns.", nameof(a));
            }

            // Matrixd md = new Matrixd();
            Matrixd md = new Matrixd(a.rows, a.columns);
            for(int i = 0; i < a.rows; i++)
            {
                for(int j = 0; j < a.columns; j++)
                {
                    md.matrixd[i,j] = a.matrixd[i,j] / b.matrixd[i,j];
                }
            }

            return md;
        }
        public static Matrixd operator /(Matrixd a, double b)
        {
            if(b.GetType() != typeof(double))
            {
                throw new ArgumentException("Wrong number, the matrixd must divide double type number.", nameof(b));
            }

            // Matrixd md = new Matrixd();
            Matrixd md = new Matrixd(a.rows, a.columns);
            for(int i = 0; i < a.rows; i++)
            {
                for(int j = 0; j < a.columns; j++)
                {
                    md.matrixd[i,j] = a.matrixd[i,j] / b;
                }
            }

            return md;
        }
        public static Matrixd Div(Matrixd a, Matrixd b)
        {
            if(b.rows != b.columns)
            {
                throw new ArgumentException("Can not divide the matrixd b because it's non-square matrixd.", nameof(b));
            }
            if(a.columns != b.rows)
            {
                throw new ArgumentException("Wrong match, can not divide two matrixd with different columns or rows.", nameof(a));
            }
            
            // Matrixd md = new Matrixd();
            Matrixd md = new Matrixd(a.rows, a.columns);
            Matrixd mInverse = Inverse(b);
            double sum = 0.0;
            for(int i = 0; i < a.rows; i++)
            {
                for(int j = 0; j < mInverse.columns; j++)
                {
                    for(int k = 0; k < a.columns; k++)
                    {
                        sum = sum + a.matrixd[i,k] * mInverse.matrixd[k,j]; // a Div b = a Mul Inverse(b)
                    }  
                    md.matrixd[i,j] = sum;
                    sum = 0.0;
                }
            }

            return md;
        }

        // get the transpose method
        public static Matrixd Transpose(Matrixd a)
        {
            if(a.rows == 0 || a.columns == 0)
            {
                throw new ArgumentException("Wrong matrixd, can not transpose one or zero dimension matrixd.", nameof(a));
            }

            // Matrixd md = new Matrixd();
            Matrixd md = new Matrixd(a.columns, a.rows); // define the transpose matrixd
            for(int i = 0; i < a.columns; i++)
            {
                for(int j = 0; j < a.rows; j++)
                {
                    md.matrixd[i,j] = a.matrixd[j,i];
                }
            }

            return md;
        }

        // get the inverse method
        public static Matrixd Inverse(Matrixd a)
        {
            if(a.rows != a.columns)
            {
                throw new ArgumentException("Can not get the inverse of the non-square matrixd.", nameof(a));
            }

            // construct the transform matrix
            int row = a.rows;
            int col = a.columns;
            Matrixd mTrans = new Matrixd(row, 2*col);
            Matrixd mTemp = new Matrixd(row, 2*col);
            Matrixd md = new Matrixd(row, col);

            // initialize the transform matrix
            for(int i = 0; i < row; i++)
            {
                for(int j = 0; j < col; j++)
                {
                    mTrans.matrixd[i,j] = a.matrixd[i,j];
                    if(i == j)
                    {
                        mTrans.matrixd[i,j+col] = 1.0;
                    }
                    else
                    {
                        mTrans.matrixd[i,j+row] = 0.0;
                    }
                    
                }
            }

            // process the elementary row transformation
            int rankFlag = 0;
            double scaleTemp = 0.0;
            for(int q = 0; q < col; q++)
            {
                // set the elements to ones in row order for every row of the matrixd
                for(int p = q; p < row; p++)
                {                  
                    if(mTrans.matrixd[p,q] != 0.0)
                    {
                        if(mTrans.matrixd[p,q] != 1.0)
                        {
                            scaleTemp = mTrans.matrixd[p,q];
                            for(int r = q; r < 2*col; r++)
                            {
                                mTrans.matrixd[p,r] = mTrans.matrixd[p,r] / scaleTemp;
                            }
                        }
                        // reset the scaleTemp
                        scaleTemp = 0.0;
                    }
                    else
                    {
                        rankFlag++;
                        continue;
                    }
                }

                // check the rank of the matrixd
                if(rankFlag == (row-q))
                {
                    throw new ArgumentException("The matrixd can not be transposed because of its non-full rank.", nameof(rankFlag));
                }

                // check whether the main element equals zero and change it to one by exchanging the rows
                if(mTrans.matrixd[q,q] == 0.0)
                {
                    for(int s = q+1; s < row; s++)
                    {
                        // check relative column elements for exchange
                        if(mTrans.matrixd[s,q] != 0.0)
                        {
                            // mTemp = mTrans;
                            mTemp.matrixd = (double[,])Clone(mTrans.matrixd);
                            for(int t = 0; t < 2*col; t++)
                            {
                                mTrans.matrixd[q,t] = mTemp.matrixd[s,t];
                                mTrans.matrixd[s,t] = mTemp.matrixd[q,t];
                            }
                            break;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    // clear the mTemp matrixd buffer
                    mTemp = Zeros(mTemp);
                }
                
                // set the non-main element to zeros in relative column of the matrixd
                for(int u = q+1; u < row; u++)
                {
                    if(mTrans.matrixd[u,q] != 0.0)
                    {
                        for(int v = 0; v < 2*col; v++)
                        {
                            mTrans.matrixd[u,v] = mTrans.matrixd[u,v] - mTrans.matrixd[q,v];
                        }
                        
                    }
                    else
                    {
                        continue;
                    }
                }

                // reset the rankFlag
                rankFlag = 0;
            }

            // set the non-main element to zeros in right-up area of the matrixd in column order
            for(int x = col-1; x > 0; x--) // check the columns from the last column to second column
            {
                // check the rows from second-to-last row to first row
                for(int y = x-1; y >= 0; y--)
                {
                    scaleTemp = mTrans.matrixd[y,x] / mTrans.matrixd[x,x];
                    for(int z = 0; z < 2*col; z++) // set the every element of the picked row
                    {
                        mTrans.matrixd[y,z] = mTrans.matrixd[y,z] - scaleTemp * mTrans.matrixd[x,z];
                    }
                    // reset the scaleTemp
                    scaleTemp = 0.0;
                }
            }

            // get the inverse of the original matrixd a
            for(int m = 0; m < row; m++)
            {
                for(int n = 0; n < col; n++)
                {
                    md.matrixd[m,n] = mTrans.matrixd[m,n+col];
                }
            }

            return md;
        }

        // equal method
        public static bool operator ==(Matrixd a, Matrixd b)
        {
            bool status = false;
            if(a.rows != b.rows || a.columns != b.columns)
            {
                throw new ArgumentException("Can not compare two matrixd with different rows or columns.", nameof(a));
                return status;
            }
            for(int i = 0; i < a.rows; i++)
            {
                for(int j = 0; j < b.columns; j++)
                {
                    if(a.matrixd[i,j] == b.matrixd[i,j])
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
            label:;

            return status;
        }

        // not equal method
        public static bool operator !=(Matrixd a, Matrixd b)
        {
            bool status = true;
            if(a.rows != b.rows || a.columns != b.columns)
            {
                throw new ArgumentException("Can not compare two matrixd with different rows or columns.", nameof(a));
                return status;
            }
            for(int i = 0; i < a.rows; i++)
            {
                for(int j = 0; j < b.columns; j++)
                {
                    if(a.matrixd[i,j] == b.matrixd[i,j])
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
            label:;

            return status;
        }

        // clone method
        public static object Clone(object obj)
        {
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            
            bf.Serialize(ms, obj);
            ms.Position = 0;

            return (bf.Deserialize(ms));
        }
    }
}
