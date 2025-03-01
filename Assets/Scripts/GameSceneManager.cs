using UnityEngine;

public class GameSceneManager : MonoBehaviour
{    
    void Start()
    {
        GameManager.Instance.InitLevel();       
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
