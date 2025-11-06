using System;
using UnityEngine;

namespace Task1
{
    public class TaskAnimationPlay : MonoBehaviour
    {
        [SerializeField] GameObject Object;
        [SerializeField] AnimationClip Animation;
        private Animator anim;
        void Awake()
        {
            anim = Object != null ? Object.GetComponent<Animator>() : GetComponent<Animator>();
        }
        void Start()
        {
            // 시작 시 아무 것도 실행하지 않음
        }
        private void OnEnable()
        {
            Object.GetComponent<Animator>().enabled = true;
            // Object.GetComponent<Animator>().SetTrigger("Open");
        }
        public void PlayOpen()
        {
            if (anim == null) return;
            anim.ResetTrigger("Open");
            anim.SetTrigger("Open");
        }
        void Update()
        {

        }
    }
}