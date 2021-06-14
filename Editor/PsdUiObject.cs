using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using PhotoshopFile;
using System.Linq;
using UnityEngine.UI;
using System;

public class PsdUiObject: ScriptableObject
{
    public UnityEngine.Object psdFile;
    public int a;
    public List<GameObject> list;
    public GameObject uiPrefab;
    [ContextMenu("生成UI预制体")]
    public void CreateUIPrefab()
    {
        var assetPath = AssetDatabase.GetAssetPath(psdFile);
        var rootPath = Path.GetDirectoryName(assetPath);
        var name = Path.GetFileNameWithoutExtension(assetPath);
        var root = assetPath.CreateUIPrefabRoot();
        for (int i = 0; i < root.childCount; i++)
        {
            var ui = root.GetChild(i);
            if (ui.childCount > 0)
            {
                ui.gameObject.AddComponent<Canvas>();
                uiPrefab = PrefabUtility.SaveAsPrefabAsset(ui.gameObject, Path.Combine(rootPath, name, ui.name + ".prefab"));
            }
            
        }
        GameObject.DestroyImmediate(root.gameObject);
    }   

}

public static class UiPsdImporterExtends
{
    public static string PathName(this Layer layer)
    {
        var name = layer.Name;
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
        return layer.GetInfo("TySh") as RawLayerInfo ;
    }
    public static LayerInfo GetInfo(this Layer layer,string key)
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
    public static string QReadUnicode(this PsdBinaryReader reader ,string viewStr = "")
    {
        var numChars = reader.ReadInt32();
        var length = 2 * numChars;
        var data = reader.ReadBytes(length);
        var str =System.Text.Encoding.BigEndianUnicode.GetString(data, 0, length - 2);
        UnityEngine.Debug.LogError(viewStr + " Unicode" + numChars + ":[" + str + "]");
        return str;
    }
    public static string QReadId(this PsdBinaryReader reader ,string name = "")
    {
        var numChars = reader.ReadInt32();
        if (numChars == 0)
        {
            numChars = 4;
        }

        var data = reader.ReadBytes(numChars);
        var str = System.Text.Encoding.ASCII.GetString(data);
        UnityEngine.Debug.LogError(name + " ID " + numChars + "[" + str + "]");
        return str;
    }
    public static void ReaderDescriptor(this PsdBinaryReader reader)
    {


        //0 0 0 1 0 0

        var className = reader.QReadUnicode();
        Debug.LogError("className [" + className + "]");

        //0 0 0 0 84 120 76 114

        var classId = reader.QReadId();
        // Debug.LogError("classId ["+classId+"]");

        //  0 0 0 8

        var iCount = reader.ReadInt32();
        Debug.LogError("iCount" + iCount);
        for (int i = 0; i < iCount; i++)
        {
            //  0 0 0 0 84 120 116 32
            reader.QReadId("密匙");
            //  84 69 88 84

            var key = reader.ReadAsciiChars(4);

            //  0 0 0 11 0 116 0 101 0 115 0 116 0 84 0 101 0 120 0 116 0 97 0 100 0 0

            Debug.LogError("key [" + key + "]");
            switch (key)
            {
                case "Objc":
                    {

                        //0 0 0 1 0 0 0 0 0 6 98 111 117 110 100 115 0 0 0 4 0 0 0 0 76 101 102 116 85 110 116 70 35 80 120 108 0 0 0 0 0 0 0 0 0 0 0 0 84 111 112 32 85 110 116 70 35 80 120 108 192 101 15 204 0 0 0 0 0 0 0 0 82 10

                        reader.ReaderDescriptor();
                    }
                    break;
                case "enum":
                    {

                        // 0 0 0 12 116 101 120 116 71 114 105 100 100 105 110 103 0 0 0 0 78 111 110 101 0 0 0 0 79 114 110 116 101 110 117 109 0 0 0 0 79 114 110 116 0 0 0 0 72 114 122 110 0 0
                        var enumName = reader.QReadId("枚举 classId");
                        reader.QReadId("枚举 值");
                    }
                    break;
                case "TEXT":
                    {
                        Debug.LogError("字符串:[" + reader.QReadUnicode() + "]");
                    }break;
                case "UntF":
                    {
                        var fKey = reader.ReadAsciiChars(4);
                        reader.ReadDouble();

                    }
                    break;
                case "long":
                    {
                        var longValue= reader.ReadInt32();
                    }break;
                case "tdta":
                    {
                        //0 0 32 23 10 10 60 60 10 9 47 69 110 103 105 110 101 68 105 99 116 10 9 60 60 10 9 9 47 69 100 105 116 111 114 10 9 9 60 60 10 9 9 9 47 84 101 120 116 32 40 254 255 0 116 0 101 0 115 0 116 0 84 0 101 0 120 0 116 0 97 0 100 0 13 41 10 9 9 62 62 10 9 9 47 80 97 114 97 103 114 97 112 104 82 117 110 10 9 9 
                        var count = reader.ReadInt32();
                        reader.ReadBytes(count);
                    }break;
                case "doub":
                    {
                        reader.ReadBytes(8);
                    }break;
                default:
                    Debug.LogError("未解析的关键字【" + key + "】");
                    break;
            }
        }
      
        //switch (key)
        //{
        //    default:
        //        break;
        //}
        //var tCount = reader.ReadInt32();
        //Debug.LogError("tCount " + tCount);
        //for (int i = 0; i < tCount; i++)
        //{
        //    Debug.LogError(reader.ReadAsciiChars(4));
        //}
        //for (int i = 0; i < count; i++)
        //{
        //    //c
        //}
        //var count = reader.ReadInt16();
        //Debug.LogError("count " + count);
        //var styleCount = reader.ReadInt16();
        //Debug.LogError("styleCount " + styleCount);
        //for (int i = 0; i < styleCount; i++)
        //{
        //    reader.ReadBytes(27);
        //}
        //reader.ReadBytes(24);
        //var lineCount = reader.ReadInt16();
        //Debug.LogError("lineCount " + lineCount);
        //for (int i = 0; i < 60; i++)
        //{
        //    Debug.LogError(i+" "+ reader.ReadByte());

        //}
        // reader.ReadBytes(49);
        //var tversion = reader.ReadByte();
        //Debug.LogError("tversion " + tversion);
        //reader.ReadByte();
        //var count= reader.ReadByte();
        //Debug.LogError("font count "+count);
    }

