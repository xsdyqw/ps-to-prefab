using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.AssetImporters;
using PhotoshopFile;
using UnityEngine.UI;
using System.IO;
using System.Linq;
using UnityEditor;

[UnityEditor.AssetImporters.ScriptedImporter(4, new string[] { }, new string[] { "psd", "psb" })]
public class UiPsdImporter : ScriptedImporter
{
    public override void OnImportAsset(AssetImportContext ctx)
    {

        var psd = new PsdFile(ctx.assetPath, new LoadContext { Encoding = System.Text.Encoding.Default });
        var root = ctx.CreateUI(psd.BaseLayer);
        root.gameObject.AddComponent<Canvas>();
        Debug.LogError("²ãÊý£º" + psd.Layers.Count);
        foreach (var item in psd.Layers)
        {
            ctx.CreateUI(item, root);
            //foreach (var channel in item.Channels)
            //{

            //}
        }
        ctx.SetMainObject(root.gameObject);
    }

}
public static class UiPsdImporterExtends
{
    //public static RectTransform CreateUI(this AssetImportContext ctx,string name, RectTransform parent = null)
    //{
    //    var ui = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
    //    ui.SetParent(parent);
    //    ctx.AddObjectToAsset(ui.name, ui.gameObject);
    //    return ui;
    //}
    public static RectTransform CreateUI(this AssetImportContext ctx, Layer layer, RectTransform parent = null)
    {
        var ui = new GameObject(layer.Name, typeof(RectTransform)).GetComponent<RectTransform>();
        ui.sizeDelta = new Vector2(layer.Rect.Width, layer.Rect.Height);
        ui.position = new Vector2(layer.Rect.X, layer.Rect.Y);
        ui.SetParent(parent);
        ctx.AddObjectToAsset(ui.name, ui.gameObject);


        var image= ctx.CreateTexture(layer);
        if (image != null)
        {
            var imageUI= ui.gameObject.AddComponent<Image>();
            imageUI.sprite = image;
        }
        return ui;
    }
    public static Sprite CreateTexture(this AssetImportContext ctx, Layer layer)
    {
        var tex = CreateTexture(layer);
        if (tex != null)
        {
            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero);
            sprite.name = layer.Name;
            ctx.AddObjectToAsset(sprite.name, sprite);

            return sprite;
        }
        return null;
    }
    public static Texture2D CreateTexture( Layer layer)
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
        tex.Apply();
        return tex;
    }
}