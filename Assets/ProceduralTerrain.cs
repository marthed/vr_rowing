using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralTerrain : MonoBehaviour
{

    public GameObject target;

    public GameObject terrain1;
    public GameObject terrain2;
    public GameObject terrain3;

    private LinkedList<GameObject> Terrains = new LinkedList<GameObject>();

    private Vector3 terrain1StartPos;
    private Vector3 terrain2StartPos;
    private Vector3 terrain3StartPos;

    void Start()
    {
        Terrains.AddFirst(terrain3);
        Terrains.AddFirst(terrain2);
        Terrains.AddFirst(terrain1);

        terrain1StartPos = terrain1.transform.position;
        terrain2StartPos = terrain2.transform.position;
        terrain3StartPos = terrain3.transform.position;

        foreach(GameObject t in Terrains) {
            Debug.Log("TERRAIN: " + t.name);
            t.GetComponentInChildren<EnterTerrain>().OnHit += OnTerrainEnter;
        }
    }

    public void ResetTerrainPositions() {
        Debug.Log("Reset terrain positions");
        terrain1.transform.position = terrain1StartPos;
        terrain2.transform.position = terrain2StartPos;
        terrain3.transform.position = terrain3StartPos;
    }

    void OnTerrainEnter(EnterTerrain enterTerrain) {

        // FIX SPAWN BUG (Occurs after a while);
        
        Debug.Log(enterTerrain.GetTerrain().name + "   " + Terrains.Last.Value.name);

        foreach(GameObject t in Terrains) {
            Debug.Log("TERRAIN: " + t.name);
        }

        if (Terrains.First.Value == enterTerrain.GetTerrain()) {
            Debug.Log("Entered first terrain");
            // Move first terrain transform last
            //float zDistance = 3 * Vector3.Distance(new Vector3(0, 0, Terrains.First.Value.transform.position.z), new Vector3(0, 0, Terrains.First.Next.Value.transform.position.z)); // ONLY WORKS FOR 3 TERRAINS
            float zDistance = 3000;
            Terrains.Last.Value.transform.position -= new Vector3(0, 0, zDistance); 
            

            // Move last terrain node first in linked list
            GameObject lastTerrain = Terrains.Last.Value;
            Terrains.RemoveLast();
            Terrains.AddFirst(lastTerrain);
                       
            
        }

        else if (Terrains.Last.Value == enterTerrain.GetTerrain()) {
            Debug.Log("Entered last terrain");
            // Move last terrain transform first
            //float zDistance = 3 * Vector3.Distance(new Vector3(0, 0, Terrains.First.Value.transform.position.z), new Vector3(0, 0, Terrains.First.Next.Value.transform.position.z));
            
            float zDistance = 3000;
            Debug.Log("Distance: " + zDistance);

            Terrains.First.Value.transform.position += new Vector3(0, 0, zDistance); 

            // Move first terrain node last in linked list
            GameObject firstTerrain = Terrains.First.Value;
            Terrains.RemoveFirst();
            Terrains.AddLast(firstTerrain); 
            
        }
    }
}
