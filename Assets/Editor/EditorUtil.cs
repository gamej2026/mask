using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class EditorUtil
{
    //# : shift
    //& : alt
    //% :  Winows의 Ctrl, macOS의 cmd키
    [MenuItem("Util/Play %E")]
    private static void PlayEditor()
    {
        EditorApplication.isPlaying = true;
    }

    [MenuItem("Util/Stop %W")]
    private static void StopEditor()
    {
        EditorApplication.isPlaying = false;
    }

    [MenuItem("GameObject/Copy Path", false, -10)]
    private static void CopyTransformsPath()
    {
        List<Transform> selectItems = new List<Transform>();
        selectItems.AddRange(Selection.transforms);
        selectItems.Sort(
            delegate (Transform p1, Transform p2)
            {
                return (p1.name.CompareTo(p2.name)) * -1;
            });

        StringBuilder sb = new StringBuilder();
        foreach (Transform t in selectItems)
        {
            string originalName = t.name;
            string componentPath = t.name;
            Transform tParent = t.parent;
            while (tParent != null)
            {
                componentPath = string.Format("{0}/{1}", tParent.name, componentPath);
                tParent = tParent.parent;
            }

            sb.AppendLine("\"" + componentPath + "\"");
        }

        clipboard = sb.ToString().Trim();
    }

    public static string clipboard
    {
        get
        {
            TextEditor te = new TextEditor();
            te.Paste();
            return te.text;
        }
        set
        {
            TextEditor te = new TextEditor();
            te.text = value;
            te.OnFocus();
            te.Copy();
        }
    }
}
