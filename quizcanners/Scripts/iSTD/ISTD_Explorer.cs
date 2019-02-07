﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace QuizCannersUtilities
{
    #region List Data
    public class List_Data : Abstract_STD, IPEGI {

        const string defaultFolderToSearch = "Assets/";

        public string label = "list";
        public string folderToSearch = defaultFolderToSearch;
        public int inspected = -1;
        public int listSectionStartIndex = 0;
        public bool Inspecting { get { return inspected != -1; } set { if (value == false) inspected = -1; } }
        public bool _keepTypeData;
        public bool allowDelete;
        public bool allowReorder;
        public bool allowCreate;
        public icon icon;
        public icon Icon => inspected == -1 ? icon : icon.Next;
        public UnnullableSTD<ElementData> elementDatas = new UnnullableSTD<ElementData>();
      

        public List<int> GetSelectedElements() {
            var sel = new List<int>();
            foreach (var e in elementDatas)
                if (e.selected) sel.Add(elementDatas.currentEnumerationIndex);
            return sel;
        }

        public bool GetIsSelected(int ind) {
            var el = elementDatas.GetIfExists(ind);
            if (el != null)
                return el.selected;
            return false;
        }

        public void SetIsSelected(int ind, bool value)
        {
            var el = value ? elementDatas[ind] : elementDatas.GetIfExists(ind);
            if (el != null)
                el.selected = value;
        }
        public ElementData this[int i] {
            get { return elementDatas.TryGet(i); }
        }

        #region Inspector
        #if PEGI

        public pegi.SearchData searchData = new pegi.SearchData();

        public void SaveElementDataFrom<T>(List<T> list)
        {
            for (int i = 0; i < list.Count; i++)
                SaveElementDataFrom(list, i);
        }

        public void SaveElementDataFrom<T>(List<T> list, int i)
        {
            var el = list[i];
            if (el != null)
                elementDatas[i].Save(el);
        }

        bool enterElementDatas = false;
        public bool inspectListMeta = false;

        public bool Inspect() {

            pegi.nl();
            if (!enterElementDatas) {
                "Show 'Explore Encoding' Button".toggleIcon(ref ElementData.EnableEnterInspectEncoding).nl();
                "List Label".edit(70, ref label).nl();
                "Keep Type Data".toggleIcon("Will keep unrecognized data when you switch between class types.", ref _keepTypeData).nl();
                "Allow Delete".toggleIcon(ref allowDelete).nl();
                "Allow Reorder".toggleIcon(ref allowReorder).nl();
            }

            if ("Elements".enter(ref enterElementDatas).nl())
                elementDatas.Inspect();

            return false;
        }

        public bool Inspect<T>(List<T> list) where T : UnityEngine.Object {
            bool changed = false;
#if UNITY_EDITOR

            "{0} Folder".F(label).edit(90, ref folderToSearch);
            if (icon.Search.Click("Populate {0} with objects from folder".F(label))) {
                if (folderToSearch.Length > 0)
                {
                    var scrObjs = AssetDatabase.FindAssets("t:Object", new string[] { folderToSearch });
                    foreach (var o in scrObjs)
                    {
                        var ass = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AssetDatabase.GUIDToAssetPath(o));
                        if (ass)
                        {
                            if (list.TryAdd_UObj_ifNew(ass)) continue;
                        }
                    }
                }
            }

            if (list.Count > 0 && list[0] != null && icon.Refresh.Click("Use location of the first element in the list"))
                folderToSearch = AssetDatabase.GetAssetPath(list[0]);

#endif
            return changed;
        }
        #endif
        #endregion

        #region Encode & Decode

        public override bool Decode(string tag, string data)
        {
            switch (tag)
            {
                case "ed": data.DecodeInto(out elementDatas); break;
                case "insp": inspected = data.ToInt(); break;
                case "fld": folderToSearch = data; break;
                case "ktd": _keepTypeData = data.ToBool(); break;
                case "del": allowDelete = data.ToBool(); break;
                case "reord": allowReorder = data.ToBool(); break;
                case "st": listSectionStartIndex = data.ToInt(); break;
#if PEGI
                case "s": searchData.Decode(data); break;
#endif
                default: return false;
            }
            return true;
        }

        public override StdEncoder Encode()
        {
            var cody = new StdEncoder()
                .Add_IfNotNegative("insp", inspected)
                .Add_IfNotZero("st", listSectionStartIndex)
                #if PEGI
                .Add_IfNotDefault("s", searchData)
                #endif
                ;
            
            if (!folderToSearch.SameAs(defaultFolderToSearch))
                cody.Add_String("fld", folderToSearch);

            cody.Add_IfNotDefault("ed", elementDatas);
 
            return cody;
        }

#endregion

        public List_Data() {
            allowDelete = true;
            allowReorder = true;
            allowCreate = true;
            _keepTypeData = false;
        }

        public List_Data(string nameMe, bool allowDeleting = true, bool allowReordering = true, bool keepTypeData = false, bool allowCreating = true, icon enterIcon = icon.Enter)
        {
            allowCreate = allowCreating;
            allowDelete = allowDeleting;
            allowReorder = allowReordering;
            label = nameMe;
            _keepTypeData = keepTypeData;
            icon = enterIcon;
        }
    }

    public class ElementData : Abstract_STD, IPEGI, IGotName {
        public string name;
        public string componentType;
        public string std_dta;
        public string guid;
        public bool unrecognized = false;
        public string unrecognizedUnderTag;
        public bool selected;

        public override bool IsDefault => (unrecognized || !guid.IsNullOrEmpty() || perTypeConfig.Count>0 );

        public static bool EnableEnterInspectEncoding = false;

        public Dictionary<string, string> perTypeConfig = new Dictionary<string, string>();

        public ElementData SetRecognized() {
            if (unrecognized) {
                unrecognized = false;
                unrecognizedUnderTag = null;
                std_dta = null;
            }
            return this;
        }

        public void Unrecognized(string tag, string data) {
            unrecognized = true;
            unrecognizedUnderTag = tag;
            std_dta = data;
        }
        
        public void ChangeType(ref object obj, Type newType, TaggedTypes_STD taggedTypes, bool keepTypeConfig = false)
        {
            var previous = obj;

            var tobj = obj as IGotClassTag;

            if (keepTypeConfig && tobj != null)
                perTypeConfig[tobj.ClassTag] = tobj.Encode().ToString();

            obj = Activator.CreateInstance(newType);

            var std = obj as ISTD;

            if (std != null)
            {
                string data;
                if (perTypeConfig.TryGetValue(taggedTypes.Tag(newType), out data))
                    std.Decode(data);
            }

            STDExtensions.TryCopy_Std_AndOtherData(previous, obj);

        }

        public void Save<T>(T el)
        {
            name = el.ToPEGIstring();

            var cmp = el as Component;
            if (cmp != null)
                componentType = cmp.GetType().ToPEGIstring_Type();

            var std = el as ISTD;
            if (std != null)
                std_dta = std.Encode().ToString();

            guid = (el as UnityEngine.Object).GetGUID(guid);
        }
        
        public bool TryGetByGUID<T>(ref T field) where T : UnityEngine.Object {

            var obj = UnityHelperFunctions.GUIDtoAsset<T>(guid);

            field = null;

            if (obj)
            {
                field = obj;

                if (componentType != null && componentType.Length > 0)
                {
                    var go = obj as GameObject;
                    if (go)
                    {
                        var getScripts = go.GetComponent(componentType) as T;
                        if (getScripts)
                            field = getScripts;
                    }
                }

                return true;
            }

            return false;
        }
        
#region Inspector
#if PEGI
        public string NameForPEGI { get { return name; } set { name = value; } }

        public bool Inspect()
        {
            bool changed = false;

            if (unrecognized)
                "Was unrecognized under tag {0}".F(unrecognizedUnderTag).writeWarning();

            if (perTypeConfig.Count > 0)
                "Per type config".edit_Dictionary_Values(ref perTypeConfig, pegi.lambda_string).nl();

            return changed;
        }

        public bool SelectType<T>(ref object obj, bool keepTypeConfig = false) {
            bool changed = false;

            var all = typeof(T).TryGetTaggetClasses();

            if (all == null) {
                "No Types Holder".writeWarning();
                return false;
            }

           // var previous = obj;

            var type = obj?.GetType();

            if (all.Select(ref type).nl()) {

                ChangeType(ref obj, type, all, keepTypeConfig);


                /* if (keepTypeConfig && tobj != null)
                     perTypeConfig[tobj.ClassTag] = tobj.Encode().ToString();

                 string data;

                 if (!perTypeConfig.TryGetValue(all.Tag(type), out data) && tobj != null)
                         data = tobj.Encode().ToString();

                 obj = data.TryDecodeInto_Type<T>(type);*/


            }

            return changed;
        }

        public bool PEGI_inList<T>(ref object obj, int ind, ref int edited) {

            bool changed = false;

            if (typeof(T).IsUnityObject()) {

                var uo = obj as UnityEngine.Object;
                if (PEGI_inList_Obj(ref uo).changes(ref changed))
                    obj = uo;

            } else {

                if (unrecognized)
                    unrecognizedUnderTag.write("Type Tag {0} was unrecognized during decoding".F(unrecognizedUnderTag), 40);

                if (!name.IsNullOrEmpty())
                    name.write();

                if (typeof(T) is IGotClassTag)
                    SelectType<T>(ref obj);

            }

            return changed;
        }

        public bool PEGI_inList_Obj<T>(ref T field) where T : UnityEngine.Object {

            if (unrecognized)
                unrecognizedUnderTag.write("Type Tag {0} was unrecognized during decoding".F(unrecognizedUnderTag), 40);

            bool changed = name.edit(100, ref field);

#if UNITY_EDITOR
            if (guid != null && icon.Search.Click("Find Object " + componentType + " by guid").nl()) {

                if (!TryGetByGUID(ref field))
                    (typeof(T).ToString() + " Not found ").showNotificationIn3D_Views();
                else changed = true;
            }
#endif

            return changed;
        }
#endif
#endregion

#region Encode & Decode
        public override bool Decode(string tag, string data) {
            switch (tag) {
                case "n": name = data; break;
                case "std": std_dta = data; break;
                case "guid": guid = data; break;
                case "t": componentType = data; break;
                case "ur": unrecognized = data.ToBool(); break;
                case "tag": unrecognizedUnderTag = data; break;
                case "perType": data.Decode_Dictionary(out perTypeConfig); break;
                case "sel": selected = data.ToBool(); break;
                default: return false;
            }
            return true;
        }

        public override StdEncoder Encode() {
            var cody = new StdEncoder()
                .Add_IfNotEmpty("n", name)
                .Add_IfNotEmpty("std", std_dta);

            if (!guid.IsNullOrEmpty()) {
                cody.Add_IfNotEmpty("guid", guid)
                    .Add_IfNotEmpty("t", componentType);
            }

            cody.Add_IfNotEmpty("perType", perTypeConfig)
                .Add_IfTrue("sel", selected);

            if (unrecognized) {
                cody.Add_Bool("ur", unrecognized)
                .Add_String("tag", unrecognizedUnderTag);
            }
            return cody;
        }

#endregion
    }

    public static class STDListDataExtensions {

        public static ElementData TryGetElement(this List_Data ld, int ind) => ld?.elementDatas.TryGet(ind);

    }

