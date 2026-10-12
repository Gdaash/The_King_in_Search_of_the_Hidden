using UnityEngine;
public sealed class QuestTitleMirror : MonoBehaviour
{
    public UnityEngine.UI.Text source,target;
    void LateUpdate(){if(source&&target)target.text=source.text;}
}
