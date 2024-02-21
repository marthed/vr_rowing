using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResetView : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] Transform player;
    [SerializeField] Transform trackingSpace;
    public bool resetFromDesktop = true;

    [ContextMenu("Reset Position")]
    public void ResetPosition() {

        

        
    }

    void FixedUpdate() {
        if (resetFromDesktop && Input.GetKeyDown(KeyCode.L)) {
            ResetPosition();
        }
    }
}
