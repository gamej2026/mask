using UnityEngine;

public class SimpleTest : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod]
    static void RunTest()
    {
        Debug.Log("SimpleTest: Scripts compiled successfully. Ready to Play.");
    }
}
