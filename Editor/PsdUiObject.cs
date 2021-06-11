using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using PhotoshopFile;
using System.Linq;
using UnityEngine.UI;

public class PsdUiObject: ScriptableObject
{
    public Object psdFile;
    public int a;
    public List<GameObject> list;
    public GameObject uiPrefab;
    [ContextMenu("生成UI预制体")]
    public void CreateUIPrefab()
    {
        var assetPath = AssetDatabase.GetAssetPath(psdFile);
        var rootPath = Path.GetDirectoryName(assetPath);
        var obj = assetPath.CreateUIPrefab().gameObject;
        uiPrefab= PrefabUtility.SaveAsPrefabAsset(obj, Path.Combine(rootPath, obj.name+".prefab"));
        GameObject.DestroyImmediate(obj);;
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
            name += layer.LayerID;
        }
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
   
    public static RectTransform CreateUIPrefab(this string assetPath)
    {
        Stack<RectTransform> groupStack = new Stack<RectTransform>();;
        var psd = new PsdFile(assetPath, new LoadContext { Encoding = System.Text.Encoding.Default });
        var name = Path.GetFileNameWithoutExtension(assetPath);
        var root = new GameObject(name, typeof(RectTransform), typeof(Canvas)).GetComponent<RectTransform>();
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
                assetPath.CreateUI(layer, parentUi);
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
                    else if(groupInfo.SectionType== LayerSectionType.ClosedFolder)
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
        Object.DestroyImmediate(tex);
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