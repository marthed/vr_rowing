using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using CustomAttributes;
using System.Runtime.InteropServices;

namespace GameLogic
{
    [Serializable] public enum GameState { // TODO: Use this?
    [SerializeField] Setup,
    [SerializeField] Testing,
    [SerializeField] Trail,
    [SerializeField] Completed_Trail, 
    [SerializeField] Finish
}

[Serializable] public enum SelectedTrack {
    Track1,
    Track2,
    Track3,
    Track4,
    Track5,
    Track6
}
[Serializable]public enum CoinSpawnDirection {
    [SerializeField] Straight,
    [SerializeField] Right,
    [SerializeField] Left,
}

[Serializable] public enum CoinSpawnDistance   {
    [SerializeField] Short,
    [SerializeField] Medium,
    [SerializeField] Long
}

 [Serializable] public enum CoinSpawnAltitude {
     [SerializeField] Same,
     [SerializeField] Down,
     [SerializeField] Up,
}

[Serializable] public class CoinPosition {
   [SerializeField] public CoinSpawnDirection direction;
  [field: SerializeField, ReadOnly] public CoinSpawnDistance distance { get; set; }
    [SerializeField] public CoinSpawnAltitude altitude;
    [field: SerializeField, ReadOnly] public Vector3 position { get; set; }
    public CoinPosition(CoinSpawnDirection dir, CoinSpawnDistance dis, CoinSpawnAltitude alt, Vector3 pos) {
        direction = dir;
        distance = dis;
        altitude = alt;
        position = pos;
    }

    public CoinPosition(Vector3 pos) {
        position = pos;
    }
}

[Serializable] public class DataCoin {
    [SerializeField] public int coinOrder;
    [SerializeField] public bool wasHit;
    [SerializeField] public CoinPosition coinPos;
    [SerializeField] public float time;
    [SerializeField] public float missDistance;

    [SerializeField] public float travelDistance;
    [SerializeField] public string sensorMetrics;

    public DataCoin(int o, bool hit, CoinPosition pos, float t, float md, float td, string sm) {
        coinOrder = o;
        wasHit = hit;
        coinPos = pos;
        time = t;
        missDistance = md;
        travelDistance = td;
        sensorMetrics = sm;
    }
}

    [Serializable] public class Track {

        [SerializeField] public CoinPosition[] coinPositions;
        public Track(float dis, float angle2D, float angle3D, CoinPosition[] cPos, Vector3 startPosition) {
            
            CoinPosition[] tempCoinPos = new CoinPosition[cPos.Length + 1 ];
            tempCoinPos[0] = new CoinPosition(CoinSpawnDirection.Straight, CoinSpawnDistance.Short, CoinSpawnAltitude.Same, startPosition); // Start coin
            Vector3 lastDir = new Vector3(0, 0, 1);

            for (int i = 0; i < cPos.Length; i++) {
                CoinPosition coinPos = cPos[i];

                Vector3 direction =  new Vector3(lastDir.x, 0, lastDir.z).normalized;

                
                if (coinPos.direction == CoinSpawnDirection.Left) {
                    Quaternion rotation = Quaternion.Euler(0f, -angle2D, 0f);
                    direction = rotation * direction;
                }
                else if (coinPos.direction == CoinSpawnDirection.Right) {
                    Quaternion rotation = Quaternion.Euler(0f, angle2D, 0f);
                    direction = rotation * direction;
                }

                if (coinPos.altitude == CoinSpawnAltitude.Down) {
                    Vector2 perpendicularVector = new Vector2(direction.z, -direction.x).normalized;


                    Quaternion rotation = Quaternion.AngleAxis(angle3D, new Vector3(perpendicularVector.x, 0, perpendicularVector.y));
                    
                    //Quaternion.Euler(angle3D, 0f, 0f);
                    //float height = lastDir.y - (rotation * new Vector3(0, 0, 1)).y;
                    direction = rotation * direction;
                }
                else if (coinPos.altitude == CoinSpawnAltitude.Up) {
                    Vector2 perpendicularVector = new Vector2(direction.z, -direction.x);

                    Quaternion rotation = Quaternion.AngleAxis(-angle3D, new Vector3(perpendicularVector.x, 0, perpendicularVector.y));
                    direction = rotation * direction;
                }

                lastDir = direction;
                direction = dis * direction;

                if (i == 0) {
                    coinPos.position = startPosition + direction;
                }
                else {
                    coinPos.position = cPos[i-1].position + direction;
                }
                tempCoinPos[i+1] = coinPos;
            }
            coinPositions = tempCoinPos;
        }

        public void ResetTrack() {
            // TODO: Do some resetting?
        }
    }
    
    
}
