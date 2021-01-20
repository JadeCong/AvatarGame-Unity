using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace AvatarRetarget.Filter
{
    public class LowpassFilter
    {
        // define the lowpass filter parameters
        float Ypi; // previous 
        float Yppi;
        float Ypppi;
        float Xpi;
        float Xppi;
        Vector3 Ypj;
        Vector3 Yppj;
        Vector3 Ypppj;
        Vector3 Xpj;
        Vector3 Xppj;
        float[] Ypk;
        float[] Yppk;
        float[] Ypppk;
        float[] Xpk;
        float[] Xppk;

        // initialize the LowpassFilter class
        public LowpassFilter()
        {

        }

        // lowpass filter for first order
        public float FirstOrderLowpassFilter(float X, float alpha) // for single data
        {
            float Y;

            Y = alpha * X + (1 - alpha) * Ypi;

            Ypi = Y;

            return Y;
        }
        public Vector3 FirstOrderLowpassFilter(Vector3 X, float alpha) // for Vector3 data with single parameter
        {
            Vector3 Y;

            Y = alpha * X + (1 - alpha) * Ypj;

            Ypj = Y;

            return Y;
        }
        public Vector3 FirstOrderLowpassFilter(Vector3 X, Vector3 alpha) // for Vector3 data with multiple parameter
        {
            Vector3 Y;

            Y.x = alpha.x * X.x + (1 - alpha.x) * Ypj.x;
            Y.y = alpha.y * X.y + (1 - alpha.y) * Ypj.y;
            Y.z = alpha.z * X.z + (1 - alpha.z) * Ypj.z;

            Ypj = Y;

            return Y;
        }
        public float[] FirstOrderLowpassFilter(float[] X, float alpha) // for multiple dimension array data with single parameter
        {
            float[] Y = new float[X.Length];
            
            for(int i = 0; i < X.Length; i++)
            {
                Y[i] = alpha * X[i] + (1 - alpha) * Ypk[i];
            }
            
            Ypk = Y;

            return Y;
        }
        public float[] FirstOrderLowpassFilter(float[] X, float[] alpha) // for multiple dimension array data with multiple parameter
        {
            float[] Y = new float[X.Length];
            
            for(int i = 0; i < X.Length; i++)
            {
                Y[i] = alpha[i] * X[i] + (1 - alpha[i]) * Ypk[i];
            }
            
            Ypk = Y;

            return Y;
        }

        // lowpass filter for second order
        public float SecondOrderLowpassFilter(float X, float alpha) // for single data
        {
            float Y;

            Y = alpha * X + alpha * (1 - alpha) * Xpi + (1 - alpha) * (1 - alpha) * Yppi;

            Xpi = X;
            Yppi = Ypi;
            Ypi = Y;

            return Y;
        }
        public Vector3 SecondOrderLowpassFilter(Vector3 X, float alpha) // for Vector3 data with single parameter
        {
            Vector3 Y;

            Y = alpha * X + alpha * (1 - alpha) * Xpj + (1 - alpha) * (1 - alpha) * Yppj;

            Xpj = X;
            Yppj = Ypj;
            Ypj = Y;

            return Y;
        }
        public Vector3 SecondOrderLowpassFilter(Vector3 X, Vector3 alpha) // for Vector3 data with multiple parameter
        {
            Vector3 Y;

            Y.x = alpha.x * X.x + alpha.x * (1 - alpha.x) * Xpj.x + (1 - alpha.x) * (1 - alpha.x) * Yppj.x;
            Y.y = alpha.y * X.y + alpha.y * (1 - alpha.y) * Xpj.y + (1 - alpha.y) * (1 - alpha.y) * Yppj.y;
            Y.z = alpha.z * X.z + alpha.z * (1 - alpha.z) * Xpj.z + (1 - alpha.z) * (1 - alpha.z) * Yppj.z;

            Xpj = X;
            Yppj = Ypj;
            Ypj = Y;

            return Y;
        }
        public float[] SecondOrderLowpassFilter(float[] X, float alpha) // for multiple dimension array data with single parameter
        {
            float[] Y = new float[X.Length];

            for(int i = 0; i < X.Length; i++)
            {
                Y[i] = alpha * X[i] + alpha * (1 - alpha) * Xpk[i] + (1 - alpha) * (1 - alpha) * Yppk[i];
            }

            Xpk = X;
            Yppk = Ypk;
            Ypk = Y;

            return Y;
        }
        public float[] SecondOrderLowpassFilter(float[] X, float[] alpha) // for multiple dimension array data with multiple parameter
        {
            float[] Y = new float[X.Length];

            for(int i = 0; i < X.Length; i++)
            {
                Y[i] = alpha[i] * X[i] + alpha[i] * (1 - alpha[i]) * Xpk[i] + (1 - alpha[i]) * (1 - alpha[i]) * Yppk[i];
            }

            Xpk = X;
            Yppk = Ypk;
            Ypk = Y;

            return Y;
        }

        // lowpass filter for third order
        public float ThirdOrderLowpassFilter(float X, float alpha) // for single data
        {
            float Y;
            
            Y = alpha * X + alpha * (1 - alpha) * Xpi + alpha * (1 - alpha) * (1 - alpha) * Xppi + (1 - alpha) * (1 - alpha) * (1 - alpha) * Ypppi;

            Xppi = Xpi;
            Xpi = X;
            Ypppi = Yppi;
            Yppi = Ypi;
            Ypi = Y;

            return Y;
        }
        public Vector3 ThirdOrderLowpassFilter(Vector3 X, float alpha) // for Vector3 data with multiple parameter
        {
            Vector3 Y;
            
            Y = alpha * X + alpha * (1 - alpha) * Xpj + alpha * (1 - alpha) * (1 - alpha) * Xppj + (1 - alpha) * (1 - alpha) * (1 - alpha) * Ypppj;

            Xppj = Xpj;
            Xpj = X;
            Ypppj = Yppj;
            Yppj = Ypj;
            Ypj = Y;

            return Y;
        }
        public Vector3 ThirdOrderLowpassFilter(Vector3 X, Vector3 alpha) // for Vector3 data with single parameter
        {
            Vector3 Y;
            
            Y.x = alpha.x * X.x + alpha.x * (1 - alpha.x) * Xpj.x + alpha.x * (1 - alpha.x) * (1 - alpha.x) * Xppj.x + (1 - alpha.x) * (1 - alpha.x) * (1 - alpha.x) * Ypppj.x;
            Y.y = alpha.y * X.y + alpha.y * (1 - alpha.y) * Xpj.y + alpha.y * (1 - alpha.y) * (1 - alpha.y) * Xppj.y + (1 - alpha.y) * (1 - alpha.y) * (1 - alpha.y) * Ypppj.y;
            Y.z = alpha.z * X.z + alpha.z * (1 - alpha.z) * Xpj.z + alpha.z * (1 - alpha.z) * (1 - alpha.z) * Xppj.z + (1 - alpha.z) * (1 - alpha.z) * (1 - alpha.z) * Ypppj.z;

            Xppj = Xpj;
            Xpj = X;
            Ypppj = Yppj;
            Yppj = Ypj;
            Ypj = Y;

            return Y;
        }
        public float[] ThirdOrderLowpassFilter(float[] X, float alpha) // for multiple dimension array data with single parameter
        {
            float[] Y = new float[X.Length];
            
            for(int i = 0; i < X.Length; i++)
            {
                Y[i] = alpha * X[i] + alpha * (1 - alpha) * Xpk[i] + alpha * (1 - alpha) * (1 - alpha) * Xppk[i] + (1 - alpha) * (1 - alpha) * (1 - alpha) * Ypppk[i];
            }

            Xppk = Xpk;
            Xpk = X;
            Ypppk = Yppk;
            Yppk = Ypk;
            Ypk = Y;

            return Y;
        }
        public float[] ThirdOrderLowpassFilter(float[] X, float[] alpha) // for multiple dimension array data with multiple parameter
        {
            float[] Y = new float[X.Length];
            
            for(int i = 0; i < X.Length; i++)
            {
                Y[i] = alpha[i] * X[i] + alpha[i] * (1 - alpha[i]) * Xpk[i] + alpha[i] * (1 - alpha[i]) * (1 - alpha[i]) * Xppk[i] + (1 - alpha[i]) * (1 - alpha[i]) * (1 - alpha[i]) * Ypppk[i];
            }

            Xppk = Xpk;
            Xpk = X;
            Ypppk = Yppk;
            Yppk = Ypk;
            Ypk = Y;

            return Y;
        }

        // clear the lowpass filter parameters
        public void Cleari()
        {
            Ypi = 0f;
            Yppi = 0f;
            Ypppi = 0f;
            Xpi = 0f;
            Xppi = 0f;
        }
        public void Clearj()
        {
            Ypj = Vector3.zero;
            Yppj = Vector3.zero;
            Ypppj = Vector3.zero;
            Xpj = Vector3.zero;
            Xppj = Vector3.zero;
        }
        public void Cleark()
        {
            Array.Clear(Ypk, 0, Ypk.Length);
            Array.Clear(Yppk, 0, Yppk.Length);
            Array.Clear(Ypppk, 0, Ypppk.Length);
            Array.Clear(Xpk, 0, Xpk.Length);
            Array.Clear(Xppk, 0, Xppk.Length);
        }
    }
}
