using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnterTerrain : MonoBehaviour
{
     public delegate void MyEventHandler(EnterTerrain caller);

    // Declare an event of the delegate type
    public event MyEventHandler OnHit;
    
    private GameObject _terrain { get; set; }

    void Start() {
        _terrain = gameObject.transform.parent.gameObject;
    }

    void OnTriggerEnter(Collider col) {
        if (col.gameObject.tag == "Player") {
            Debug.Log("On new terrain enter!");
            OnHit?.Invoke(this);
        }
    }

    public GameObject GetTerrain() {
        return _terrain;
    }
}
