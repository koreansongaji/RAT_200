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
        if (!b_Light.activeSelf) b_Light.SetActive(true);
        if (s_Light.activeSelf) s_Light.SetActive(false);
            
    }
    
    public void TurnOnSmallLight()
    {
        if(b_Light.activeSelf) b_Light.SetActive(false);
        if(!s_Light.activeSelf) s_Light.SetActive(true);
    }
    
    public void TurnOffAllLight()
    {
        if(b_Light.activeSelf) b_Light.SetActive(false);
        if(s_Light.activeSelf) s_Light.SetActive(false);
    }
}
