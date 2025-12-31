using UnityEngine;
using System;
using System.Collections;
using HitSystem;
using TMPro;
using Unity.Mathematics;

namespace KeyPad3DScript
{
   public class Keypad3D : MonoBehaviour
   {

      #region Properties

      [Tooltip("Maximum amount is 5 numbers")][SerializeField] string Code;
      [SerializeField] Behaviour Task;

      [Header("Other")]
      [Space(5)]
      [SerializeField] TextMeshPro InputField;
      [SerializeField] public RayScript RayLink;
      [SerializeField] private AudioSource ClickSound;
      [NonSerialized] public string ButtonTag;
      [NonSerialized] public bool locked;
      [NonSerialized] public bool ScriptActive;


      #endregion

      #region Methods

      void Start()
      {
         if (Code.Length > 5)
         {
            Code = "00000";
         }

         Task.enabled = false;
         locked = false;

      }

      public void NumberInput3D(int number) //Input for number
      {
         if (locked != true)
         {
            if (InputField.text == "wrong")
            {
               InputField.text = InputField.text;
            }
            else if (InputField.text.Length >= 5)
            {
               InputField.text = InputField.text;
            }
            else
            {
               InputField.text += number.ToString();
               ClickSound.Play();
            }
         }
      }

      public void Clear()
      {
         if (InputField.text != "wrong")
         {
            InputField.text = "";
            ClickSound.Play();
         }
      }

      public void Check()
      {

         if (InputField.text == Code.ToString())
         {
            InputField.text = "enter!";
            ClickSound.Play();

            locked = true;
            StartCoroutine(Correct());
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
      }

      //Time effect

      IEnumerator Reset()
      {
         yield return new WaitForSeconds(0.5f);
         InputField.text = "";
      }

      IEnumerator Correct()
      {
         yield return new WaitForSeconds(0.35f);

         // Task.enabled 토글 대신 명시적 호출
         var mb = Task as MonoBehaviour;
         if (mb != null)
         {
             var tap = mb.GetComponent<Task1.TaskAnimationPlay>();
             if (tap != null) {
                 tap.PlayOpen();

                 // 공용 퍼즐 성공 사운드
                 CommonSoundController.Instance?.PlayPuzzleSuccess();
             }
         }

         StartCoroutine(Reset());
      }

      #endregion

      void Update()
      {
         if (RayLink.Active3D == true)
         {
            ScriptActive = true;
            ButtonTag = RayLink.HitTag;
         }
         else
         {
            ScriptActive = false;
            ButtonTag = "";
         }
      }

   }
}
