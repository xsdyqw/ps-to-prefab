using PhotoshopFile;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace QTool.Psd2Ui
{
    public interface IKey
    {
        public string Key { get; set; }
    }
    [System.Serializable]
    public class FontRef :IKey
    {
        public string Key { get => key; set => key = value; }
        public string key;
        [UnityEngine.Serialization.FormerlySerializedAs("obj")]
        public Font font;
        [Range(-0.5f,0.5f)]
        public float YOffset;
    }
    [System.Serializable]
    public class Prefab : IKey
    {
        public string Key { get => key; set => key = value; }
        public string key;
        [UnityEngine.Serialization.FormerlySerializedAs("obj")]
        public GameObject prefab;
    }
    public class UiImportSetting : BaseUiImportSetting
    {
        public BaseUiImportSetting parentSetting;
        [HideInInspector]
        [SerializeField]
        private UnityEngine.Object psdFile;
      
        public float UiSizeScale
        {
            get
            {
                return uiSizeScale * (parentSetting == null ? 1 : parentSetting.uiSizeScale);
            }
        }
        public float TextScale
        {
            get
            {
                return textScale * (parentSetting == null ? 1 : parentSetting.textScale);
            }
        }
        public float AutoanchoredRate
        {
            get
            {
                return autoanchoredRate * (parentSetting == null ? 1 : parentSetting.autoanchoredRate);
            }
        }
        [HideInInspector] 
        public List<Prefab> basePrefabList = new List<Prefab>();


        public Action SavePrefabAction;
        public Action LoadSpriteAction;
        public Action LoadPrefabAction;
        public string AssetPath
        {
            get
            {
                return AssetDatabase.GetAssetPath(psdFile);
            }
        }
        public string RootPath
        {
            get
            {
                return Path.Combine(Path.GetDirectoryName(AssetPath), name);
            }
        }
        public string ResourcesPath
        {
            get
            {
                return Path.Combine(RootPath, "BaseResources");
            }
        }
        
       
        public void Init(UnityEngine.Object psdFile)
        {
            this.psdFile = psdFile;
            prefabList.Clear();
            var psd = new PsdFile(AssetPath, new LoadContext { Encoding = System.Text.Encoding.Default });
            foreach (var layer in psd.Layers)
            {
                var name = layer.TrueName();
                if (layer.Name.Contains("=prefab"))
                {
                    prefabList.CheckGet(layer.TrueName(),parentSetting?.prefabList);
                }else if (layer.Name.Contains("=&prefab"))
                {
                    prefabList.CheckGet(layer.TrueName(),parentSetting?.prefabList);
                }
                else if (layer.Name.Contains("=text["))
                {
                    var startIndex = layer.Name.IndexOf("=text[") + "=text[".Length;
                    var endIndex = layer.Name.IndexOf("]");
                    var infos = layer.Name.Substring(startIndex, endIndex - startIndex).Split('|');
                    var font = infos[1];
                    fontList.CheckGet( font,parentSetting?.fontList );
                }
            }
        }
        [ContextMenu("生成UI预制体")]
        public void CreateUIPrefab()
        {
            if (!Directory.Exists(RootPath))
            {
                Directory.CreateDirectory(RootPath);
            }
            if (!Directory.Exists(ResourcesPath))
            {
                Directory.CreateDirectory(ResourcesPath);
            }
            var root = this.CreateUIPrefabRoot();
            for (int i = 0; i < root.childCount; i++)
            {
                var ui = root.GetChild(i) as RectTransform;
                if (ui.childCount > 0)
                {
                    this.SaveAsPrefab(ui);
                }

            }
            GameObject.DestroyImmediate(root.gameObject);

        }

    }
    #region 拓展 

    public static class UiPsdImporterExtends
    {
        public static T CheckGet<T>(this List<T> objList, string key, List<T> parentList = null) where T : IKey,new() 
        {
            foreach (var kv in objList)
            {
                if (kv.Key.Equals(key))
                {
                    return kv;
                }
            }
            if (parentList != null)
            {
                foreach (var kv in parentList)
                {
                    if (kv.Key.Equals(key))
                    {
                        return kv;
                    }
                }
            }
            var obj = new T { Key = key };
            objList.Add(obj);
            return obj;
        }
        public static string SaveName(this Layer layer)
        {
            var name = layer.TrueName();
            if (name.Contains('+'))
            {
                name.Replace('+', '_');
            }
            if (name.Contains('<') || name.Contains('>'))
            {
                name = name.Replace('<', '_').Replace('>', '_');
            }
            name += layer.LayerID;
            return name;
        }
        public static LayerSectionInfo GetGroupInfo(this Layer layer)
        {
            return layer.GetInfo("lsct") as LayerSectionInfo;
        }
        public static RawLayerInfo GetTextInfo(this Layer layer)
        {
            return layer.GetInfo("TySh") as RawLayerInfo;
        }
        public static LayerInfo GetInfo(this Layer layer, string key)
        {
            foreach (var info in layer.AdditionalInfo)
            {
                if (info.Key.Equals(key))
                {
                    return info;
                }
            }
            return null;
        }


        public static RectTransform CreateUIPrefabRoot(this UiImportSetting psdUi)
        {
            psdUi.LoadSpriteAction = null;
            psdUi.SavePrefabAction = null;
            psdUi.LoadPrefabAction = null;
            Stack<RectTransform> groupStack = new Stack<RectTransform>(); ;
            var psd = new PsdFile(psdUi.AssetPath, new LoadContext { Encoding = System.Text.Encoding.Default });
            var name = Path.GetFileNameWithoutExtension(psdUi.AssetPath);
            var root = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            root.sizeDelta = new Vector2(psd.ColumnCount, psd.RowCount)*psdUi.UiSizeScale;
            foreach (var layer in psd.Layers)
            {
                //var str = "";
                //foreach (var item in layer.AdditionalInfo)
                //{
                //    //  if (!(item is RawLayerInfo)) continue;
                //    str += item.GetType().Name + "  " + item.Key + "  ";
                //}
                var parentUi = groupStack.Count > 0 ? groupStack.Peek() : root;
                //  str += "\t\t" + layer.Name + ":";
                // Debug.Log(str);
                var textInfo = layer.GetTextInfo();
                if (textInfo != null)
                {

                    psdUi.CreateText(layer, parentUi);

                    // assetPath.CreateUI(layer, parentUi);
                }
                else
                {
                    var groupInfo = layer.GetGroupInfo();
                    if (groupInfo != null)
                    {
                        switch (groupInfo.SectionType)
                        {
                            case LayerSectionType.OpenFolder:
                            case LayerSectionType.ClosedFolder:
                                {
                                    //层级开启标志
                                    var groupUI = groupStack.Pop();
                                    Bounds bounds = new Bounds();
                                    var childList = new List<RectTransform>();

                                    for (int i = groupUI.childCount - 1; i >= 0; i--)
                                    {
                                        var child = groupUI.GetChild(0) as RectTransform;
                                        if (bounds.center == Vector3.zero && bounds.size == Vector3.zero)
                                        {
                                            bounds = new Bounds(child.position, Vector3.zero);
                                        }
                                        bounds.Encapsulate(child.transform.position + new Vector3(child.rect.xMin, child.rect.yMin));
                                        bounds.Encapsulate(child.transform.position + new Vector3(child.rect.xMax, child.rect.yMax));
                                        //bounds.Encapsulate(new Vector3(child.rect.xMin, child.rect.yMin));
                                        //bounds.Encapsulate(new Vector3(child.rect.xMax, child.rect.yMax));
                                        childList.Add(child);
                                        child.SetParent(root);
                                    }

                                    groupUI.sizeDelta = bounds.size;
                                    groupUI.position = bounds.center;
                                    foreach (var item in childList)
                                    {
                                        item.SetParent(groupUI);
                                    }
                                    groupUI.name = layer.TrueName();
                                    groupUI.gameObject.SetActive(layer.Visible);
                                    if (layer.Name.Contains("=prefab"))
                                    {
                                        psdUi.SavePrefabAction += () =>
                                        {
                                            var index = groupUI.GetSiblingIndex();
                                            psdUi.SaveAsPrefab(groupUI);

                                        };
                                    }
                                    else if (layer.Name.Contains("=&prefab"))
                                    {
                                        psdUi.LoadPrefabAction += () =>
                                        {
                                            var ui = psdUi.ChangeToPrefab(groupUI);
                                        };

                                    }
                                }
                                break;
                            case LayerSectionType.SectionDivider:
                                {
                                    //层级结束标志
                                    groupStack.Push(psdUi.CreateGroup(layer, parentUi));
                                }
                                break;
                            default:
                                Debug.LogError("未解析的层级逻辑" + groupInfo.SectionType);
                                break;
                        }

                    }
                    else
                    {
                        psdUi.CreateImage(layer, parentUi);
                    }
                }


            }

            AssetDatabase.Refresh();
            psdUi.LoadSpriteAction?.Invoke();
            psdUi.SavePrefabAction?.Invoke();
            psdUi.LoadPrefabAction?.Invoke();
            foreach (var item in destoryList)
            {
                GameObject.DestroyImmediate(item);
            }
            destoryList.Clear();
            return root;
        }
        public static Vector2 Center(this Layer layer)
        {
            return new Vector2
            {
                x = layer.Rect.Left + layer.Rect.Width / 2,
                y = layer.PsdFile.RowCount - (layer.Rect.Top + layer.Rect.Height / 2),
            };
        }
        public static string TrueName(this Layer layer)
        {
            return layer.Name.Contains("=") ? (layer.Name.Substring(0, layer.Name.IndexOf("="))) : layer.Name;
        }
     
        static List<GameObject> destoryList = new List<GameObject>();
        public static RectTransform ChangeToPrefab(this UiImportSetting psdUi, RectTransform tempUi)
        {
            if (tempUi == null) Debug.LogError("tempUI IsNull " + tempUi);
            var prefab = psdUi.prefabList.CheckGet(tempUi.name,psdUi.parentSetting?.prefabList).prefab;
           
            if (prefab == null)
            {
                Debug.LogError("缺少预制体【" + tempUi.name + "】");
                return null;
            }
            if (UnityEditor.PrefabUtility.IsAnyPrefabInstanceRoot(tempUi.gameObject))
            {
                var prefabAsset = UnityEditor.PrefabUtility.GetCorrespondingObjectFromOriginalSource(tempUi.gameObject);
                if (prefab == prefabAsset)
                {
                    return null;
                }
            }
            var instancePrefab = PrefabUtility.InstantiatePrefab(prefab, tempUi.parent) as GameObject;
            var ui = instancePrefab.GetComponent<RectTransform>();
            instancePrefab.transform.SetSiblingIndex(tempUi.GetSiblingIndex());
            var size = ui.sizeDelta;
           //  = tempUi.sizeDelta;
            ui.anchorMin = tempUi.anchorMin;
            ui.anchorMax = tempUi.anchorMax;
            ui.transform.position = tempUi.transform.position;
            ui.offsetMax = tempUi.offsetMax;
            ui.offsetMin = tempUi.offsetMin;
            destoryList.Add(tempUi.gameObject);
            ui.sizeDelta = size;
            //GameObject.DestroyImmediate(tempUi.gameObject);
            return ui;
        }
        public static void SaveAsPrefab(this UiImportSetting psdUi, RectTransform ui)
        {
            var basePrefab = psdUi.basePrefabList.CheckGet(ui.name + "Base").prefab;
            if (basePrefab == null)
            {
                psdUi.Autoanchored(ui);
                psdUi.basePrefabList.CheckGet(ui.name+"Base").prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(ui.gameObject, Path.Combine(psdUi.ResourcesPath, ui.name + "Base.prefab"), InteractionMode.AutomatedAction);
                var uiPrefab = psdUi.prefabList.CheckGet(ui.name,psdUi.parentSetting?.prefabList).prefab;
                if (uiPrefab == null)
                {
                    uiPrefab = PrefabUtility.SaveAsPrefabAssetAndConnect(ui.gameObject, Path.Combine(psdUi.RootPath, ui.name + ".prefab"), InteractionMode.AutomatedAction);
                    psdUi.prefabList.CheckGet(ui.name).prefab=uiPrefab;
                    return;
                }
            }
          
            psdUi.LoadPrefabAction += () =>
            {
                psdUi.ChangeToPrefab(ui);
            };

        }
        public static void Autoanchored(this UiImportSetting psdUi, RectTransform ui)
        {
            for (int i = 0; i < ui.childCount; i++)
            {

                var child = ui.GetChild(i) as RectTransform;

                var rightUpOffset = ui.UpRight() - child.UpRight();
                var leftDonwOffset = child.DownLeft() - ui.DownLeft();

                var widthCheck = psdUi.AutoanchoredRate * ui.Width();
                var heightCheck = psdUi.AutoanchoredRate * ui.Height();


                if (rightUpOffset.x < widthCheck && leftDonwOffset.x < widthCheck)
                {
                    child.offsetMax = new Vector2(-rightUpOffset.x, child.offsetMax.y);
                    child.anchorMax = new Vector2(1, child.anchorMax.y);
                    child.offsetMin = new Vector2(leftDonwOffset.x, child.offsetMin.y);
                    child.anchorMin = new Vector2(0, child.anchorMin.y);
                }
                else if(rightUpOffset.x<widthCheck*2&&leftDonwOffset.x>1-widthCheck*2+child.Width()/2)
                {
                    child.offsetMax = new Vector2(-rightUpOffset.x, child.offsetMax.y);
                    child.anchorMax = new Vector2(1, child.anchorMax.y);
                    child.offsetMin = new Vector2(-(rightUpOffset.x+child.Width()), child.offsetMin.y);
                    child.anchorMin = new Vector2(1, child.anchorMin.y);
                }
                else if (rightUpOffset.x > 1 - widthCheck * 2 + child.Width() / 2 && leftDonwOffset.x < widthCheck * 2)
                {
                    child.offsetMax = new Vector2(leftDonwOffset.x+child.Width(), child.offsetMax.y);
                    child.anchorMax = new Vector2(0, child.anchorMax.y);
                    child.offsetMin = new Vector2(leftDonwOffset.x, child.offsetMin.y);
                    child.anchorMin = new Vector2(0, child.anchorMin.y);
                }
                if (rightUpOffset.y < heightCheck && leftDonwOffset.y < heightCheck)
                {
                    child.offsetMax = new Vector2(child.offsetMax.x, -rightUpOffset.y);
                    child.anchorMax = new Vector2(child.anchorMax.x, 1);
                    child.offsetMin = new Vector2(child.offsetMin.x, leftDonwOffset.y);
                    child.anchorMin = new Vector2(child.anchorMin.x, 0);
                }
                else if(rightUpOffset.y < heightCheck*2 && leftDonwOffset.y >1- heightCheck*2+child.Height()/2)
                {
                    child.offsetMax = new Vector2(child.offsetMax.x, -rightUpOffset.y);
                    child.anchorMax = new Vector2(child.anchorMax.x, 1);
                    child.offsetMin = new Vector2(child.offsetMin.x, -(rightUpOffset.y+child.Height()));
                    child.anchorMin = new Vector2(child.anchorMin.x, 1);
                }
                else if (rightUpOffset.y > 1 - heightCheck * 2 + child.Height() / 2 && leftDonwOffset.y<heightCheck*2 )
                {
                    child.offsetMax = new Vector2(child.offsetMax.x, leftDonwOffset.y + child.Height());
                    child.anchorMax = new Vector2(child.anchorMax.x, 0);
                    child.offsetMin = new Vector2(child.offsetMin.x, leftDonwOffset.y);
                    child.anchorMin = new Vector2(child.anchorMin.x, 0);
                }
                if (!UnityEditor.PrefabUtility.IsAnyPrefabInstanceRoot(child.gameObject))
                {
                    psdUi.Autoanchored(child);
                }
            }
        }
        public static RectTransform CreateUIBase(this UiImportSetting psdUi, Layer layer, RectTransform parent = null)
        {
            var ui = new GameObject(layer.TrueName(), typeof(RectTransform)).GetComponent<RectTransform>();
            ui.sizeDelta = new Vector2(layer.Rect.Width, layer.Rect.Height)*psdUi.UiSizeScale;
            ui.position = layer.Center()*psdUi.UiSizeScale - parent.rect.size / 2;
            ui.SetParent(parent);
            ui.gameObject.SetActive(layer.Visible);
            return ui;
        }

        public static RectTransform CreateGroup(this UiImportSetting psdUi, Layer layer, RectTransform parent = null)
        {
            var ui = psdUi.CreateUIBase(layer, parent);
            ui.sizeDelta = parent.rect.size;
            return ui;
        }
        public static void CreateImage(this UiImportSetting psdUi, Layer layer, RectTransform parent = null)
        {
            var ui = psdUi.CreateUIBase(layer, parent);
            var tex = CreateTexture(layer);
            if (tex != null)
            {
                var image = ui.gameObject.AddComponent<Image>();
                image.color = new Color(1, 1, 1, layer.Opacity / 255f);
                psdUi.GetSprite(layer, (sprite) =>
                {
                    image.sprite = sprite;
                });
            }
        }
        public static Text CreateText(this UiImportSetting psdUi, Layer layer, RectTransform root = null)
        {
            var textParent= psdUi.CreateUIBase(layer, root);
            var ui = psdUi.CreateUIBase(layer, root);
            ui.SetParent(textParent);
            //  var tex = CreateTexture(layer);
            var text = "";
            var font = "";
            var size = 40f;
            var color = Color.black;
            if (layer.Name.Contains("=text["))
            {
                var startIndex = layer.Name.IndexOf("=text[") + "=text[".Length;
                var endIndex = layer.Name.IndexOf("]");
                var infos = layer.Name.Substring(startIndex, endIndex - startIndex).Split('|');
                text = infos[0].Replace("#$%", "\n");
                font = infos[1];
                size = float.Parse(infos[2]);
                ColorUtility.TryParseHtmlString("#" + infos[3], out color);

                //Debug.LogError(subStr);
            }
            else
            {
                Debug.LogWarning("文字层 缺少字体大小等相关信息 请先在Ps中运行脚本(生成UGUI格式文件.jsx)");
            }

            var textUi = ui.gameObject.AddComponent<Text>();
            ui.sizeDelta += Vector2.one * 4;
            textUi.text = text;
            var fongSetting= psdUi.fontList.CheckGet(font,psdUi.parentSetting?.fontList);
            textUi.fontSize = (int)(size * psdUi.TextScale);
            if (fongSetting.font == null)
            {
                Debug.LogError("未指定字体【" + font + "】");
            }
            else
            {
                textUi.font = fongSetting.font;
                textUi.transform.position += Vector3.up * fongSetting.YOffset * textUi.fontSize;
            }
          
            //textUi.horizontalOverflow = HorizontalWrapMode.Overflow;
            textUi.verticalOverflow = VerticalWrapMode.Overflow;
            textUi.color = color;
            return textUi;
        }


        public static void GetSprite(this UiImportSetting psdUi, Layer layer, Action<Sprite> callBack)
        {
            var tex = CreateTexture(layer);

            if (tex != null)
            {
                string path = Path.Combine(psdUi.ResourcesPath, psdUi.name + "_" + tex.name + ".png");
                File.WriteAllBytes(path, tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
                psdUi.LoadSpriteAction += () =>
                {
                    var sprite = psdUi.LoadSprite(path);
                    if (sprite != null)
                    {
                        callBack?.Invoke(sprite);
                    }
                };
            }

        }
        static Sprite LoadSprite(this UiImportSetting psdUi, string path)
        {
            TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;
            textureImporter.textureType = TextureImporterType.Sprite;
            textureImporter.spriteImportMode = SpriteImportMode.Single;
            textureImporter.spritePivot = new Vector2(0.5f, 0.5f);
            textureImporter.spritePixelsPerUnit = 100;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            return (Sprite)AssetDatabase.LoadAssetAtPath(path, typeof(Sprite));
        }

        public static Texture2D CreateTexture(Layer layer)
        {
            if ((int)layer.Rect.Width == 0 || (int)layer.Rect.Width == 0)
                return null;
            Texture2D tex = new Texture2D((int)layer.Rect.Width, (int)layer.Rect.Height, TextureFormat.RGBA32, true);
            Color32[] pixels = new Color32[tex.width * tex.height];

            Channel red = (from l in layer.Channels where l.ID == 0 select l).First();
            Channel green = (from l in layer.Channels where l.ID == 1 select l).First();
            Channel blue = (from l in layer.Channels where l.ID == 2 select l).First();
            Channel alpha = layer.AlphaChannel;
            for (int i = 0; i < pixels.Length; i++)
            {
                byte r = red.ImageData[i];
                byte g = green.ImageData[i];
                byte b = blue.ImageData[i];
                byte a = 255;

                if (layer.Name.Contains("white"))
                {
                    if (r > 0)
                    {
                        r = 255;
                    }
                    if (g > 0)
                    {
                        g = 255;
                    }
                    if (b > 0)
                    {
                        b = 255;
                    }
                }
                if (alpha != null)
                    a = alpha.ImageData[i];

                int mod = i % tex.width;
                int n = ((tex.width - mod - 1) + i) - mod;
                pixels[pixels.Length - n - 1] = new Color32(r, g, b, a);
            }

            tex.SetPixels32(pixels);
            tex.name = layer.SaveName();
            tex.Apply();
            return tex;
        }
    }

    //public static string QReadUnicode(this PsdBinaryReader reader ,string viewStr = "")
    //{
    //    var numChars = reader.ReadInt32();
    //    var length = 2 * numChars;
    //    var data = reader.ReadBytes(length);
    //    var str =System.Text.Encoding.BigEndianUnicode.GetString(data, 0, length - 2);
    //    (viewStr + " Unicode" + numChars + ":[" + str + "]").TestDebug();
    //    return str;
    //}
    //public static string QReadId(this PsdBinaryReader reader ,string name = "")
    //{
    //    var numChars = reader.ReadInt32();
    //    if (numChars == 0)
    //    {
    //        numChars = 4;
    //    }

    //    var data = reader.ReadBytes(numChars);
    //    var str = System.Text.Encoding.ASCII.GetString(data);
    //    (name + " ID " + numChars + "[" + str + "]").TestDebug();
    //    return str;
    //}
    //public static void TestDebug(this object obj)
    //{
    //    Debug.Log(obj);
    //}
    //public static void ReaderDescriptor(this PsdBinaryReader reader)
    //{

    //    //System.Text.Encoding.ASCII.GetBytes("obj ").DebugInfo("obj :");
    //    //System.Text.Encoding.ASCII.GetBytes("TEXT").DebugInfo("TEXT :");
    //    //System.Text.Encoding.ASCII.GetBytes("Objc").DebugInfo("Objc :");
    //    //System.Text.Encoding.ASCII.GetBytes("VlLs").DebugInfo("VlLs :");
    //    //System.Text.Encoding.ASCII.GetBytes("doub").DebugInfo("doub :");
    //    //System.Text.Encoding.ASCII.GetBytes("UntF").DebugInfo("UntF :");
    //    //System.Text.Encoding.ASCII.GetBytes("enum").DebugInfo("enum :");
    //    //System.Text.Encoding.ASCII.GetBytes("long").DebugInfo("long :");
    //    //System.Text.Encoding.ASCII.GetBytes("comp").DebugInfo("comp :");
    //    //System.Text.Encoding.ASCII.GetBytes("bool").DebugInfo("bool :");
    //    //System.Text.Encoding.ASCII.GetBytes("GlbO").DebugInfo("GlbO :");
    //    //System.Text.Encoding.ASCII.GetBytes("type").DebugInfo("type :");
    //    //System.Text.Encoding.ASCII.GetBytes("GlbC").DebugInfo("GlbC :");
    //    //System.Text.Encoding.ASCII.GetBytes("alis").DebugInfo("alis :");
    //    //System.Text.Encoding.ASCII.GetBytes("tdta").DebugInfo("tdta :");
    //    //   Debug.LogError(System.Text.Encoding.ASCII.GetString(new Byte[] { 8, 4, 1, 2 }));
    //    // reader.ReadBytes(500).DebugInfo();
    //    //0 0 0 1 0 0

    //    var className = reader.QReadUnicode();
    //    ("className [" + className + "]").TestDebug();

    //    //0 0 0 0 84 120 76 114

    //    var classId = reader.QReadId();
    //    // Debug.LogError("classId ["+classId+"]");

    //    //  0 0 0 8

    //    var iCount = reader.ReadInt32();
    //    ("iCount" + iCount).TestDebug();
    //    for (int i = 0; i < iCount; i++)
    //    {
    //        //  0 0 0 0 84 120 116 32
    //        var name= reader.QReadId("密匙");

    //        //  84 69 88 84

    //        var key = reader.ReadAsciiChars(4);
    //        Debug.LogError("name " + name+" ("+key+")");
    //        //  0 0 0 11 0 116 0 101 0 115 0 116 0 84 0 101 0 120 0 116 0 97 0 100 0 0

    //        //  Debug.LogError("key [" + key + "]");
    //        switch (key)
    //        {
    //            case "Objc":
    //                {

    //                    //0 0 0 1 0 0 0 0 0 6 98 111 117 110 100 115 0 0 0 4 0 0 0 0 76 101 102 116 85 110 116 70 35 80 120 108 0 0 0 0 0 0 0 0 0 0 0 0 84 111 112 32 85 110 116 70 35 80 120 108 192 101 15 204 0 0 0 0 0 0 0 0 82 10

    //                    reader.ReaderDescriptor();
    //                }
    //                break;
    //            case "enum":
    //                {

    //                    // 0 0 0 12 116 101 120 116 71 114 105 100 100 105 110 103 0 0 0 0 78 111 110 101 0 0 0 0 79 114 110 116 101 110 117 109 0 0 0 0 79 114 110 116 0 0 0 0 72 114 122 110 0 0
    //                    var enumName = reader.QReadId("枚举 classId");
    //                    reader.QReadId("枚举 值");
    //                }
    //                break;
    //            case "TEXT":
    //                {
    //                    ("字符串:[" + reader.QReadUnicode() + "]").TestDebug();
    //                }break;
    //            case "UntF":
    //                {
    //                    var fKey = reader.ReadAsciiChars(4);
    //                    reader.ReadDouble();

    //                }
    //                break;
    //            case "long":
    //                {
    //                    var longValue= reader.ReadInt32();
    //                }break;
    //            case "tdta":
    //                {
    //                    //0 0 32 23 10 10 60 60 10 9 47 69 110 103 105 110 101 68 105 99 116 10 9 60 60 10 9 9 47 69 100 105 116 111 114 10 9 9 60 60 10 9 9 9 47 84 101 120 116 32 40 254 255 0 116 0 101 0 115 0 116 0 84 0 101 0 120 0 116 0 97 0 100 0 13 41 10 9 9 62 62 10 9 9 47 80 97 114 97 103 114 97 112 104 82 117 110 10 9 9 
    //                    var count = reader.ReadInt32();
    //                    //10 10 60 60 10 9 47 69 110 103 105 110 101 68 105 99 116 10 9 60 60 10 9 9 47 69 100 105 116 111 114 10 9 9 60 60 10 9 9 9 47 84 101 120 116 32 40 254 255 0 116 0 101 0 115 0 116 0 84 0 101 0 120 0 116 0 97 0 100 0 13 41 10 9 9 62 62 10 9 9 47 80 97 114 97 103 114 97 112 104 82 117 110 10 9 9 60 60 10 9 9 9 47 68 101 102 97 117 108 116 82 117 110 68 97 116 97 10 9 9 9 60 60 10 9 9 9 9 47 80 97 114 97 103 114 97 112 104 83 104
    //                    //  System.Text.Encoding.ASCII.GetBytes("size").DebugInfo("size:");
    //                    reader.ReadBytes(count);//.DebugInfo("测试数据");

    //                }break;
    //            case "doub":
    //                {
    //                    reader.ReadBytes(8);
    //                }break;
    //            default:
    //                new Exception("未解析的关键字【" + key + "】");
    //                break;
    //        }
    //    }

    //    //switch (key)
    //    //{
    //    //    default:
    //    //        break;
    //    //}
    //    //var tCount = reader.ReadInt32();
    //    //Debug.LogError("tCount " + tCount);
    //    //for (int i = 0; i < tCount; i++)
    //    //{
    //    //    Debug.LogError(reader.ReadAsciiChars(4));
    //    //}
    //    //for (int i = 0; i < count; i++)
    //    //{
    //    //    //c
    //    //}
    //    //var count = reader.ReadInt16();
    //    //Debug.LogError("count " + count);
    //    //var styleCount = reader.ReadInt16();
    //    //Debug.LogError("styleCount " + styleCount);
    //    //for (int i = 0; i < styleCount; i++)
    //    //{
    //    //    reader.ReadBytes(27);
    //    //}
    //    //reader.ReadBytes(24);
    //    //var lineCount = reader.ReadInt16();
    //    //Debug.LogError("lineCount " + lineCount);
    //    //for (int i = 0; i < 60; i++)
    //    //{
    //    //    Debug.LogError(i+" "+ reader.ReadByte());

    //    //}
    //    // reader.ReadBytes(49);
    //    //var tversion = reader.ReadByte();
    //    //Debug.LogError("tversion " + tversion);
    //    //reader.ReadByte();
    //    //var count= reader.ReadByte();
    //    //Debug.LogError("font count "+count);
    //}

    //public static void DebugInfo(this byte[] bytes,string startStr="")
    //{
    //    var str = startStr;
    //   // var lng = maxLength > 0 ? (Mathf.Max(bytes.Length, maxLength)):bytes.Length;
    //    for (int i = 0; i < Mathf.Min(bytes.Length,1000); i++)
    //    {
    //        str += bytes[i]+" ";
    //    }
    //    Debug.LogError(str);
    //}
    //     using (var memery=new MemoryStream(textInfo.Data))
    //                {
    //                    using (var reader = new PsdBinaryReader(memery, System.Text.Encoding.Default))
    //                    {

    //                        //textInfo.Data.DebugInfo(2000);
    //                        //   reader.ReadByte();
    //                        var version = reader.ReadInt16();
    //("version " + version).TestDebug();
    //reader.ReadBytes(48);
    //                        var fontVersion = reader.ReadInt16();
    //("fontVersion " + fontVersion).TestDebug();
    //                        if (fontVersion != 50)
    //                        {
    //                            Debug.LogError("Text 错误文本对象格式应使用对应版本的文件格式 Type tool object setting (Photoshop 6.0)");

    //                        }
    //                        var textVersion = reader.ReadInt32();
    //Debug.LogError("textVersion " + textVersion);


    //                        reader.ReaderDescriptor();
    //                        var warpVersion = reader.ReadInt16();
    //var DescriptorVersion = reader.ReadInt32();
    //reader.ReaderDescriptor();
    //                        reader.ReadBytes(4 * 8);

    //                    }


    //                }
    #endregion


    public static class RectTransformExtend
    {
        public static Vector2 UpRightRectOffset(this RectTransform rectTransform)
        {
            return new Vector2(rectTransform.Width() * (1 - rectTransform.pivot.x), rectTransform.Height() * (1 - rectTransform.pivot.y));
        }
        public static Vector2 DownLeftRectOffset(this RectTransform rectTransform)
        {
            return new Vector2(rectTransform.Width() * (rectTransform.pivot.x), rectTransform.Height() * (rectTransform.pivot.y));
        }

        public static float Height(this RectTransform rectTransform)
        {
            return rectTransform.rect.size.y;
        }
        public static float Width(this RectTransform rectTransform)
        {
            return rectTransform.rect.size.x;
        }
        public static Vector2 Size(this RectTransform rectTransform)
        {
            return rectTransform.rect.size;
        }

        public static RectTransform RectTransform(this Transform transform)
        {
            return transform as RectTransform;
        }
        public static Vector2 UpRight(this RectTransform rectTransform)
        {
            return new Vector2(rectTransform.position.x, rectTransform.position.y) + rectTransform.UpRightRectOffset();
        }
        public static Vector2 DownLeft(this RectTransform rectTransform)
        {
            return new Vector2(rectTransform.position.x, rectTransform.position.y) - rectTransform.DownLeftRectOffset();
        }

    }
}