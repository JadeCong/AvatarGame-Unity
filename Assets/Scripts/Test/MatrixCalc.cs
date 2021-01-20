using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using MathematicUtility.Definition;

public class MatrixCalc : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        // float[] haha = new float[] {1f, 2f, 3f};
        // Vectorf hehe = new Vectorf(haha);
        // float a = 1f;
        // Vectorf res1 = hehe + a;
        // for(int i = 0; i < res1.size; i++)
        // {
        //     Debug.Log("Vectorf res: " + res1[i]);
        // }
        

        // float[,] src1 = new float[,] {{1f, 0f, 1f}, {2f, 2f, 0f}, {0f, 3f, 3f}};
        float[,] src1 = new float[,] {{1f, 2f, 3f}, {2f, 2f, 1f}, {3f, 4f, 3f}};
        float[,] src2 = new float[,] {{1f, 0f, 0f}, {0f, 2f, 0f}, {0f, 0f, 3f}};
        Matrixf test1 = new Matrixf(src1);
        Matrixf test2 = new Matrixf(src2);
        float b = 2.0f;

        // Matrixf res = Matrixf.Ones(test1);
        // Matrixf res = Matrixf.Zeros(test1);

        // Matrixf res = test1 + b;
        // Matrixf res = test1 + test2;
        // Matrixf res = test1 - b;
        // Matrixf res = test1 - test2;
        
        // Matrixf res = test1 * test2;
        // Matrixf res = test1 * b;
        // Matrixf res = Matrixf.Mul(test1, test2);
        // Matrixf res = test2 / test1;
        // Matrixf res = test1 / b;
        // Matrixf res = Matrixf.Div(test1, test2);

        // Matrixf res = Matrixf.Transpose(test1);
        Matrixf res = Matrixf.Inverse(test1);
        // if(test1 != test2)
        // {
        //     Debug.Log("That's right, test1 are not equal to test2.");
        // }

        Debug.Log("Get the inverse of the given matrixf......");
        Debug.Log("res.rows: " + res.rows);
        Debug.Log("res.columns: " + res.columns);

        for(int i = 0; i < res.rows; i++)
        {
            for(int j = 0; j < res.columns; j++)
            {
                Debug.Log("No.: " + res.matrixf[i,j]);
            }
        }

        // displayMatrix(res.matrixf);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public static void displayMatrix(float[,] matrix)
    {
        for(int i=0;i<matrix.GetLength(0);i++)
        {
            for (int j = 0; j < matrix.GetLength(1); j++)
            {
                Console.Write("{0,4}", matrix[i,j]);
            }
            Console.WriteLine();
        }
    }
}
