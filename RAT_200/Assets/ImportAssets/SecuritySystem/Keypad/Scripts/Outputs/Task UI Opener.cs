using UnityEngine;

namespace Task2
{
    public class TaskUIOpener : MonoBehaviour
    {
        [SerializeField] GameObject UI;
        bool locked = false ;

        void Start()
        {
             UI.SetActive(false);
        }

        void Update()
        {
            if (locked == false)
            {
                UI.SetActive(true);
            }
            else
            {
                UI.SetActive(false);
            }

            if (Input.GetKeyDown(KeyCode.E) && UI.active)
            {
                UI.SetActive(false);
                locked = true;
            }
        }
    }
}
