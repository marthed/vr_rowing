using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cone : MonoBehaviour
{
    public float amplitude = 1f;     // Height of the oscillation
    public float frequency = 1f;     // How fast it oscillates


    private float _y;
    void Start()
    {
        _y = transform.position.y;
    }

    void FixedUpdate()
    {
        _y = transform.position.y; 
        float newY = _y + Mathf.Sin(Time.time * frequency) * amplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
}
