using System;
using UnityEngine;

public class ToggleLight : MonoBehaviour
{
    [Header("빛")]
    [SerializeField] private GameObject b_Light;
    [SerializeField] private GameObject s_Light;

    private void Start()
    {
        TurnOnSmallLight();
    }
    public void TurnOnBigLight()
    {
        b_Light.SetActive(true);
        s_Light.SetActive(false);
    }
    
    public void TurnOnSmallLight()
    {
        b_Light.SetActive(false);
        s_Light.SetActive(true);
    }
    
    public void TurnOffAllLight()
    {
        b_Light.SetActive(false);
        s_Light.SetActive(false);
    }
}
