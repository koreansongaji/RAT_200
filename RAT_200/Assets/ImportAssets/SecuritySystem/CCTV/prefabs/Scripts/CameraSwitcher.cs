using HitSystem;
using UnityEngine;

namespace CameraSwitcherScript
{
    public class CameraSwitcher : MonoBehaviour
    {
        public RayScript RayLink;

        private Renderer renderer;

        [SerializeField] private Texture[] Displays;
        private int RandomNumber;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            renderer = gameObject.GetComponent<Renderer>();
        }

        // Update is called once per frame
        void Update()
        {

            if (RayLink.CameraActive == true)
            {
                if (Input.GetKeyDown(KeyCode.E))
                {
                    RandomNumber = Random.Range(0, Displays.Length);
                    renderer.material.mainTexture = Displays[RandomNumber];
                }
            }
        }
    }
}