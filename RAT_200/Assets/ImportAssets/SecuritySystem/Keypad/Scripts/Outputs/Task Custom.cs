using UnityEngine;
using UnityEngine.Events;

namespace Task3
{
    public class TaskCustom: MonoBehaviour
    {
        [SerializeField] UnityEvent OnOutput;
      
        void Update() //Here you can try to code something...
        {
            OnOutput.Invoke(); 
        }
    }
}
