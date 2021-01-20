using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using MathematicUtility.Definition;
using AvatarRetarget.Filter;

public class KalmanTest : MonoBehaviour
{
    // define the discrete measurement
    public List<Matrixf> meas = new List<Matrixf>();
    
    // define the initial state and estimate uncertainty
    public static float[,] state = new float[,] {{10f}};
    public static float[,] uncertainty = new float[,] {{10000f}};
    public static Matrixf stateVector = new Matrixf(state);
    public static Matrixf estimateUncertainty = new Matrixf(uncertainty);

    // define the kalman filter
    public KalmanFilterf kff = new KalmanFilterf(1, 1, 1, stateVector, estimateUncertainty);
    public Matrixf F = new Matrixf();
    public Matrixf G = new Matrixf();
    public Matrixf u = new Matrixf();
    public Matrixf w = new Matrixf();
    public Matrixf H = new Matrixf();
    public Matrixf v = new Matrixf();

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("xf_init: " + kff.xf.matrixf[0,0].ToString("#.00"));
        Debug.Log("Pf_init: " + kff.Pf.matrixf[0,0].ToString("#.0000"));
        
        meas.AddRange(
            new Matrixf[] {
                new Matrixf(new float[,]{{50.45f}}),
                new Matrixf(new float[,]{{50.967f}}),
                new Matrixf(new float[,]{{51.6f}}),
                new Matrixf(new float[,]{{52.106f}}),
                new Matrixf(new float[,]{{52.492f}}),
                new Matrixf(new float[,]{{52.819f}}),
                new Matrixf(new float[,]{{53.433f}}),
                new Matrixf(new float[,]{{54.007f}}),
                new Matrixf(new float[,]{{54.523f}}),
                new Matrixf(new float[,]{{54.99f}}),
            }); 

        F = new Matrixf(new float[,]{{1.0f}});
        G = new Matrixf(new float[,]{{1.0f}});
        u = new Matrixf(new float[,]{{0f}});
        w = new Matrixf(new float[,]{{0.387298f}});
        H = new Matrixf(new float[,]{{1.0f}});
        v = new Matrixf(new float[,]{{0.1f}});

        kff.KalmanInitializef(F, G, u, w, H, v);
        Debug.Log("xf: " + kff.xf.matrixf[0,0].ToString("#.00"));
        Debug.Log("Pf: " + kff.Pf.matrixf[0,0].ToString("#.0000"));
       

        for(int i = 0; i < 10; i++)
        {
            Debug.Log("Measurement Temperature: " + meas[i].matrixf[0,0].ToString("#.000"));

            kff.KalmanCorrectf(meas[i]);
            kff.KalmanPredictf();
            
            Debug.Log("Estimate Temperature: " + kff.xf.matrixf[0,0].ToString("#.00"));
        }   
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
