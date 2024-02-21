using System.Collections;
using UnityEngine;
using Singleton;
using System;
using CustomAttributes;
using GameLogic;


public class GameManager : Singleton<GameManager>
{
    [Header("Settings")]
    public int coinDistance = 90;

    [Range(0, 90)] [SerializeField] public float coinAngle2D = 30;
    [Range(0, 90)] [SerializeField] public float coinAngle3D = 20;
    [SerializeField] public Vector3 playerStartPosition = new Vector3(0, 1, 0);
    [SerializeField] public Vector3 startCoinSpawnPosition = new Vector3(0, 2.5f, 30);
    public SelectedTrack selectedTrack = SelectedTrack.Track1;

    [Header("Track settings")]
    [field: SerializeField] public CoinPosition[] Track1Pos;
    [field: SerializeField] public CoinPosition[] Track2Pos;
    [field: SerializeField] public CoinPosition[] Track3Pos;
    [field: SerializeField] public CoinPosition[] Track4Pos;
    [field: SerializeField] public CoinPosition[] Track5Pos;
    [field: SerializeField] public CoinPosition[] Track6Pos;

    [Header("State")]
    private int dummy = 1;
    [field: SerializeField, ReadOnly] private GameState CurrentGameState { get; set; }
    [field: SerializeField, ReadOnly] private int CurrentGoldCoinNumber { get; set; }
    [field: SerializeField, ReadOnly] private Track CurrentTrack {get; set; }

    [field: SerializeField, ReadOnly] private DataCoin[] DataCoins { get; set; }

    [Header("Dependencies")]
    public RowLocomotionController rowLocomotionController;
    public GameObject goldCoinPrefab;
    public GameObject missCoinZonePrefab;
    public GameObject environment;
    public GameObject conePrefab;
    public SensorLogging sensorLogging;
    public GameObject traingingGrounObjects;

    #region "internal"
    public  Track[] _tracks = new Track[6];
    private GoldCoin _coin1;
    private GoldCoin _coin2;
    private MissCoinZone _missCoinZone;
    private Cone _cone;
    private float startTime = 0;
    private float minCoinDistance = 0;
    private float travelDistance = 0;
    private Vector3 previousPosition;
    #endregion

    void Start() {
        sensorLogging = gameObject.GetComponent<SensorLogging>();
        LogExternalSensors();

        SetGameStateSetup();
        GenerateTracks();

    }

    public GameState GetCurrentGameState() {
        return CurrentGameState;
    }

    private void DebugCoinPositions() {

        for (int i = 0; i < CurrentTrack.coinPositions.Length; i++) {
            Vector3 position = CurrentTrack.coinPositions[i].position;
            Instantiate(goldCoinPrefab, position, Quaternion.identity);
        }
    }

