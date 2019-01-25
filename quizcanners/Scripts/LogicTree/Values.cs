﻿using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using PlayerAndEditorGUI;
using QuizCannersUtilities;

namespace STD_Logic {


    public class Values : AbstractKeepUnrecognized_STD, IPEGI, IGotCount
    {

        public static Values global = new Values();

        public UnnullableSTD<CountlessBool> bools = new UnnullableSTD<CountlessBool>();
        public UnnullableSTD<CountlessInt> ints = new UnnullableSTD<CountlessInt>();
        UnnullableSTD<CountlessInt> enumTags = new UnnullableSTD<CountlessInt>();
        UnnullableSTD<CountlessBool> boolTags = new UnnullableSTD<CountlessBool>();

        #region Encode & Decode

        public override StdEncoder Encode() => this.EncodeUnrecognized()
            .Add_IfNotDefault("ints", ints)
            .Add_IfNotDefault("bools", bools)
            .Add_IfNotDefault("tags", boolTags)
            .Add_IfNotDefault("enumTags", enumTags);
           
        public override bool Decode(string tag, string data) {
            switch (tag) {
                case "ints": data.DecodeInto(out ints); break;
                case "bools": data.DecodeInto(out bools); break;
                case "tags": data.DecodeInto(out boolTags); break;
                case "enumTags": data.DecodeInto(out enumTags); break;
                default: return false;
            }
            return true;
        }

        public override void Decode(string data) {

            bools = new UnnullableSTD<CountlessBool>();
            ints = new UnnullableSTD<CountlessInt>();
            enumTags = new UnnullableSTD<CountlessInt>();
            boolTags = new UnnullableSTD<CountlessBool>();

            base.Decode(data);
        }

        #endregion

        #region Get/Set
        public bool GetTagBool(ValueIndex ind) => boolTags.Get(ind);

        public int GetTagEnum(ValueIndex ind) => enumTags.Get(ind);

        public void SetTagBool(ValueIndex ind, bool value) => SetTagBool(ind.groupIndex, ind.triggerIndex, value);

        public void SetTagBool(TriggerGroup gr, int tagIndex, bool value) => SetTagBool(gr.IndexForPEGI , tagIndex, value);

        public void SetTagBool(int groupIndex, int tagIndex, bool value) {

            boolTags[groupIndex][tagIndex] = value;

            TriggerGroup s = TriggerGroup.all[groupIndex];

            if (s.taggedBool[tagIndex].Contains(this))
            {
                if (value)
                    return;
                else
                    s.taggedBool[tagIndex].Remove(this);

            }
            else if (value)
                s.taggedBool[tagIndex].Add(this);
        }

        public void SetTagEnum(TriggerGroup gr, int tagIndex, int value) => SetTagEnum(gr.IndexForPEGI, tagIndex, value);

        public void SetTagEnum(ValueIndex ind, int value) => SetTagEnum(ind.groupIndex, ind.triggerIndex, value);

        public void SetTagEnum(int groupIndex, int tagIndex, int value) {

            enumTags[groupIndex][tagIndex] = value;

            TriggerGroup s = TriggerGroup.all[groupIndex];

            if (s.taggedInts[tagIndex][value].Contains(this)) {
                if (value != 0)
                    return;
                else
                    s.taggedInts[tagIndex][value].Remove(this);

            }
            else if (value != 0)
                s.taggedInts[tagIndex][value].Add(this);
        }
        #endregion

        public void Clear()
        {
            ints.Clear();
            bools.Clear();
            RemoveAllTags();
          
        }

        public void RemoveAllTags() {
            List<int> groupInds;
            List<CountlessBool> lsts = boolTags.GetAllObjs(out groupInds);
            //Stories.all.GetAllObjs(out inds);

            for (int i = 0; i < groupInds.Count; i++)
            {
                CountlessBool vb = lsts[i];
                List<int> tag = vb.GetItAll();

                foreach (int t in tag)
                    SetTagBool(groupInds[i], t, false);

            }


            enumTags.Clear();
            boolTags.Clear(); // = new UnnullableSTD<CountlessBool>();
        }

        #region Inspector

        public int CountForInspector => bools.CountForInspector + ints.CountForInspector + enumTags.CountForInspector + boolTags.CountForInspector; 

#if PEGI

        public static Values inspected;


        public override bool Inspect() {
            
            bool changed = false;

            inspected = this;

            if (icon.Next.Click("Add 1 to logic version (to update all the logics)").nl())
                    LogicMGMT.AddLogicVersion();

            foreach (var bGr in bools) {
                var group = TriggerGroup.all[bools.currentEnumerationIndex];
                foreach (var b in bGr)
                    group[b].Inspect_AsInList().nl(ref changed);
            }

            foreach (var iGr in ints) {
                var group = TriggerGroup.all[ints.currentEnumerationIndex];
                foreach (var i in iGr) 
                   group[iGr.currentEnumerationIndex].Inspect_AsInList().nl(ref changed);
                
            }

            inspected = null;

            return changed;
        }
        #endif
        #endregion
    }


   

}