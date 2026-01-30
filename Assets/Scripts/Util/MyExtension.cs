using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public static class MyExtension
{        
    static public string GetPath(this Transform t)
    {
        // 부모가 있으면 부모 경로와 경로 구분자를 넣는다.
        StringBuilder sb = new StringBuilder();
        GetParentPath(t, sb);
        sb.Append(t.name);
        return sb.ToString();

        void GetParentPath(Transform tr, StringBuilder sb)
        {
            if (tr.parent != null)
            {
                GetParentPath(tr.parent, sb);

                sb.Append(tr.parent.name);
                sb.Append('/');
            }
        }
    }
}