#endregion

#region Saved STD
[Serializable]
    public class Exploring_STD : Abstract_STD, IPEGI, IGotName, IPEGI_ListInspect, IGotCount
    {
        ISTD Std { get { return ISTD_ExplorerData.inspectedSTD; } }

        public string tag;
        public string data;
        public bool dirty = false;

        public void UpdateData()
        {
            if (tags != null)
                foreach (var t in tags)
                    t.UpdateData();

            dirty = false;
            if (tags != null)
                data = Encode().ToString();
        }

        public int inspectedTag = -1;
        [NonSerialized]
        public List<Exploring_STD> tags;

        public Exploring_STD() { tag = ""; data = ""; }

        public Exploring_STD(string ntag, string ndata) {
            tag = ntag;
            data = ndata;
        }

#region Inspector
#if PEGI

        public int CountForInspector => tags.IsNullOrEmpty() ? data.Length : tags.CountForInspector();
        
        public string NameForPEGI  {
            get  {  return tag; }
            set {  tag = value; }
        }
        
        public bool Inspect()
        {
            if (tags == null && data.Contains("|"))
                Decode(data);//.DecodeTagsFor(this);

            if (tags != null)
                tag.edit_List(ref tags, ref inspectedTag).changes(ref dirty);

            if (inspectedTag == -1)
            {
                "data".edit(40, ref data).nl(ref dirty);

                UnityEngine.Object myType = null;

                if (pegi.edit(ref myType))
                {
                    dirty = true;
                    data = StuffLoader.LoadTextAsset(myType);
                }

                if (dirty)
                {
                    if (icon.Refresh.Click("Update data string from tags"))
                        UpdateData();

                    if (icon.Load.Click("Load from data String").nl())
                    {
                        tags = null;
                        Decode(data);//.DecodeTagsFor(this);
                        dirty = false;
                    }
                }
            }


            pegi.nl();

            return dirty;
        }

        public bool PEGI_inList(IList list, int ind, ref int edited)
        {

            bool changed = false;

            CountForInspector.ToString().write(50);

            if (data != null && data.Contains("|"))
            {
                changed |= pegi.edit(ref tag);

                if (icon.Enter.Click("Explore data"))
                    edited = ind;
            }
            else
            {
                dirty |= pegi.edit(ref tag);
                dirty |= pegi.edit(ref data);
            }

            if (icon.Copy.Click("Copy " + tag + " data to buffer."))
            {
                STDExtensions.copyBufferValue = data;
                STDExtensions.copyBufferTag = tag;
            }

            if (STDExtensions.copyBufferValue != null && icon.Paste.Click("Paste " + STDExtensions.copyBufferTag + " Data").nl())
            {
                dirty = true;
                data = STDExtensions.copyBufferValue;
            }

            return dirty | changed;
        }

#endif
#endregion

#region Encode & Decode
        public override StdEncoder Encode()
        {
            var cody = new StdEncoder();

            if (tags != null)
                foreach (var t in tags)
                    cody.Add_String(t.tag, t.data);

            return cody;

        }

        public override bool Decode(string tag, string data)
        {
            if (tags == null)
                tags = new List<Exploring_STD>();
            tags.Add(new Exploring_STD(tag, data));
            return true;
        }
#endregion

    }

    [Serializable]
    public class SavedISTD : IPEGI, IGotName, IPEGI_ListInspect, IGotCount {

        ISTD Std => ISTD_ExplorerData.inspectedSTD;

        public string comment;
        public Exploring_STD dataExplorer = new Exploring_STD("", "");

#region Inspector
        public string NameForPEGI { get { return dataExplorer.tag; } set { dataExplorer.tag = value; } }

#if PEGI
        public static ISTD_ExplorerData Mgmt => ISTD_ExplorerData.inspected;

        public int CountForInspector => dataExplorer.CountForInspector;

        public bool Inspect()
        {
            bool changed = false;


            if (dataExplorer.inspectedTag == -1)
            {
                this.inspect_Name();
                if (Std != null && dataExplorer.tag.Length > 0 && icon.Save.Click("Save To Assets"))
                {
                    StuffSaver.Save_ToAssets_ByRelativePath(Mgmt.fileFolderHolder, dataExplorer.tag, dataExplorer.data);
                    UnityHelperFunctions.RefreshAssetDatabase();
                }

                pegi.nl();

                if (Std != null)
                {
                    if (dataExplorer.tag.Length == 0)
                        dataExplorer.tag = Std.ToPEGIstring() + " config";

                    "Save To:".edit(50, ref Mgmt.fileFolderHolder);

                    var uobj = Std as UnityEngine.Object;

                    if (uobj && icon.Done.Click("Use the same directory as current object."))
                        Mgmt.fileFolderHolder = uobj.GetAssetFolder();

                    uobj.ClickHighlight();

                    pegi.nl();
                }


                "Comment:".editBig(ref comment).nl();
            }

            dataExplorer.Nested_Inspect();

            return changed;
        }

        public bool PEGI_inList(IList list, int ind, ref int edited)
        {
            bool changed = false;

            CountForInspector.ToString().edit(60, ref dataExplorer.tag);

            if (Std != null)
            {
                if (icon.Load.ClickUnfocus("Decode Data into " + Std.ToPEGIstring())) {
                    dataExplorer.UpdateData();
                    Std.Decode(dataExplorer.data);
                }
                if (icon.Save.ClickUnfocus("Save data from " + Std.ToPEGIstring()))
                    dataExplorer = new Exploring_STD(dataExplorer.tag, Std.Encode().ToString());
            }

            if (icon.Enter.Click(comment))
                edited = ind;

            return changed;
        }
#endif
#endregion
    }

    [Serializable]
    public class ISTD_ExplorerData : IGotCount
    {
        public List<SavedISTD> states = new List<SavedISTD>();
        [NonSerialized]
        public int inspectedState = -1;
        public string fileFolderHolder = "STDEncodes";
        public static ISTD inspectedSTD;

#region Inspector
#if PEGI
        public int CountForInspector => states.Count;
        
        public static bool PEGI_Static(ISTD target)
        {
            inspectedSTD = target;

            bool changed = false;
            pegi.write("Load File:", 90);
            target.LoadOnDrop().nl();

            if (icon.Copy.Click("Copy Component Data"))
                STDExtensions.copyBufferValue = target.Encode().ToString();

            pegi.nl();

            return changed;
        }

        public static ISTD_ExplorerData inspected;

        public bool Inspect(ISTD target)
        {
            bool changed = false;
            inspectedSTD = target;
            inspected = this;

            var aded = "Saved CFGs:".edit_List(ref states, ref inspectedState, ref changed);

            if (aded != null && target != null)
            {
                aded.dataExplorer.data = target.Encode().ToString();
                aded.NameForPEGI = target.ToPEGIstring();
                aded.comment = DateTime.Now.ToString();
            }

            if (inspectedState == -1)
            {
                UnityEngine.Object myType = null;
                if ("From File:".edit(65, ref myType))
                {
                    aded = new SavedISTD();
                    aded.dataExplorer.data = StuffLoader.LoadTextAsset(myType);
                    aded.NameForPEGI = myType.name;
                    aded.comment = DateTime.Now.ToString();
                    states.Add(aded);
                }

                var selfSTD = target as IKeepMySTD;

                if (selfSTD != null)
                {
                    if (icon.Save.Click("Save itself (IKeepMySTD)"))
                        selfSTD.Save_STDdata();
                    var slfData = selfSTD.Config_STD;
                    if (slfData != null && slfData.Length > 0) {

                        if (icon.Load.Click("Use IKeepMySTD data to create new CFG")) {
                            var ss = new SavedISTD();
                            states.Add(ss);
                            ss.dataExplorer.data = slfData;
                            ss.NameForPEGI = "from Keep my STD";
                            ss.comment = DateTime.Now.ToString();
                        }

                      if (icon.Refresh.Click("Load from itself (IKeepMySTD)"))
                        target.Decode(slfData);
                    }
                }
                pegi.nl();
            }

            inspectedSTD = null;

            return changed;
        }
#endif
#endregion
    }
#endregion
}