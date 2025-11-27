using UnityEngine;

namespace ZakhanPolygonalWater
{
    public class Demo : MonoBehaviour {
   
        public Camera MainCamera;
        public Transform Center;
        public float Speed = -0.07f;

        void Update()
        {
            MainCamera.transform.RotateAround(Center.position, transform.up * Time.deltaTime, -0.07f);
        }
    }
}
