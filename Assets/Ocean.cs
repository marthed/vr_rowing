using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ocean : MonoBehaviour
{

    public GameObject target;
    // Start is called before the first frame update

    private float _height;
    void Start()
    {

        _height = transform.position.y;
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Vector3 position = target.transform.position;
        position.y = _height;
        transform.position = Vector3.Lerp(transform.position, position, 0.5f);
        
    }
}
