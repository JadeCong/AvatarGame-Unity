using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using MathematicUtility.Definition; // for mathematics

namespace AvatarRetarget.Filter
{
    // --- float type --- //
    public class KalmanFilterf
    {
        // define the variables
        public int nxf; // state vector dimension
        public int nzf; // output vector dimension
        public int nuf; // input variable dimension
        public Matrixf xf; // state vector(nx x 1)
        public Matrixf zf; // output vector(nz x 1)
        public Matrixf Ff; // state transition matrix(nx x nx)
        public Matrixf uf; // input variable(nu x 1)
        public Matrixf Gf; // control matrix(nx x nu)
        public Matrixf Pf; // estimate uncertainty(nx x nx)
        public Matrixf Qf; // process noise uncertainty(nx x nx)
        public Matrixf Rf; // measurement uncertainty(nz x nz)
        public Matrixf wf; // process noise vector(nx x 1)
        public Matrixf vf; // measurement noise vector(nz x 1)
        public Matrixf Hf; // observation matrix(nz x nx)
        public Matrixf Kf; // kalman gain(nx x nz)

        // initialize the KalmanFilter class(float type)
        public KalmanFilterf(int nx, int nz, int nu, Matrixf initStateVector, Matrixf initEstimateUncertainty)
        {
            // define the dimensions of state vector, process noise vector and measurement noise vector
            nxf = nx;
            nzf = nz;
            nuf = nu;

            // initialize the variables
            xf = new Matrixf(nxf, 1);
            zf = new Matrixf(nzf, 1);
            Ff = new Matrixf(nxf, nxf);
            uf = new Matrixf(nuf, 1);
            Gf = new Matrixf(nxf, nuf);
            Pf = new Matrixf(nxf, nxf);
            Qf = new Matrixf(nxf, nxf);
            Rf = new Matrixf(nzf, nzf);
            wf = new Matrixf(nxf, 1);
            vf = new Matrixf(nzf, 1);
            Hf = new Matrixf(nzf, nxf);
            Kf = new Matrixf(nxf, nzf);

            // assign the initial state vector and estimate uncertainty
            if(initStateVector.rows != xf.rows || initStateVector.columns != xf.columns)
            {
                throw new ArgumentException("Can not assign the initial state vector with different rows or columns.", nameof(initStateVector));
            }
            else
            {
                xf = initStateVector;
            }
            if(initEstimateUncertainty.rows != Pf.rows || initEstimateUncertainty.columns != Pf.columns)
            {
                throw new ArgumentException("Can not assign the initial estimate uncertainty with different rows or columns.", nameof(initEstimateUncertainty));
            }
            else
            {
                Pf = initEstimateUncertainty;
            }
        }

        // KalmanInitializef is called for initializing the initial estimate
        public void KalmanInitializef(Matrixf stateTransMatrix, Matrixf controlMatrix, Matrixf inputVar, Matrixf procNoiseVector, Matrixf obsvMatrix, Matrixf measNoiseVector)
        {
            // assign the variables
            if(stateTransMatrix.rows != Ff.rows || stateTransMatrix.columns != Ff.columns)
            {
                throw new ArgumentException("Can not assign the state transition matrix with different rows or columns.", nameof(stateTransMatrix));
            }
            else
            {
                Ff = stateTransMatrix;
            }
            if(controlMatrix.rows != Gf.rows || controlMatrix.columns != Gf.columns)
            {
                throw new ArgumentException("Can not assign the control matrix with different rows or columns.", nameof(controlMatrix));
            }
            else
            {
                Gf = controlMatrix;
            }
            if(inputVar.rows != uf.rows || inputVar.columns != uf.columns)
            {
                throw new ArgumentException("Can not assign the input variable with different rows or columns.", nameof(inputVar));
            }
            else
            {
                uf = inputVar;
            }
            if(procNoiseVector.rows != wf.rows || procNoiseVector.columns != wf.columns)
            {
                throw new ArgumentException("Can not assign the process noise vector with different rows or columns.", nameof(procNoiseVector));
            }
            else
            {
                wf = procNoiseVector;
            }
            if(obsvMatrix.rows != Hf.rows || obsvMatrix.columns != Hf.columns)
            {
                throw new ArgumentException("Can not assign the observation matrix with different rows or columns.", nameof(obsvMatrix));
            }
            else
            {
                Hf = obsvMatrix;
            }
            if(measNoiseVector.rows != vf.rows || measNoiseVector.columns != vf.columns)
            {
                throw new ArgumentException("Can not assign the measurement noise vector with different rows or columns.", nameof(measNoiseVector));
            }
            else
            {
                vf = measNoiseVector;
            }
            
            // construct the process noise uncertainty and measurement noise uncertainty
            Qf = Matrixf.Mul(wf, Matrixf.Transpose(wf));
            Rf = Matrixf.Mul(vf, Matrixf.Transpose(vf));

            // calculate the initial estimate(initial state vector and estimate uncertainty) for updating iteration
            KalmanPredictf();
        }

