using UnityEngine;

public class PerformanceTest : MonoBehaviour
{
    public GameObject testPrefab;
    public Vector3 offset = new Vector3(1, 0, 0);
 
    public Vector3 startPos;
    public void SpawnTestPrefab()
    {
        var newPrefab = Instantiate(testPrefab);

        newPrefab.transform.SetParent(Camera.main.transform);
        newPrefab.transform.localPosition = startPos;
        startPos += offset;
        newPrefab.transform.Rotate(Vector3.up, Random.Range(0, 360));
        
    }
}