    public static void DebugInfo(this byte[] bytes,string startStr="")
    {
        var str = startStr;
       // var lng = maxLength > 0 ? (Mathf.Max(bytes.Length, maxLength)):bytes.Length;
        for (int i = 0; i < bytes.Length; i++)
        {
            str += bytes[i]+" ";
        }
        Debug.LogError(str);
    }
    public static RectTransform CreateUIPrefabRoot(this string assetPath)
    {
        Stack<RectTransform> groupStack = new Stack<RectTransform>();;
        var psd = new PsdFile(assetPath, new LoadContext { Encoding = System.Text.Encoding.Default });
        var name = Path.GetFileNameWithoutExtension(assetPath);
        var root = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        root.sizeDelta = new Vector2(psd.ColumnCount, psd.RowCount);
        foreach (var layer in psd.Layers)
        {
            var str ="";
            foreach (var item in layer.AdditionalInfo)
            {
              //  if (!(item is RawLayerInfo)) continue;
                str += item.GetType().Name+"  " +item.Key+"  ";
            }
            var parentUi=groupStack.Count>0?groupStack.Peek():root;
            str += "\t\t" + layer.Name+":";
            Debug.Log(str);
            var textInfo= layer.GetTextInfo();
            if (textInfo!=null)
            {
              //  System.Text.Encoding.ASCII.GetBytes("Berlin Sans FB Demi").DebugInfo();
                Debug.LogError("text 【"+layer.Name + "】"+textInfo.Data.Length);
                using (var memery=new MemoryStream(textInfo.Data))
                {
                    using (var reader = new PsdBinaryReader(memery, System.Text.Encoding.Default))
                    {

                        //textInfo.Data.DebugInfo(2000);
                        //   reader.ReadByte();
                        var version = reader.ReadInt16();
                        Debug.LogError("version " + version);
                        reader.ReadBytes(48);
                        var fontVersion = reader.ReadInt16();
                        Debug.LogError("fontVersion " + fontVersion);
                        if (fontVersion != 50)
                        {
                            Debug.LogError("Text 错误文本对象格式应使用对应版本的文件格式 Type tool object setting (Photoshop 6.0)");

                        }
                        var textVersion = reader.ReadInt32();
                        Debug.LogError("textVersion " + textVersion);

                        System.Text.Encoding.ASCII.GetBytes("obj ").DebugInfo("obj :");
                        System.Text.Encoding.ASCII.GetBytes("TEXT").DebugInfo("TEXT :");
                        System.Text.Encoding.ASCII.GetBytes("Objc").DebugInfo("Objc :");
                        System.Text.Encoding.ASCII.GetBytes("VlLs").DebugInfo("VlLs :");
                        System.Text.Encoding.ASCII.GetBytes("doub").DebugInfo("doub :");
                        System.Text.Encoding.ASCII.GetBytes("UntF").DebugInfo("UntF :");
                        System.Text.Encoding.ASCII.GetBytes("enum").DebugInfo("enum :");
                        System.Text.Encoding.ASCII.GetBytes("long").DebugInfo("long :");
                        System.Text.Encoding.ASCII.GetBytes("comp").DebugInfo("comp :");
                        System.Text.Encoding.ASCII.GetBytes("bool").DebugInfo("bool :");
                        System.Text.Encoding.ASCII.GetBytes("GlbO").DebugInfo("GlbO :");
                        System.Text.Encoding.ASCII.GetBytes("type").DebugInfo("type :");
                        System.Text.Encoding.ASCII.GetBytes("GlbC").DebugInfo("GlbC :");
                        System.Text.Encoding.ASCII.GetBytes("alis").DebugInfo("alis :");
                        System.Text.Encoding.ASCII.GetBytes("tdta").DebugInfo("tdta :");
                        //   Debug.LogError(System.Text.Encoding.ASCII.GetString(new Byte[] { 8, 4, 1, 2 }));
                        // reader.ReadBytes(500).DebugInfo();
                        reader.ReaderDescriptor();
                        var warpVersion = reader.ReadInt16();
                        var DescriptorVersion = reader.ReadInt32();
                        reader.ReaderDescriptor();
                        reader.ReadBytes(4 * 8);
                      
                    }


                }

               
               // assetPath.CreateUI(layer, parentUi);
            }
            else
            {
                var groupInfo= layer.GetGroupInfo();
                if (groupInfo!=null)
                {
                    if(groupInfo.SectionType== LayerSectionType.SectionDivider)
                    {
                        groupStack.Push(assetPath.CreateGroup(layer, parentUi));
                    }
                    else if(groupInfo.SectionType== LayerSectionType.ClosedFolder||groupInfo.SectionType== LayerSectionType.OpenFolder)
                    {
                        var groupUI= groupStack.Pop();
                        Bounds bounds = new Bounds();
                        var childList = new List<RectTransform>();
                        
                        for (int i = groupUI.childCount-1; i >=0; i--)
                        {
                            var child = groupUI.GetChild(0) as RectTransform;
                            if (bounds.center==Vector3.zero&&bounds.size==Vector3.zero)
                            {
                                bounds = new Bounds(child.position, Vector3.zero);
                            }
                            bounds.Encapsulate(child.transform.position+ new Vector3(child.rect.xMin, child.rect.yMin));
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
                            item.SetParent( groupUI);
                        }
                        groupUI.name = layer.Name;
                        groupUI.gameObject.SetActive(layer.Visible);
                    }
                   // Debug.LogError(layer.Name + " Group:" + groupInfo.Key + " ：" + groupInfo.Signature+":"+ groupInfo.SectionType);
                }
                else
                {
                    assetPath.CreateUI(layer, parentUi);
                }
            }
           

        }
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
    public static RectTransform CreateUI(this string assetPath, Layer layer, RectTransform parent = null)
    {
        var ui = new GameObject(layer.Name, typeof(RectTransform)).GetComponent<RectTransform>();
        ui.sizeDelta = new Vector2(layer.Rect.Width, layer.Rect.Height);
        ui.position = layer.Center() - parent.rect.size / 2;
        ui.SetParent(parent);
        ui.gameObject.SetActive(layer.Visible);
        assetPath.CreateImage(layer, ui);
        return ui;
    }
    public static RectTransform CreateGroup(this string assetPath, Layer layer, RectTransform parent = null)
    {
        var ui = new GameObject(layer.Name, typeof(RectTransform)).GetComponent<RectTransform>();
        ui.position = layer.Center() - parent.rect.size / 2;
        ui.SetParent(parent);
        ui.gameObject.SetActive(layer.Visible);
        ui.sizeDelta = parent.rect.size;
        assetPath.CreateImage(layer, ui);
        return ui;
    }
    public static Sprite CreateImage(this string assetPath, Layer layer, RectTransform ui)
    {
        var tex = CreateTexture(layer);
        if (tex != null)
        {
            var sprite = assetPath.SaveSprite(tex);
            var image = ui.gameObject.AddComponent<Image>();
           image.color=new Color(1,1,1, layer.Opacity / 255f);
            image.sprite = sprite;
            return sprite;
        }
        return null;
    }

    public static Sprite SaveSprite(this string assetPath, Texture2D tex)
    {
        if (tex == null) return null;
        var rootPath = Path.Combine(Path.GetDirectoryName(assetPath), Path.GetFileNameWithoutExtension(assetPath));
        if (!Directory.Exists(rootPath))
        {
            Directory.CreateDirectory(rootPath);
        }
        string path;
        try
        {
            path = Path.Combine(rootPath, tex.name + ".png");
        }
        catch (System.Exception)
        {

            throw new System.Exception(rootPath+"  ： "+tex.name);
        }
      

        byte[] buf = tex.EncodeToPNG();
        File.WriteAllBytes(path, buf);
        AssetDatabase.Refresh();
        TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;
        textureImporter.textureType = TextureImporterType.Sprite;
        textureImporter.spriteImportMode = SpriteImportMode.Single;
        textureImporter.spritePivot = new Vector2(0.5f, 0.5f);
        textureImporter.spritePixelsPerUnit = 100;
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        UnityEngine.Object.DestroyImmediate(tex);
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

            if (alpha != null)
                a = alpha.ImageData[i];

            int mod = i % tex.width;
            int n = ((tex.width - mod - 1) + i) - mod;
            pixels[pixels.Length - n - 1] = new Color32(r, g, b, a);
        }

        tex.SetPixels32(pixels);
        tex.name = layer.PathName();
        tex.Apply();
        return tex;
    }
}