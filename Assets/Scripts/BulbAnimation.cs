using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulbAnimation : MonoBehaviour
{   
    private float time;
    private Vector3 position;

    public float amplitue = 0.1f;
    public float frequency = 1.0f;

    void Start()
    {
        position = transform.position;
    }

    void Update()
    {
        time += Time.deltaTime;
        
        transform.position = position + new Vector3(0.0f, amplitue * (float)Math.Sin(time * frequency), 0.0f);
    }
}
