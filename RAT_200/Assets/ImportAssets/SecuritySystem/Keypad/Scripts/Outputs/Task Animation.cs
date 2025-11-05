using UnityEngine;

namespace Task1
{
    public class TaskAnimationPlay : MonoBehaviour
    {
        [SerializeField] GameObject Object;
        [SerializeField] AnimationClip Animation;
        private Animator anim;
        bool locked;

        void Start()
        {
            Object.GetComponent<Animator>().enabled = false;
        }

        void Update()
        {
            Object.GetComponent<Animator>().enabled = true;
            Object.GetComponent<Animator>().Play(Animation.name);
        }
    }

}