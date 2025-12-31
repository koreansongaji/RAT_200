using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using HitSystem;
using FirstPersonControllerEx;


namespace KeyPad2DScript
{

   public class Keypad2D : MonoBehaviour
   {

      #region Properties
      [Tooltip("Maximum amount is 5 numbers")] [SerializeField] private string Code = "00000";
      [SerializeField] public Behaviour Task;

      [Header("UI Settings")]
      [Space(5)]
      [SerializeField] GameObject KeypadUI;
      [SerializeField] Text InputField;

      [Header("Other")]
      [Space(5)]
      [SerializeField] public GameObject PlayerLink;
      [SerializeField] public RayScript RayLink;
      [SerializeField] private AudioSource ClickSound;
      [Header("Effect")]
      [SerializeField] GameObject BackgroundTransition;

      private string Keypadid;
      private string NameOld;
      bool Opened;
      bool Pressed = false;


      bool allowed;

      #endregion

      #region Methods

      void Start()
      {
         if (Code.Length > 5)
         {
            Code = "00000";
         }

         Task.enabled = false;
         KeypadUI.SetActive(false);

         Keypadid = RandomGen2D();
         NameOld = gameObject.name;
         BackgroundTransition.SetActive(false);

      }

      private string RandomGen2D()
      {
         return Path.GetRandomFileName().Replace(" ", ".");
      }

      public void OpenUI() //Method for Opening Keypad UI
      {
         KeypadUI.SetActive(true);
         RayLink.openUI = true;

         PlayerLink.GetComponent<FirstPersonControllerExample>().enabled = false;
         Cursor.lockState = CursorLockMode.None;
         Cursor.visible = true;

         Pressed = true;
         BackgroundTransition.SetActive(true);
         BackgroundTransition.GetComponent<Animator>().Play("Panel Transition");
      }

      public void CloseUI() //Method for Closing Keypad UI
      {
         KeypadUI.SetActive(false);
         RayLink.openUI = false;

         PlayerLink.GetComponent<FirstPersonControllerExample>().enabled = true;
         Cursor.lockState = CursorLockMode.Locked;
         Cursor.visible = false;


         InputField.text = "";
         BackgroundTransition.SetActive(false);
      }

      public void NumberInput(int number) //Method for adding number to input field
      {
         if (InputField.text == "wrong")
         {
            InputField.text = InputField.text;
         }
         else if (InputField.text.Length >= 5)
         {
            InputField.text = InputField.text;
         }
         else if(InputField.text != "wrong")
         {
            InputField.text += number.ToString();
            ClickSound.Play();
         }
      }

      public void Clear() //Method for clearig input field
      {
         if (InputField.text != "wrong")
         {
            InputField.text = "";
            ClickSound.Play();
         }
      }

      public void Check() //Check if player input is equal to code. If true task is enabled.
      {

         if (InputField.text == Code.ToString())
         {
            InputField.text = "enter!";
            ClickSound.Play();
            allowed = true;
         }
         else
         {
            if (InputField.text != "")
            {
               InputField.text = "wrong";
               ClickSound.Play();

               // 공용 퍼즐 실패 사운드
               CommonSoundController.Instance?.PlayPuzzleFail();

               StartCoroutine(Reset());
            }
         }

         if (allowed == true)
         {
            StartCoroutine(Correct());
         }
      }

      //Some timing effects
      IEnumerator Correct()
      {
         yield return new WaitForSeconds(0.25f);

         CloseUI();
         gameObject.tag = "Untagged";
         Task.enabled = true;
         allowed = false;

         // 공용 퍼즐 성공 사운드
         CommonSoundController.Instance?.PlayPuzzleSuccess();
      }
      IEnumerator Reset()
      {
         yield return new WaitForSeconds(0.5f);
         InputField.text = "";
      }

      #endregion

      #region Body

      void Update() //If Ray hit gameobject then you can use keypad
      {
         if (RayLink.HitTag == gameObject.name)
         {
            gameObject.name = Keypadid;

            if (RayLink.HitTag == gameObject.name)
            {
               if (Input.GetKeyDown(KeyCode.E))
               {
                  if (Opened == false && RayLink.Active2D == true)
                  {
                     OpenUI();
                     Opened = true;
                  }

                  else if (Opened == true && Pressed == true)
                  {
                     CloseUI();
                     Opened = false;
                     Pressed = false;
                  }
               }
            }
         }
         else
         {
            gameObject.name = NameOld;
         }


      }

      #endregion
   }

}
   