        // KalmanCorrectf is called for correcting the predict of the pre-state(first update for every iteration)
        public void KalmanCorrectf(Matrixf measurement)
        {
            // update the output vector(measurement result)
            if(measurement.rows != zf.rows || measurement.columns != zf.columns)
            {
                throw new ArgumentException("Can not update the measurement with different rows or columns.", nameof(measurement));
            }
            else
            {
                zf = measurement;
            }

            // compute the kalman gain and correct the discrete state and estimate uncertainty
            Kf = Matrixf.Mul(Matrixf.Mul(Pf, Matrixf.Transpose(Hf)), Matrixf.Inverse(Matrixf.Mul(Matrixf.Mul(Hf, Pf), Matrixf.Transpose(Hf)) + Rf));
            xf = xf + Matrixf.Mul(Kf, (zf - Matrixf.Mul(Hf, xf)));
            Pf = Matrixf.Mul(Matrixf.Mul((Matrixf.Identity(xf.rows, xf.rows) - Matrixf.Mul(Kf, Hf)), Pf), Matrixf.Transpose(Matrixf.Identity(xf.rows, xf.rows) - Matrixf.Mul(Kf, Hf))) + Matrixf.Mul(Matrixf.Mul(Kf, Rf), Matrixf.Transpose(Kf));
        }

        // KalmanPredictf is called for predicting the state(second update for every iteration)
        public void KalmanPredictf()
        {
            // predict the discrete state and covariance
            xf = Matrixf.Mul(Ff, xf) + Matrixf.Mul(Gf, uf);
            Pf = Matrixf.Mul(Matrixf.Mul(Ff, Pf), Matrixf.Inverse(Ff)) + Qf;
        }
    }

    // --- double type --- //
    public class KalmanFilterd
    {
        // define the variables
        public int nxd; // state vector dimension
        public int nzd; // output vector dimension
        public int nud; // input variable dimension
        public Matrixd xd; // state vector(nx x 1)
        public Matrixd zd; // output vector(nz x 1)
        public Matrixd Fd; // state transition matrix(nx x nx)
        public Matrixd ud; // input variable(nu x 1)
        public Matrixd Gd; // control matrix(nx x nu)
        public Matrixd Pd; // estimate uncertainty(nx x nx)
        public Matrixd Qd; // process noise uncertainty(nx x nx)
        public Matrixd Rd; // measurement uncertainty(nz x nz)
        public Matrixd wd; // process noise vector(nx x 1)
        public Matrixd vd; // measurement noise vector(nz x 1)
        public Matrixd Hd; // observation matrix(nz x nx)
        public Matrixd Kd; // kalman gain(nx x nz)

        // initialize the KalmanFilter class(double type)
        public KalmanFilterd(int nx, int nz, int nu, Matrixd initStateVector, Matrixd initEstimateUncertainty)
        {
            // define the dimensions of state vector, process noise vector and measurement noise vector
            nxd = nx;
            nzd = nz;
            nud = nu;

            // initialize the variables
            xd = new Matrixd(nxd, 1);
            zd = new Matrixd(nzd, 1);
            Fd = new Matrixd(nxd, nxd);
            ud = new Matrixd(nud, 1);
            Gd = new Matrixd(nxd, nud);
            Pd = new Matrixd(nxd, nxd);
            Qd = new Matrixd(nxd, nxd);
            Rd = new Matrixd(nzd, nzd);
            wd = new Matrixd(nxd, 1);
            vd = new Matrixd(nzd, 1);
            Hd = new Matrixd(nzd, nxd);
            Kd = new Matrixd(nxd, nzd);

            // assign the initial state vector and estimate uncertainty
            if(initStateVector.rows != xd.rows || initStateVector.columns != xd.columns)
            {
                throw new ArgumentException("Can not assign the initial state vector with different rows or columns.", nameof(initStateVector));
            }
            else
            {
                xd = initStateVector;
            }
            if(initEstimateUncertainty.rows != Pd.rows || initEstimateUncertainty.columns != Pd.columns)
            {
                throw new ArgumentException("Can not assign the initial estimate uncertainty with different rows or columns.", nameof(initEstimateUncertainty));
            }
            else
            {
                Pd = initEstimateUncertainty;
            }
        }

