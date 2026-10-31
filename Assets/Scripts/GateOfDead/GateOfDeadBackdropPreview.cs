using UnityEngine;

namespace RoyalFortress.GateOfDead
{
    /// <summary>Art review only: switches the plate without disturbing registered animation layers.</summary>
    public sealed class GateOfDeadBackdropPreview : MonoBehaviour
    {
        public SpriteRenderer background;
        public Sprite original;
        public Sprite revised;
        public bool useRevised = true;
        public GameObject revisedProps;
        private Font font;
        private GUIStyle labelStyle, buttonStyle;
        private void Start() { Apply(); }
        public void SelectRevised(bool value) { useRevised = value; Apply(); }
        private void Apply()
        {
            if(background!=null)background.sprite=useRevised?revised:original;
            if(revisedProps!=null)revisedProps.SetActive(useRevised);
        }
        private void OnGUI()
        {
            if(font==null)
            {
                font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","Arial"},16);
                labelStyle=new GUIStyle(GUI.skin.label){font=font,fontSize=16};
                buttonStyle=new GUIStyle(GUI.skin.button){font=font,fontSize=15};
            }
            GUILayout.BeginArea(new Rect(Screen.width-326,12,314,86),GUI.skin.box);
            GUILayout.Label(useRevised?"图三底图 · 枯骨墓域 v2":"图三底图 · 原版",labelStyle);
            GUILayout.BeginHorizontal();
            if(GUILayout.Button("新版底图",buttonStyle))SelectRevised(true);
            if(GUILayout.Button("原版对照",buttonStyle))SelectRevised(false);
            GUILayout.EndHorizontal();GUILayout.EndArea();
        }
        private void OnDestroy(){if(font!=null)Destroy(font);}
    }
}
