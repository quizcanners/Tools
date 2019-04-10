﻿using UnityEngine;
using PlayerAndEditorGUI;
using QuizCannersUtilities;
using System.Collections;

namespace Playtime_Painter
{

    [ExecuteInEditMode]
    public class LightCaster : MonoBehaviour, IPEGI , IGotIndex, IGotName, IPEGI_ListInspect {

        public static readonly Countless<LightCaster> AllProbes = new Countless<LightCaster>();
        private static int freeIndex;

        public ProjectorCameraConfiguration cameraConfiguration;

        public ProjectorCameraConfiguration UpdateAndGetCameraConfiguration() {
            cameraConfiguration.CopyTransform(transform);

            if (camShake > 0)
            {


                cameraConfiguration.rotation = Quaternion.Lerp(cameraConfiguration.rotation, Random.rotation, camShake);
            }

            return cameraConfiguration;

        }

        public Color ecol = Color.yellow;
        public float brightness = 1;
        public float camShake = 0.0001f;

        public int index;

        public int IndexForPEGI { get { return index;  } set { index = value; } }
        public string NameForPEGI { get { return gameObject.name; } set { gameObject.name = value; } }

        private void OnEnable() {

            if (cameraConfiguration == null)
                cameraConfiguration = new ProjectorCameraConfiguration();

            if (AllProbes[index]) {
                while (AllProbes[freeIndex]) freeIndex++;
                index = freeIndex;
            }

            AllProbes[index] = this;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = ecol;
            Gizmos.DrawWireSphere(transform.position, 1);

            //

            Gizmos.matrix = transform.localToWorldMatrix;

            cameraConfiguration.DrawFrustrum(transform.localToWorldMatrix);

          

        }

        private void OnDisable() {
            if (AllProbes[index] == this)
                AllProbes[index] = null;
        }

        private void ChangeIndexTo (int newIndex) {
            if (AllProbes[index] == this)
                AllProbes[index] = null;
            index = newIndex;

            if (AllProbes[index])
                Debug.Log("More then one probe is sharing index {0}".F(index));

            AllProbes[index] = this;
        }

        #if PEGI

        private int inspectedElement = -1;

        public bool Inspect()
        {
            var changed = false;

            if (inspectedElement == -1)
            {

                var tmp = index;
                if ("Index".edit(ref tmp).nl(ref changed))
                    ChangeIndexTo(tmp);

                "Shake".edit(50, ref camShake, 0, 0.001f).nl();

                "Emission Color".edit(ref ecol).nl(ref changed);
                "Brightness".edit(ref brightness).nl(ref changed);
            }

            if ("Projection".enter(ref inspectedElement, 1).nl())
            {
                cameraConfiguration.Nested_Inspect().nl(ref changed);
            }

            if (changed) UnityUtils.RepaintViews();

            return changed;
        }

        public bool PEGI_inList(IList list, int ind, ref int edited)
        {
            var changed = false;
           index.ToString().write(25);
           pegi.edit(ref ecol, 40).changes(ref changed);
               //   pegi.edit(ref brightness, 0, 10).changes(ref changed);

           if (icon.Enter.Click("Inspect"))
               edited = ind;

           return changed;
        }
#endif

    }
}