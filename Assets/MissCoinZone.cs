using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class MissCoinZone : MonoBehaviour
{
    // Start is called before the first frame update

    // Define a delegate type for the event
    public delegate void MyEventHandler();

    // Declare an event of the delegate type
    public event MyEventHandler OnHit;

    private AudioSource _audioSource;

    void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter(Collider col) {
        if (col.gameObject.tag == "Player") {
            Debug.Log("On coin miss!");
            _audioSource?.Play();
            OnHit?.Invoke();
        }
    }
}