    IEnumerator DummyTestCoins() {


        while (CurrentGoldCoinNumber <  CurrentTrack.coinPositions.Length) {
            Debug.Log("Next dummy coin: " + CurrentGoldCoinNumber);
            System.Random random = new System.Random();

            if (random.Next(2) == 1) {
                CoinHasBeenHit(_coin1);
            }
            else {
                CoinHasBeenMissed();
            }


            yield return new WaitForSeconds(0.5f);

        }
        Debug.Log("Dummy trail generated");
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.Return)) {
            //StartCoroutine(DummyTestCoins());
            DebugCoinPositions();
        }
        if (Input.GetKeyDown(KeyCode.Escape)) {
            NextGameState();
        }
    }

    public void NextGameState() {
        switch (CurrentGameState) {
            case GameState.Setup:
                CurrentGameState = GameState.Testing;
                traingingGrounObjects.SetActive(true);
                rowLocomotionController.enabled = true;
                CurrentGoldCoinNumber = 0;
                environment.GetComponent<ProceduralTerrain>().ResetTerrainPositions();
                ResetPlayer();
                minCoinDistance = 0;
                previousPosition = rowLocomotionController.transform.position;
                InfoBoard.Instance.SetGameStateText("Training Ground");
                InfoBoard.Instance.SetStatsText("");
                break;
            case GameState.Testing:
                traingingGrounObjects.SetActive(false);
                CurrentGameState = GameState.Trail;
                CurrentGoldCoinNumber = 0;
                
                InitTrack(selectedTrack);
                startTime = 0;
                travelDistance = 0;
                environment.GetComponent<ProceduralTerrain>().ResetTerrainPositions();

                _coin1.gameObject.SetActive(true);
                _coin2.gameObject.SetActive(true);
                _missCoinZone.gameObject.SetActive(true);
                _cone.gameObject.SetActive(true);

                SpawnCone();
                
                ResetPlayer();
                ResetMinCoinDistance();
                previousPosition = rowLocomotionController.transform.position;
                InfoBoard.Instance.SetGameStateText("Trail: " + rowLocomotionController.steeringMethod + " " + rowLocomotionController.travelMode + " Track: " + selectedTrack.ToString());
                InfoBoard.Instance.SetStatsText("");
                break;
            case GameState.Trail:
                traingingGrounObjects.SetActive(false);
                CurrentGameState = GameState.Completed_Trail;
                TrailCompleted();
                // Text set in on coin hit, maybe change?
                break;
            case GameState.Completed_Trail:
                traingingGrounObjects.SetActive(false);
                SetGameStateSetup();
                break;
            case GameState.Finish:
                InfoBoard.Instance.SetGameStateText("Finished");
                InfoBoard.Instance.SetStatsText("Here all stats should be displayed");
                break;
        }
    }

    public void SetSelectedTrack(string message) {

        string[] input = message.Split(";"); // Since minus values exist; 
        int trackNumber = int.Parse(input[1]);

        if (trackNumber == 1) {
             selectedTrack = SelectedTrack.Track1;
        }
        else if (trackNumber == 2) {
            selectedTrack = SelectedTrack.Track2;
        }
        else if (trackNumber == 3) {
            selectedTrack  = SelectedTrack.Track3;
        }
        else if (trackNumber == 4) {
            selectedTrack = SelectedTrack.Track4;
        }
        else if (trackNumber == 5) {
            selectedTrack = SelectedTrack.Track5;
        }
        else if (trackNumber == 6) {
            selectedTrack = SelectedTrack.Track6;
        }


    }

    public void ResetPlayer() {
        rowLocomotionController.transform.position = playerStartPosition;
        rowLocomotionController.transform.rotation = Quaternion.identity;
        rowLocomotionController.GetComponent<Rigidbody>().velocity = Vector3.zero;
        rowLocomotionController.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
    }

    public void ResetMinCoinDistance() {
        minCoinDistance = Vector3.Distance(rowLocomotionController.transform.position, _coin1.transform.position);
    }

    public void SetGameStateSetup() {
        CurrentGameState = GameState.Setup;
        rowLocomotionController.enabled = false;
        InfoBoard.Instance.SetGameStateText("Setup");
        InfoBoard.Instance.SetStatsText("");
    }

    public void InitTrack(SelectedTrack track) {
        CurrentTrack = GetTrack(track);
        DataCoins = new DataCoin[CurrentTrack.coinPositions.Length];
        SpawnStartCoin();
        SpawnSecondCoin();
        SpawnMissCoinZone();
        SpawnCone();
        ResetMinCoinDistance();
    }

    void SpawnStartCoin() {
        GameObject goldCoinObject;
        
        if (_coin1 == null) {
            goldCoinObject =  Instantiate(goldCoinPrefab, startCoinSpawnPosition, Quaternion.identity);
            _coin1 = goldCoinObject.GetComponent<GoldCoin>();
            _coin1._id = 1;
            _coin1.OnHit += CoinHasBeenHit;
        }
        else {
            goldCoinObject = _coin1.gameObject;
            goldCoinObject.SetActive(true);
            goldCoinObject.transform.position = startCoinSpawnPosition;
            _coin1._id = 1;
        }
        CurrentGoldCoinNumber = 0;
    }

    void SpawnSecondCoin() {
      Vector3 pos = CurrentTrack.coinPositions[CurrentGoldCoinNumber + 1].position;
      GameObject goldCoinObject;

      if (_coin2 == null) {
        goldCoinObject =   Instantiate(goldCoinPrefab, pos, Quaternion.identity);

      }
      else {
            goldCoinObject = _coin2.gameObject;
            goldCoinObject.SetActive(true);
            goldCoinObject.transform.position = pos;
      }
      
      _coin2 = goldCoinObject.GetComponent<GoldCoin>();
      _coin2._id = 2;
      //_coin2.OnHit += CoinHasBeenHit;
      
    }

    void SpawnCone() {
        GameObject coneObject;
        
        if (_cone == null) {
            coneObject =  Instantiate(conePrefab, _coin1.transform.position + new Vector3(0, 10, 0), Quaternion.identity);
        }
        else {
            coneObject = _cone.gameObject;
            coneObject.SetActive(true);
            coneObject.transform.position = _coin1.transform.position + new Vector3(0, 10, 0);
        }
        
        _cone = coneObject.GetComponent<Cone>();
    }

    void SpawnMissCoinZone() {
        
        GameObject missCoinZoneObject;

        if (_missCoinZone == null) {
            missCoinZoneObject =  Instantiate(missCoinZonePrefab, _coin1.transform.position + new Vector3(0, 0, 10), Quaternion.identity);
        }
        else {
            missCoinZoneObject = _missCoinZone.gameObject;
            missCoinZoneObject.SetActive(true);
            missCoinZoneObject.transform.position = _coin1.transform.position + new Vector3(0, 0, 10);
        }
        
        _missCoinZone = missCoinZoneObject.GetComponent<MissCoinZone>();
        _missCoinZone.OnHit += CoinHasBeenMissed;
    }

    public void GenerateTracks() {
        for (int i = 0; i < _tracks.Length; i++) {
           
            CoinPosition[] coinPositions = GetCoinPositions(i+1);
            _tracks[i] = new Track(coinDistance, coinAngle2D, coinAngle3D, coinPositions, startCoinSpawnPosition);
            
        }
    }
    

    private Track GetTrack(SelectedTrack track) {
        switch (track) {
            case SelectedTrack.Track1:
                return _tracks[0];
            case SelectedTrack.Track2:
                return _tracks[1];
            case SelectedTrack.Track3:
                return _tracks[2];
            case SelectedTrack.Track4:
                return _tracks[3];
            case SelectedTrack.Track5:
                return _tracks[4];
            case SelectedTrack.Track6:
                return _tracks[5];
            default:
                return null;
    }
}

    private CoinPosition[] GetCoinPositions(int trackNumber) {
        switch (trackNumber) {
            case 1:
                return Track1Pos;
            case 2:
                return Track2Pos;
            case 3:
                return Track3Pos;
            case 4:
                return Track4Pos;
            case 5:
                return Track5Pos;
            case 6:
                return Track6Pos;
            default:
                return Track1Pos;
        }
    }

    private void RecordHit(bool wasHit) {
        float time = Time.time - startTime;

        float missDis = wasHit ? 0 : minCoinDistance;

        string sensorMetrics = sensorLogging.GetMetricsAsString();

        DataCoin dataCoin = new DataCoin(CurrentGoldCoinNumber, wasHit, CurrentTrack.coinPositions[CurrentGoldCoinNumber], time, missDis, travelDistance, sensorMetrics);
        DataCoins[CurrentGoldCoinNumber] = dataCoin;
        sensorLogging.Flush();

    }

    private void UpdateGoldCoinPositions() {

        Vector3 missCoinZoneDir = (_coin2.transform.position - _coin1.transform.position) / 2;
        _coin1.transform.position = _coin2.transform.position;

        _missCoinZone.transform.position = _coin1.transform.position + missCoinZoneDir;
        _missCoinZone.transform.right =  Vector3.Cross(missCoinZoneDir, Vector3.up);

        _cone.transform.position = _coin1.transform.position + new Vector3(0, 10, 0);

        if (CurrentGoldCoinNumber + 1 < CurrentTrack.coinPositions.Length) {
            CoinPosition nextCoinPosition = CurrentTrack.coinPositions[CurrentGoldCoinNumber + 1];
            _coin2.transform.position = nextCoinPosition.position;
        }
        else {
            _coin2.transform.position += new Vector3(0, 0, 30);
            _coin2.gameObject.SetActive(false);
        }
    }


    public void CoinHasBeenHit(GoldCoin caller) {
        if (CurrentGoldCoinNumber == 0) {
            startTime = Time.time;
            RecordHit(true);
            ResetMinCoinDistance();

            CurrentGoldCoinNumber = 1;
            UpdateGoldCoinPositions();
            coinDistance = 0;
            return;
        }

        if (caller._id == 1) {
            
            Debug.Log("Hit coin number: " + CurrentGoldCoinNumber);

            RecordHit(true);
            ResetMinCoinDistance();

            CurrentGoldCoinNumber += 1;
            coinDistance = 0;

            if (CurrentGoldCoinNumber == CurrentTrack.coinPositions.Length) {
                TrailCompleted();
                return;
            }

            UpdateGoldCoinPositions();

        }
        else {
            Debug.Log("Missing ID for coin");
        }
    }

    public void CoinHasBeenMissed() {

        if (CurrentGoldCoinNumber == 0) {
            Debug.Log("BAAAAAD");

            startTime = Time.time;

            RecordHit(false);
            ResetMinCoinDistance();


            UpdateGoldCoinPositions();
            CurrentGoldCoinNumber = 1;
            return;
        }

        Debug.Log("Miss coin number: " + CurrentGoldCoinNumber);

        RecordHit(false);
        ResetMinCoinDistance();

        CurrentGoldCoinNumber += 1;

        coinDistance = 0;
                
        if (CurrentGoldCoinNumber == CurrentTrack.coinPositions.Length) {
            NextGameState();
            return;
        }

        UpdateGoldCoinPositions();

    }

    void TrailCompleted() {

        if (CurrentGoldCoinNumber < CurrentTrack.coinPositions.Length) {
            Debug.Log("Track cancelled!");
            _coin1.gameObject.SetActive(false);
             _coin2.gameObject.SetActive(false);
            _missCoinZone.gameObject.SetActive(false);
            _cone.gameObject.SetActive(false);

            SetGameStateSetup(); 

        }
        else {
            Debug.Log("Trail completed!");
            _coin1.gameObject.SetActive(false);
            _missCoinZone.gameObject.SetActive(false);
            _coin2.gameObject.SetActive(false);
            _cone.gameObject.SetActive(false);

            int hits = 0;
            int missed = 0;

            for (int i = 0; i < DataCoins.Length; i++) {
                DataCoin dataCoin = DataCoins[i];
                
                if (dataCoin.wasHit) {
                    hits += 1;
                }
                else {
                    missed += 1;
                }
            }

            gameObject.GetComponent<AudioSource>().Play(); // Plays fanfare
            InfoBoard.Instance.SetStatsText("Time: "+ Mathf.Round((Time.time - startTime) * 10f) / 10f + "sec" + "  Hits: " + hits + "  Missed: " + missed);
            FormatDataAndSend();
            Debug.Log("Complete!");
        }
        
    }


    private void FormatDataAndSend() {
        
        string metricsOrder = sensorLogging.GetMetricOrder();

        string dataString = "method,mode,hit,coin,time,direction,altitude,x,y,z,missdistance,traveldistance," + metricsOrder + "\n";

        string method = rowLocomotionController.steeringMethod.ToString();
        string mode = rowLocomotionController.travelMode.ToString();

        
        for (int i = 0; i < DataCoins.Length; i++) {
            DataCoin dataCoin = DataCoins[i];
            Vector3 position = dataCoin.coinPos.position;
            dataString = dataString + method + ","+ mode + "," + dataCoin.wasHit + "," + dataCoin.coinOrder + "," + dataCoin.time + "," + dataCoin.coinPos.direction.ToString() + "," + dataCoin.coinPos.altitude.ToString() + ","+ position.x + "," + position.y + "," + position.z +  "," + dataCoin.missDistance +  ","+ dataCoin.travelDistance + "," + dataCoin.sensorMetrics + "\n";
        }
        try {
   
            HTTPClient.Instance.PostRequest("trail", dataString);
            HTTPClient.Instance.PostRequest("state", "" + GetCurrentGameState());

        } catch (Exception e) {
            Debug.Log(e);
        } 

    }

    public void LogExternalSensors() {
        rowLocomotionController.OnFeetInput.AddListener(LogFeet);
        rowLocomotionController.OnHandInput.AddListener(LogHand);
        rowLocomotionController.OnHeadInput.AddListener(LogHead);
    }

    

    public void LogFeet(int leftHeel, int rightHeel, int leftToe, int rightToe) {
        if (CurrentGameState == GameState.Trail) {
            sensorLogging.LogFeetPressure(leftToe, leftHeel, rightToe, rightHeel);
        }

    }

    public void LogHand(int down, int up, Vector3 gyro) {
        if (CurrentGameState == GameState.Trail) {
            sensorLogging.LogButtonPress(down, up);
            sensorLogging.LogHandRotation(gyro);
        }
    }

    public void LogHead(Vector3 headRotation) {
        if (CurrentGameState == GameState.Trail) {
            sensorLogging.LogHeadRotation(headRotation);
        }
    }

    void FixedUpdate() {

        if (CurrentGameState == GameState.Trail) {
            float coinDistance = Vector3.Distance(_coin1.transform.position, rowLocomotionController.transform.position);
                if (coinDistance < minCoinDistance) {
                    minCoinDistance = coinDistance;
                }

            travelDistance += Vector3.Distance(previousPosition, rowLocomotionController.transform.position);
            previousPosition = rowLocomotionController.transform.position;
            
        }
        

    }



}
