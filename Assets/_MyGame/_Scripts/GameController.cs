using System;
using UnityEngine;

public class GameController : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        
    }

    private void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60; 
    }
}