        // KalmanInitialized is called for initializing the initial estimate
        public void KalmanInitialized(Matrixd stateTransMatrix, Matrixd controlMatrix, Matrixd inputVar, Matrixd procNoiseVector, Matrixd obsvMatrix, Matrixd measNoiseVector)
        {
            // assign the variables
            if(stateTransMatrix.rows != Fd.rows || stateTransMatrix.columns != Fd.columns)
            {
                throw new ArgumentException("Can not assign the state transition matrix with different rows or columns.", nameof(stateTransMatrix));
            }
            else
            {
                Fd = stateTransMatrix;
            }
            if(controlMatrix.rows != Gd.rows || controlMatrix.columns != Gd.columns)
            {
                throw new ArgumentException("Can not assign the control matrix with different rows or columns.", nameof(controlMatrix));
            }
            else
            {
                Gd = controlMatrix;
            }
            if(inputVar.rows != ud.rows || inputVar.columns != ud.columns)
            {
                throw new ArgumentException("Can not assign the input variable with different rows or columns.", nameof(inputVar));
            }
            else
            {
                ud = inputVar;
            }
            if(procNoiseVector.rows != wd.rows || procNoiseVector.columns != wd.columns)
            {
                throw new ArgumentException("Can not assign the process noise vector with different rows or columns.", nameof(procNoiseVector));
            }
            else
            {
                wd = procNoiseVector;
            }
            if(obsvMatrix.rows != Hd.rows || obsvMatrix.columns != Hd.columns)
            {
                throw new ArgumentException("Can not assign the observation matrix with different rows or columns.", nameof(obsvMatrix));
            }
            else
            {
                Hd = obsvMatrix;
            }
            if(measNoiseVector.rows != vd.rows || measNoiseVector.columns != vd.columns)
            {
                throw new ArgumentException("Can not assign the measurement noise vector with different rows or columns.", nameof(measNoiseVector));
            }
            else
            {
                vd = measNoiseVector;
            }

            // construct the process noise uncertainty and measurement noise uncertainty
            Qd = Matrixd.Mul(wd, Matrixd.Transpose(wd));
            Rd = Matrixd.Mul(vd, Matrixd.Transpose(vd));

            // calculate the initial estimate(initial state vector and estimate uncertainty) for updating iteration
            KalmanPredictd();
        }

        // KalmanCorrectd is called for correcting the predict of the pre-state(first update for every iteration)
        public void KalmanCorrectd(Matrixd measurement)
        {
            // update the output vector(measurement result)
            if(measurement.rows != zd.rows || measurement.columns != zd.columns)
            {
                throw new ArgumentException("Can not update the measurement with different rows or columns.", nameof(measurement));
            }
            else
            {
                zd = measurement;
            }

            // compute the kalman gain and correct the discrete state and estimate uncertainty
            Kd = Matrixd.Mul(Matrixd.Mul(Pd, Matrixd.Transpose(Hd)), Matrixd.Inverse(Matrixd.Mul(Matrixd.Mul(Hd, Pd), Matrixd.Transpose(Hd)) + Rd));
            xd = xd + Matrixd.Mul(Kd, (zd - Matrixd.Mul(Hd, xd)));
            Pd = Matrixd.Mul(Matrixd.Mul((Matrixd.Identity(xd.rows, xd.rows) - Matrixd.Mul(Kd, Hd)), Pd), Matrixd.Transpose(Matrixd.Identity(xd.rows, xd.rows) - Matrixd.Mul(Kd, Hd))) + Matrixd.Mul(Matrixd.Mul(Kd, Rd), Matrixd.Transpose(Kd));
        }

        // KalmanPredictd is called for predicting the state(second update for every iteration)
        public void KalmanPredictd()
        {
            // predict the discrete state and covariance
            xd = Matrixd.Mul(Fd, xd) + Matrixd.Mul(Gd, ud);
            Pd = Matrixd.Mul(Matrixd.Mul(Fd, Pd), Matrixd.Inverse(Fd)) + Qd;
        }
    }
}
