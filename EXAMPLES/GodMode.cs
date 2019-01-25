﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using QuizCannersUtilities;
using System;

namespace Playtime_Painter.Examples {
    
    [ExecuteInEditMode]
    public class GodMode : MonoBehaviour, IPEGI {

        public static GodMode inst;
        public float speed = 20;
        public float sensitivity = 5;
        public static bool disableRotation = false;
        public bool rotateWithotRMB;
        public static string PrefSpeed = "GodSpeed";
        public static string PrefSens = "GodSensitivity";
        

        bool Rotate() {

            #if !UNITY_IOS && !UNITY_ANDROID
            return (rotateWithotRMB || Input.GetMouseButton(1));
            #else
            return true;

            #endif
        }

        private void OnEnable() {

            inst = this;
        }

        private void Start()
        {
            inst = this;
        }

        // Update is called once per frame
        protected float rotationY;

        public virtual void Update()
        {

            Vector3 add = Vector3.zero;

            if (Input.GetKey(KeyCode.W)) add += transform.forward;
            if (Input.GetKey(KeyCode.A)) add -= transform.right;
            if (Input.GetKey(KeyCode.S)) add -= transform.forward;
            if (Input.GetKey(KeyCode.D)) add += transform.right;
            add.y = 0;
            if (Input.GetKey(KeyCode.Q)) add += Vector3.down;
            if (Input.GetKey(KeyCode.E)) add += Vector3.up;


            transform.position += add * speed * Time.deltaTime * (Input.GetKey(KeyCode.LeftShift) ? 3f: 1f);

            if ((Application.isPlaying) && (!disableRotation))
            {

                if (rotateWithotRMB || Input.GetMouseButton(1))
                {
                    float rotationX = transform.localEulerAngles.y;
                    rotationY = transform.localEulerAngles.x;



                    rotationX += Input.GetAxis("Mouse X") * sensitivity;
                    rotationY -= Input.GetAxis("Mouse Y") * sensitivity;

                    if (rotationY < 120)
                        rotationY = Mathf.Min(rotationY, 85);
                    else
                        rotationY = Mathf.Max(rotationY, 270);


                    transform.localEulerAngles = new Vector3(rotationY, rotationX, 0);

                }


                SpinAround();
            }
        }
        
        public Vector2 camOrbit = new Vector2();
        public Vector3 SpinCenter;
        protected float OrbitDistance = 0;
        public bool OrbitingFocused;
        public float SpinStartTime = 0;

        public void SpinAround()
        {

            Transform camTr = gameObject.TryGetCameraTransform();
            if (Input.GetMouseButtonDown(2))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    SpinCenter = hit.point;
                }
                else return;
                
                Quaternion before = camTr.transform.rotation;
                camTr.transform.LookAt(SpinCenter);
                Vector3 rot = camTr.transform.rotation.eulerAngles;
                camOrbit.x = rot.y;
                camOrbit.y = rot.x;
                OrbitDistance = (SpinCenter - camTr.transform.position).magnitude;

                camTr.transform.rotation = before;
                OrbitingFocused = false;
                SpinStartTime = Time.time;
            }

            if (Input.GetMouseButtonUp(2))
                OrbitDistance = 0;

            if ((OrbitDistance > 0) && (Input.GetMouseButton(2)))
            {

                camOrbit.x += Input.GetAxis("Mouse X") * 5;
                camOrbit.y -= Input.GetAxis("Mouse Y") * 5;

                if (camOrbit.y <= -360)
                    camOrbit.y += 360;
                if (camOrbit.y >= 360)
                    camOrbit.y -= 360;

                Quaternion rot = Quaternion.Euler(camOrbit.y, camOrbit.x, 0);
                Vector3 campos = rot *
                    (new Vector3(0.0f, 0.0f, -OrbitDistance)) +
                    SpinCenter;

                camTr.position = campos;
                if (!OrbitingFocused)
                {
                    camTr.rotation = MyMath.Lerp_bySpeed(camTr.rotation, rot, 200);
                    if (Quaternion.Angle(camTr.rotation, rot) < 1)
                        OrbitingFocused = true;
                }
                else camTr.rotation = rot;
   
            }
        }

        public virtual void DistantUpdate()
        {
        }

#region Inspector
#if PEGI
        public bool Inspect()
        {
            
            "Speed:".edit("Speed of movement", 50, ref speed).nl();
  
            if ("sensitivity:".edit(60, ref sensitivity).nl())
                PlayerPrefs.SetFloat(PrefSens, sensitivity);
 
            "Rotate without RMB".toggleIcon(ref rotateWithotRMB).nl();

            "WASD - move {0} Q, E - Dwn, Up {0} Shift - faster {0} RMB - look around {0} MMB - Orbit Collider".F(pegi.EnvironmentNL);

            return false;
        }
#endif
#endregion
    }
}