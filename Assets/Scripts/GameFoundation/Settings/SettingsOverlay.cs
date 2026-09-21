using GameFoundation.Localization;
using UnityEngine;

namespace GameFoundation.Settings
{
    public sealed class SettingsOverlay : MonoBehaviour
    {
        private bool open;
        private void OnGUI()
        {
            if (GUI.Button(new Rect(Screen.width-130, 18, 110, 32), "⚙ Settings")) open=!open;
            if(!open)return; var s=GameSettingsService.Instance; if(s==null)return;
            GUI.Box(new Rect(Screen.width/2-190,90,380,310),"Settings");
            GUI.Label(new Rect(Screen.width/2-160,135,130,22),"Master"); s.SetMaster(GUI.HorizontalSlider(new Rect(Screen.width/2-20,140,140,20),s.Master,0,1));
            GUI.Label(new Rect(Screen.width/2-160,180,130,22),"Music"); s.SetMusic(GUI.HorizontalSlider(new Rect(Screen.width/2-20,185,140,20),s.Music,0,1));
            GUI.Label(new Rect(Screen.width/2-160,225,130,22),"Effects"); s.SetEffects(GUI.HorizontalSlider(new Rect(Screen.width/2-20,230,140,20),s.Effects,0,1));
            s.SetFullscreen(GUI.Toggle(new Rect(Screen.width/2-160,270,150,22),s.Fullscreen,"Fullscreen")); s.SetVSync(GUI.Toggle(new Rect(Screen.width/2+10,270,120,22),s.VSync,"VSync"));
            if(GUI.Button(new Rect(Screen.width/2-160,315,140,28),"Русский")) LocalizationService.Instance.SetLanguage("ru"); if(GUI.Button(new Rect(Screen.width/2+20,315,140,28),"English")) LocalizationService.Instance.SetLanguage("en");
            if(GUI.Button(new Rect(Screen.width/2-60,360,120,25),"Close"))open=false;
        }
    }
}
