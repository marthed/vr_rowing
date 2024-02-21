using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartRow : MonoBehaviour
{
    // Start is called before the first frame update

    public HandleBar handleBar;
    public delegate void TriggerEventHandler();
    public event TriggerEventHandler OnTriggerEnterEvent;
    
    void Update() {

        //if (!GameManager.Instance.InStartPosition) {
         //   if ((handleBar.transform.position - gameObject.transform.position).magnitude < 0.1f) {
          //    OnTriggerEnterEvent.Invoke();
           // }
        //}
    }
}
