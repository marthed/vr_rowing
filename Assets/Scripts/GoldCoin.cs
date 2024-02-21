using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using CustomAttributes;

public class GoldCoin : MonoBehaviour
{
    [field: SerializeField, ReadOnlyAttribute] public int _id { get; set; }

    public float rotationSpeed = 10f;

    // Define a delegate type for the event
    public delegate void MyEventHandler(GoldCoin caller);

    // Declare an event of the delegate type
    public event MyEventHandler OnHit;

    [field: SerializeField, ReadOnlyAttribute]
    private AudioSource _audioSource;


    void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        StartCoroutine(Rotate()); 
    }


    private IEnumerator Rotate()
    {
        while (true) {
            transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
            yield return null;
        }

    }

    void OnDestroy() {
        StopCoroutine(Rotate());
    }

    void OnTriggerEnter(Collider col) {
        Debug.Log("trigger!");
        if (col.gameObject.tag == "Player") {
            Debug.Log("On coin hit!");
            _audioSource?.Play();
            OnHit?.Invoke(this);
        }
    }

}
