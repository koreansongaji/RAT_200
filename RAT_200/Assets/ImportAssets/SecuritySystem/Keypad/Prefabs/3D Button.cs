
using System;
using System.IO;
using HitSystem;
using KeyPad3DScript;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Button3DScript
{
    public class Button3D: MonoBehaviour
    {
        public Keypad3D Script;
        public UnityEvent OnClick3D;

        string buttonid;
        string NameOld;


         void Start()
        {
            string id = RandomGen3D();
            NameOld = gameObject.name;
            buttonid = id;
        }

        private string RandomGen3D() //Button name will be randomly generated.
        {
                return Path.GetRandomFileName().Replace(" ",".");
        }


        void Update()
        {
            if (Script.ButtonTag == gameObject.name && Script.ScriptActive == true) //Check if object equal to button 
            {
                gameObject.name = buttonid;
                if (Script.ButtonTag == gameObject.name)
                {
                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        OnClick3D.Invoke();
                    }
                }
            }
            else if(Script.ButtonTag != gameObject.name && Script.ScriptActive == false)
            {
                gameObject.name = NameOld;
            }

            if (Script.locked == true)
            {
                gameObject.tag = "Untagged";
            }
        }
    }

}