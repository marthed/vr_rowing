using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction;
using UnityEngine.Events;

public class GrabInteraction : MonoBehaviour
{
    // Start is called before the first frame update

    private HandGrabInteractable _grabbedObject;

    private InteractableUnityEventWrapper _interactableUnityEventWrapper;

    private PointableUnityEventWrapper _pointableEventWrapper;

    private UnityAction _onSelect;
    void Start()
    {
        _onSelect += OnSelect;

        _grabbedObject = gameObject.GetComponent<HandGrabInteractable>();

        Debug.Log("HAHA: " + _grabbedObject == null);
        Debug.Log(_grabbedObject);



        _interactableUnityEventWrapper.InjectInteractableView(_grabbedObject);

        _interactableUnityEventWrapper.WhenSelect.AddListener(_onSelect);

        //_pointableEventWrapper.WhenSelect.AddListener(_onSelect);
        //_pointableEventWrapper.WhenHover.AddListener(_onSelect);
        
    }



    void OnSelect() {
        Debug.Log("Im selected!");
    }

    // Update is called once per frame
    void Update()
    {

        
    }
}
