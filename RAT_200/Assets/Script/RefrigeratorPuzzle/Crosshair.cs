
using System;
using UnityEngine;

namespace HitSystem
{
        public class RayScript : MonoBehaviour
    {
        [SerializeField] Transform RaySource;
        [SerializeField] public GameObject CrosshairImage;
        [SerializeField] public float rayrange = 10f;
        [SerializeField] Camera targetCamera; // 화면상의 마우스 위치에서 레이를 쏠 카메라
        
        [NonSerialized] public bool openUI;
        [NonSerialized] float hitrange;
        [NonSerialized] public string HitTag;
        [NonSerialized] public bool Active2D;
        [NonSerialized] public bool Active3D;

        [NonSerialized] public bool CameraActive;


        void Start()
        {
            CrosshairImage.SetActive(false);
            if (targetCamera == null) targetCamera = Camera.main; // 카메라가 비면 메인 카메라 사용
        }


        void Update()
        {
            // 마우스 위치에서 화면 밖으로 나가는 레이
            Ray R = targetCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(R, out hit, rayrange)) //When raycast decects collider 
            {

                if (hit.collider.gameObject.tag == "Keypad2D") //if detected object have this tag it will be 2D
                {
                    if (openUI == false)
                    {
                        Active2D = true;
                        HitTag = hit.collider.gameObject.name;
                    }
                    else
                    {
                        Active2D = false;
                    }
                }
                else if (hit.collider.gameObject.tag == "Keypad3D") //if detected object have this tag it will be 3D
                {
                    Active3D = true;
                    HitTag = hit.collider.gameObject.name;
                }
                else if (hit.collider.gameObject.tag == "Display") //if detected object have this tag it will be 3D
                {
                    CameraActive = true;
                }
                else
                {
                    Active2D = false;
                    Active3D = false;
                    CameraActive = false;
                }

                if (hit.distance >= rayrange - 0.1f) // 0.1 because with this we can disable ray from range we set.
                {
                    Active2D = false;
                    Active3D = false;
                    CameraActive = false;
                    CrosshairImage.SetActive(false);
                }

                hitrange = hit.distance;
            }
            else
            {
                Active2D = false;
                Active3D = false;
                CameraActive = false;
            }

            if (Active2D == true || Active3D == true || CameraActive == true) //crosshair will be displayed only if its true
            {
                CrosshairImage.SetActive(true);
            }
            else if (Active2D == false || Active3D == false || CameraActive == false)
            {
                CrosshairImage.SetActive(false);
            }

            if (CameraActive == true)
            {
                Debug.Log("Camera!");
            }

        }
    }

}
