using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.AssetImporters;
using PhotoshopFile;
using UnityEngine.UI;
using System.IO;
using System.Linq;
using UnityEditor;

namespace QTool.Psd2Ui
{
    public static class Impoter
    {
        [MenuItem("Assets/����/psd|psb����UI %u")]
        static void CreateUI()
        {
            foreach (var obj in Selection.objects)
            {
                var path = AssetDatabase.GetAssetPath(obj);
                var ext = Path.GetExtension(path);
                var name = Path.GetFileNameWithoutExtension(path);
                if (ext.Equals(".psd") || ext.Equals(".psb"))
                {
                    var uiObj = ScriptableObject.CreateInstance<PsdUiObject>();
                    uiObj.Init(obj);
                    AssetDatabase.CreateAsset(uiObj, Path.Combine(Path.GetDirectoryName(path), name + ".asset"));
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
                else
                {
                    Debug.LogError(path + "�ļ�����psd��psb�ļ��޷�����UI");
                }

            }

        }
    }
}