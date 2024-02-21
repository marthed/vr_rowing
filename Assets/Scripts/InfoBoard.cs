using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Singleton;
using TMPro;
using CustomAttributes;
using UnityEngine.UIElements;

public class InfoBoard : Singleton<InfoBoard>
{
    // Start is called before the first frame update

    [Header("Following Settings")]
    public float smoothness = 0.5f;
    public float offset = 75f;

    [Header("Dependencies")]
    public TMP_Text _handsText;
    public TMP_Text _feetText;
    public TMP_Text _antText;
    public TMP_Text _acceleration;

    public TMP_Text _metricsText;
    public TMP_Text _gameStateText;
    public TMP_Text _statsText;
    public TMP_Text alertText;

    #region "internal"
    private float _duration = 0f;
    #endregion

    [field: SerializeField, ReadOnlyAttribute] GameObject target;

    [field: SerializeField] private bool showMetrics { get; set; }


    void Start()
    {
        //ToggleMetrics();
    }

    public void ToggleMetrics() {

        _handsText.gameObject.SetActive(showMetrics);
        _feetText.gameObject.SetActive(showMetrics);
        _antText.gameObject.SetActive(showMetrics);
        _acceleration.gameObject.SetActive(showMetrics);
        _gameStateText.gameObject.SetActive(showMetrics);
        _statsText.gameObject.SetActive(showMetrics);
        alertText.gameObject.SetActive(showMetrics);
        _metricsText.gameObject.SetActive(showMetrics);
        
        showMetrics = !showMetrics;
       
    }

    public void SetRowAccelerationText(string text) {
        _acceleration.text = text;
    }

    public void SetFeetText(string text) {
        _feetText.text = text;
    }

    public void SetHandsText(string text) {
        _handsText.text = text;
    }

    public void SetSpeedText(string text) {
        _antText.text = text;
    }

    public void SetGameStateText(string text) {
        _gameStateText.text = "GAME STATE: " + text.ToUpper();
    }

    public void SetStatsText(string text) {
        _statsText.text =  text;
    }


    public void SetCalibrationText(string text) {
        alertText.text = text;
    }

    public void FollowMe(GameObject g) {
        target = g;
    }

}
