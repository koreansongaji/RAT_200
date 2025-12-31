using System;
using UnityEngine;

namespace FirstPersonControllerEx
{
    public class FirstPersonControllerExample : MonoBehaviour
    {
        public Camera playerCamera;
        public float walkSpeed = 6f;
        float lookSpeed = 2f;
        float lookXLimit = 45f;


        Vector3 moveDirection = Vector3.zero;
        float rotationX = 0;

        [NonSerialized]public bool canMove = true;


        CharacterController characterController;
        void Start()
        {
            characterController = GetComponent<CharacterController>();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void Update()
        {

            Vector3 forward = transform.TransformDirection(Vector3.forward);
            Vector3 right = transform.TransformDirection(Vector3.right);

            float curSpeedX = canMove ? (walkSpeed) * Input.GetAxis("Vertical") : 0;
            float curSpeedY = canMove ? (walkSpeed) * Input.GetAxis("Horizontal") : 0;
            float movementDirectionY = moveDirection.y;

            moveDirection = (forward * curSpeedX) + (right * curSpeedY);
            characterController.Move(moveDirection * Time.deltaTime);

            if (canMove)
            {
                rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
                rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
                playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
                transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0);
            }

        }
    }
}
