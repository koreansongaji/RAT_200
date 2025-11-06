using Unity.VisualScripting;
using UnityEngine;

public class OpenRefrigerator: MonoBehaviour
{
    public GameObject refrigeratorUnderDoor;
        
    void OnOpenDoor()
    {
        refrigeratorUnderDoor.transform.rotation = Quaternion.Euler(0, -60, 0);
    }
}
