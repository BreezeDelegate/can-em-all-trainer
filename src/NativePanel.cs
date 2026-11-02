using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using Il2Cpp;
using Il2CppInterop.Runtime;
using Il2CppTMPro;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace CanEmAllMod
{
    // Native Unity UI. No GUI / OnGUI / IMGUI calls are used to build, render,
    // or interact with this panel. All text uses the game's original TMP font.
    internal static class NativePanel
    {
        [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int code);
        [DllImport("user32.dll")] private static extern bool OpenClipboard(IntPtr owner);
        [DllImport("user32.dll")] private static extern bool CloseClipboard();
        [DllImport("user32.dll")] private static extern IntPtr GetClipboardData(uint kind);
        [DllImport("kernel32.dll")] private static extern IntPtr GlobalLock(IntPtr handle);
        [DllImport("kernel32.dll")] private static extern bool GlobalUnlock(IntPtr handle);

        private static readonly CultureInfo I=CultureInfo.InvariantCulture;
        private static readonly Color White=new Color(.96f,.96f,.96f,1f);
        private static readonly Color TextGray=new Color(.66f,.66f,.66f,1f);
        private static readonly Color Black=new Color(.055f,.055f,.055f,1f);
        private static readonly Color Surface=new Color(.14f,.14f,.14f,1f);
        private static readonly Color Hover=new Color(.28f,.28f,.28f,1f);
        // Single restrained accent, taken from the game's playful palette.
        private static readonly Color Accent=new Color(.41f,.78f,.62f,1f);
        private static readonly string[] Tabs={"JOUEUR","CREATURES","CANETTES","ARMES","CHANCES","MONDE","PROGRESSION","SAUVEGARDE","CASINO"};
        private static readonly int[] MoneyKeys={0x08,0x09,0x0D,0x1B,0x2e,0x41,0x56,
            0x30,0x31,0x32,0x33,0x34,0x35,0x36,0x37,0x38,0x39,
            0x60,0x61,0x62,0x63,0x64,0x65,0x66,0x67,0x68,0x69};
        private static readonly bool[] KeyDown=new bool[256];
        private static GameObject root, content, createdEventSystem;
        private static TMP_FontAsset gameFont;
        private static readonly Dictionary<string,Sprite> gameSprites=
            new Dictionary<string,Sprite>(StringComparer.OrdinalIgnoreCase);
        private static TextMeshProUGUI statusLabel, moneyLabel, subtitleLabel, closeCaption;
        // Preserve managed delegates for the entire lifetime of the native panel.
        // Unity's IL2CPP UnityEvent may otherwise call a collected delegate.
        private static readonly List<object> callbackRoots=new List<object>();
        private static readonly List<object> pageCallbacks=new List<object>();
        private static readonly List<object> retiredCallbacks=new List<object>();
        private static readonly List<Button> navigationButtons=new List<Button>();
        private static readonly List<Image> navigationImages=new List<Image>();
        private static readonly List<TextMeshProUGUI> navigationLabels=new List<TextMeshProUGUI>();
        private static bool constructed,open,needsBuild,moneyEditing,smokeMode,buildingPage,argsChecked,localeSmokeTested;
        private static float retireCallbacksAt;
        private static int smokeLimit=9;
        private static int boundScene=int.MinValue,tab,enemyIndex,weaponIndex,page;
        private static int smokeStep;
        private static float nextAttempt, nextRefresh, nextLog;
        private static string moneyInput="",status="";
        private static bool researchConfirm,killConfirm,worldEquipment;
        private static Action closeMenu;

        internal static void Tick(bool visible,Action close)
        {
            closeMenu=close;
            if(!argsChecked)
            {
                argsChecked=true;
                foreach(string arg in Environment.GetCommandLineArgs())
                {
                    if(arg=="-trainer-ui-smoke"){smokeMode=true;smokeLimit=9;break;}
                    if(arg=="-trainer-ui-stress"){smokeMode=true;smokeLimit=180;break;}
                    if(arg=="-trainer-ui-locales"){smokeMode=true;smokeLimit=18;break;}
                }
            }
            bool wantsVisible=visible||smokeMode&&smokeStep<=smokeLimit;
            if(!constructed && wantsVisible && Time.realtimeSinceStartup>=nextAttempt)
            {
                nextAttempt=Time.realtimeSinceStartup+2f;
                Initialize();
            }
            if(!constructed)return;
            if(root==null){constructed=false;return;}
            try
            {
                var manager=GameManager.Instance;
                if(manager!=null && manager.GetInstanceID()!=boundScene)
                {
                    Destroy();
                    return;
                }
                open=wantsVisible;
                if(root.activeSelf!=open)root.SetActive(open);
                if(!open)return;
                if(EventSystem.current!=null && !EventSystem.current.enabled)EventSystem.current.enabled=true;
                if(!localeSmokeTested && Array.IndexOf(Environment.GetCommandLineArgs(),"-trainer-ui-locales")>=0)
                {
                    localeSmokeTested=true;
                    try
                    {
                        SetLanguage("fr");
                        if(navigationLabels[0].text!="JOUEUR")MelonLogger.Error("NATIVE_LOCALE_FR_MISMATCH");
                        MelonLogger.Msg("NATIVE_LOCALE_FR="+navigationLabels[0].text);
                        SetLanguage("en");
                        if(navigationLabels[0].text!="PLAYER")MelonLogger.Error("NATIVE_LOCALE_EN_MISMATCH");
                        MelonLogger.Msg("NATIVE_LOCALE_EN="+navigationLabels[0].text);
                        SetLanguage("fr");
                        SetLanguage("en");
                        MelonLogger.Msg("NATIVE_LOCALE_SWITCHES_OK=4");
                    }
                    catch(Exception ex){MelonLogger.Error("NATIVE_LOCALE_TEST_FAILED "+ex);}
                }
                if(needsBuild)BuildPage();
                if(moneyEditing)ReadMoneyKeys();
                if(Time.realtimeSinceStartup>nextRefresh)
                {
                    nextRefresh=Time.realtimeSinceStartup+.5f;
                    if(statusLabel!=null)statusLabel.text=Localization.Tr(string.IsNullOrEmpty(status)
                        ?"TOUCHE ²  /  REGLAGES SAUVEGARDES AUTOMATIQUEMENT":status);
                }
                if(retiredCallbacks.Count>0 && Time.realtimeSinceStartup>=retireCallbacksAt)
                    retiredCallbacks.Clear();
                if(smokeMode && smokeStep<smokeLimit)
                {
                    int target=smokeStep++%Tabs.Length;
                    navigationButtons[target].onClick.Invoke();
                    if(tab!=target)MelonLogger.Error("NATIVE_UI_SMOKE_NAV_MISMATCH="+target);
                    needsBuild=true;
                }
                else if(smokeMode && smokeStep==smokeLimit)
                {
                    smokeStep=smokeLimit+1;
                    MelonLogger.Msg("NATIVE_UI_SMOKE_COMPLETE tab_count="+Tabs.Length+" navigations="+smokeLimit);
                    root.SetActive(false);
                }
            }
            catch(Exception ex)
            {
                if(Time.realtimeSinceStartup>nextLog)
                {
                    nextLog=Time.realtimeSinceStartup+5f;
                    MelonLogger.Error("Native UI update: "+ex);
                }
                // Keep game playable on a native UI failure.
                Destroy();
            }
        }
        internal static void Show(bool value)
        {
            open=value;
            moneyEditing=false;
            Array.Clear(KeyDown,0,KeyDown.Length);
            if(value){status="";needsBuild=true;}
            if(root!=null)root.SetActive(value);
            if(!value)TrainerSettings.Save();
        }
        internal static void Destroy()
        {
            constructed=false;
            open=false;
            moneyEditing=false;
            if(root!=null){root.SetActive(false);UnityEngine.Object.Destroy(root);}
            if(createdEventSystem!=null)UnityEngine.Object.Destroy(createdEventSystem);
            root=null;content=null;createdEventSystem=null;
            statusLabel=null;moneyLabel=null;subtitleLabel=null;closeCaption=null;
            gameFont=null;
            gameSprites.Clear();
            callbackRoots.Clear();
            pageCallbacks.Clear();
            retiredCallbacks.Clear();
            navigationButtons.Clear();
            navigationImages.Clear();
            navigationLabels.Clear();
            languageButtonBackgrounds.Clear();
            languageButtonLabels.Clear();
            buildingPage=false;
            boundScene=int.MinValue;
            needsBuild=false;
        }
        private static T Add<T>(GameObject g) where T:Component
        {
            var raw=g.AddComponent(Il2CppType.Of<T>());
            var component=raw==null?null:raw.TryCast<T>();
            if(component==null)throw new InvalidOperationException("uGUI component unavailable: "+typeof(T).Name);
            return component;
        }
        private static GameObject RectObject(string name,Transform parent,float x,float y,float w,float h)
        {
            var obj=new GameObject(name);
            obj.transform.SetParent(parent,false);
            var rect=Add<RectTransform>(obj);
            rect.anchorMin=new Vector2(.5f,.5f);
            rect.anchorMax=new Vector2(.5f,.5f);
            rect.pivot=new Vector2(.5f,.5f);
            rect.anchoredPosition=new Vector2(x,y);
            rect.sizeDelta=new Vector2(w,h);
            return obj;
        }
        private static Image Panel(Transform parent,string name,float x,float y,float w,float h,Color color,bool blocks=false)
        {
            var obj=RectObject(name,parent,x,y,w,h);
            var image=Add<Image>(obj);
            image.color=color;
            image.raycastTarget=blocks;
            return image;
        }
        private static void CollectGameSprites()
        {
            // The sprites already exist in the original game. No extraction,
            // CPU texture reads, Texture2D allocation or custom art is needed.
            // Only once per scene, never in a page redraw or callback.
            gameSprites.Clear();
            string[] names={
                "PABLO_hope","creature_icon","logo_food_corp","main_menu2",
                "can","can2","slotdragon","slotwater","slotplant","slotcorp",
                "moneyui","money","gun","ak47","Uzi","ChainSaw","bat","knife",
                "punch","suckyui","suckyui_selector","skilltreeicon","gamblingicon","settings"
            };
            try
            {
                var loaded=Resources.FindObjectsOfTypeAll(Il2CppType.Of<Sprite>());
                for(int i=0;i<loaded.Length;i++)
                {
                    var item=loaded[i];
                    if(item==null)continue;
                    string key=item.name;
                    if(Array.IndexOf(names,key)<0 && !Array.Exists(names,n=>string.Equals(n,key,StringComparison.OrdinalIgnoreCase)))
                        continue;
                    var sprite=item.TryCast<Sprite>();
                    if(sprite!=null && !gameSprites.ContainsKey(key))gameSprites[key]=sprite;
                }
                MelonLogger.Msg("NATIVE_GAME_SPRITES_READY="+gameSprites.Count);
            }
            catch(Exception ex){MelonLogger.Warning("Native sprites unavailable: "+ex.GetBaseException().Message);}
        }
        private static Image GameIcon(Transform parent,string name,string spriteName,
                                     float x,float y,float w,float h,Color? tint=null)
        {
            if(string.IsNullOrEmpty(spriteName) ||
               !gameSprites.TryGetValue(spriteName,out Sprite sprite) || sprite==null)return null;
            var img=Panel(parent,name,x,y,w,h,tint??White);
            img.sprite=sprite;
            img.preserveAspect=true;
            img.raycastTarget=false;
            return img;
        }
        private static string EnemyIcon(string enemyName)
        {
            string n=(enemyName??"").ToLowerInvariant();
            if(n.Contains("dragon"))return "slotdragon";
            if(n.Contains("water"))return "slotwater";
            if(n.Contains("plant"))return "slotplant";
            return "creature_icon";
        }
        private static string WeaponIcon(string weaponName)
        {
            string n=(weaponName??"").ToLowerInvariant();
            if(n.Contains("ak_")||n.Contains("ak47"))return "ak47";
            if(n.Contains("smg")||n.Contains("uzi"))return "Uzi";
            if(n.Contains("chain"))return "ChainSaw";
            if(n.Contains("baseball")||n.Contains("bat"))return "bat";
            if(n.Contains("knife"))return "knife";
            if(n.Contains("vacuum"))return "suckyui_selector";
            return "";
        }
        private static TextMeshProUGUI Text(Transform parent,string name,string text,float x,float y,float w,float h,
                                           int size=16,bool strong=false,Color? color=null,bool centered=false)
        {
            // Ordinary labels use x as their left edge; centered labels use x
            // as their actual center. This keeps all text within the panel.
            var go=RectObject(name,parent,centered?x:x+w*.5f,y,w,h);
            var label=Add<TextMeshProUGUI>(go);
            label.font=gameFont;
            label.text=Localization.Tr(text);
            label.fontSize=size;
            label.fontStyle=strong?FontStyles.Bold:FontStyles.Normal;
            label.color=color??White;
            label.alignment=centered?TextAlignmentOptions.Center:TextAlignmentOptions.MidlineLeft;
            label.raycastTarget=false;
            return label;
        }
        private static Button ActionButton(Transform parent,string name,string text,float x,float y,float w,float h,
                                           Action handler,bool light=false)
        {
            var image=Panel(parent,name,x,y,w,h,light?White:Surface,true);
            var button=Add<Button>(image.gameObject);
            button.targetGraphic=image;
            button.transition=Selectable.Transition.ColorTint;
            var cb=button.colors;
            cb.normalColor=Color.white;
            cb.highlightedColor=new Color(.82f,.82f,.82f,1);
            cb.pressedColor=new Color(.65f,.65f,.65f,1);
            button.colors=cb;
            TextMeshProUGUI caption=null;
            if(!string.IsNullOrEmpty(text))
            {
                caption=Text(image.transform,name+" label",text,0,0,w-10,h-4,13,true,light?Black:White,true);
                if(name=="Close")closeCaption=caption;
                caption.enableAutoSizing=true;
                caption.fontSizeMin=10;
                caption.fontSizeMax=13;
                caption.enableWordWrapping=false;
                caption.overflowMode=TextOverflowModes.Ellipsis;
            }
            if(name=="Locale EN"||name=="Locale FR")
            {
                languageButtonBackgrounds.Add(image);
                languageButtonLabels.Add(caption);
            }
            if(name.StartsWith("Tab ",StringComparison.Ordinal))
            {
                navigationImages.Add(image);
                navigationLabels.Add(caption);
            }
            var managed=new Action(()=>{try{handler();}catch(Exception ex){MelonLogger.Error("UI button "+name+": "+ex);Status("Action indisponible.");}});
            var native=DelegateSupport.ConvertDelegate<UnityEngine.Events.UnityAction>(managed);
            if(buildingPage){pageCallbacks.Add(managed);pageCallbacks.Add(native);}
            else {callbackRoots.Add(managed);callbackRoots.Add(native);}
            button.onClick.AddListener(native);
            return button;
        }
        private static Slider Range(Transform parent,string name,string title,float x,float y,float w,
                                    float low,float high,float initial,Action<float> onChange,string prefix="")
        {
            Text(parent,name+" caption",title,-420,y+10,240,28,15);
            var number=Text(parent,name+" value",prefix+initial.ToString("0.0",I),354,y+10,84,28,15,true,null,true);
            var go=RectObject(name+" slider",parent,55,y+7,475,32);
            var slider=Add<Slider>(go);
            slider.minValue=low;slider.maxValue=high;slider.wholeNumbers=false;
            Panel(go.transform,"Track",0,0,464,6,new Color(.35f,.35f,.35f,1));
            // No Image for Slider.fillRect: the former stretched white Fill
            // obscured entire UI rows. A thin track and a thumb are sufficient.
            var handle=Panel(go.transform,"Handle",0,0,16,23,Accent,true);
            slider.fillRect=null;
            slider.handleRect=handle.rectTransform;
            slider.targetGraphic=handle;
            slider.direction=Slider.Direction.LeftToRight;
            slider.SetValueWithoutNotify(initial);
            var managed=new Action<float>(v=>{
                try{onChange(v);number.text=prefix+v.ToString("0.0",I);}
                catch(Exception ex){MelonLogger.Error("UI slider "+name+": "+ex);}
            });
            var native=DelegateSupport.ConvertDelegate<UnityEngine.Events.UnityAction<float>>(managed);
            if(buildingPage){pageCallbacks.Add(managed);pageCallbacks.Add(native);}
            else {callbackRoots.Add(managed);callbackRoots.Add(native);}
            slider.onValueChanged.AddListener(native);
            return slider;
        }
        private static void Initialize()
        {
            GameObject created=null;
            try
            {
                var manager=GameManager.Instance;
                if(manager==null||manager.timerText==null||manager.timerText.font==null)return;
                gameFont=manager.timerText.font;
                boundScene=manager.GetInstanceID();
                CollectGameSprites();
                created=new GameObject("CanEmAll Native Trainer");
                var canvas=Add<Canvas>(created);
                var rootRaw=created.GetComponent(Il2CppType.Of<RectTransform>());
                var rootRect=rootRaw!=null?rootRaw.TryCast<RectTransform>():null;
                MelonLogger.Msg("NATIVE_UI_ROOT_RECT="+(rootRect!=null));
                canvas.renderMode=RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder=32000;
                Add<GraphicRaycaster>(created);
                var scaler=Add<CanvasScaler>(created);
                scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution=new Vector2(1600,900);
                scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight=.5f;
                root=created;
                Panel(root.transform,"Click shield",0,0,4096,4096,new Color(0,0,0,.72f),true);
                var window=Panel(root.transform,"Main window",0,0,1020,690,new Color(.063f,.063f,.063f,1),true);
                MelonLogger.Msg("NATIVE_UI_BACKGROUND_TEXTURE="+(window.mainTexture!=null));
                // Faint in-game menu artwork; keep it behind controls and text.
                // Removed: "main_menu2" showed stray orange strokes over tabs.
                GameIcon(window.transform,"Game emblem","PABLO_hope",-449,285,46,46);
                Text(window.transform,"Title","Can 'Em All",-411,289,380,48,26,true);
                subtitleLabel=Text(window.transform,"Subtitle","TRAINER  /  VERSION COMPLETE",-410,259,420,23,12,false,TextGray);
                ActionButton(window.transform,"Locale EN","EN",250,288,50,35,()=>{
                    SetLanguage("en");
                },Localization.English);
                ActionButton(window.transform,"Locale FR","FR",316,288,50,35,()=>{
                    SetLanguage("fr");
                },!Localization.English);
                ActionButton(window.transform,"Close","FERMER",421,288,115,37,()=>closeMenu?.Invoke());
                Panel(window.transform,"Divider",0,198,968,1,Hover);
                for(int i=0;i<Tabs.Length;i++)
                {
                    int index=i;
                    float tabWidth=106f;
                    var nav=ActionButton(window.transform,"Tab "+i,Tabs[i],-427+i*tabWidth,225,tabWidth-3,42,()=>{
                        tab=index;page=0;researchConfirm=false;killConfirm=false;worldEquipment=false;
                        moneyEditing=false;needsBuild=true;Status("");
                    });
                    navigationButtons.Add(nav);
                }
                Panel(window.transform,"Footer line",0,-287,968,1,Hover);
                statusLabel=Text(window.transform,"Status","TOUCHE ²  /  REGLAGES SAUVEGARDES AUTOMATIQUEMENT",
                                 -467,-312,935,27,12,false,TextGray);
                var co=RectObject("Page content",window.transform,0,-26,960,480);
                content=co;
                needsBuild=true;
                constructed=true;
                if(EventSystem.current==null)
                {
                    createdEventSystem=new GameObject("CanEmAll event system");
                    Add<EventSystem>(createdEventSystem);
                    Add<StandaloneInputModule>(createdEventSystem);
                }
                root.SetActive(false);
                MelonLogger.Msg("NATIVE_UI_READY canvas=1 tmp=1 tabs=9 event_system="+(EventSystem.current!=null));
            }
            catch(Exception ex)
            {
                MelonLogger.Error("Native UI initialization: "+ex);
                if(created!=null)UnityEngine.Object.Destroy(created);
                root=null;gameFont=null;
                callbackRoots.Clear();
                constructed=false;
            }
        }
        private static void Status(string msg)
        {
            status=msg;
            if(statusLabel!=null)statusLabel.text=Localization.Tr(string.IsNullOrEmpty(msg)
                ?"TOUCHE ²  /  REGLAGES SAUVEGARDES AUTOMATIQUEMENT":msg);
        }
        private static void SetLanguage(string language)
        {
            if(Localization.Language==language)return;
            Localization.Set(language);
            TrainerSettings.Save();
            for(int i=0;i<navigationLabels.Count;i++)
                if(navigationLabels[i]!=null)navigationLabels[i].text=Localization.Tr(Tabs[i]);
            if(subtitleLabel!=null)subtitleLabel.text=Localization.Tr("TRAINER  /  VERSION COMPLETE");
            if(closeCaption!=null)closeCaption.text=Localization.Tr("FERMER");
            // Refresh selected-state highlights without rebuilding the Canvas.
            UpdateLanguageButtons();
            Status(status);
            needsBuild=true;
        }
        private static readonly List<Image> languageButtonBackgrounds=new List<Image>();
        private static readonly List<TextMeshProUGUI> languageButtonLabels=new List<TextMeshProUGUI>();
        private static void UpdateLanguageButtons()
        {
            for(int i=0;i<languageButtonBackgrounds.Count && i<languageButtonLabels.Count;i++)
            {
                bool selected=i==0?Localization.English:!Localization.English;
                if(languageButtonBackgrounds[i]!=null)languageButtonBackgrounds[i].color=selected?White:Surface;
                if(languageButtonLabels[i]!=null)languageButtonLabels[i].color=selected?Black:White;
            }
        }
        private static void Head(string title,string sub="")
        {
            Text(content.transform,"Heading",title,-350,193,735,44,24,true);
            if(!string.IsNullOrEmpty(sub))Text(content.transform,"Description",sub,-350,157,790,28,13,false,TextGray);
        }
        private static void Switch(string name,float y,Func<bool> read,Action change)
        {
            bool enabled=read();
            Text(content.transform,name,name,-345,y,470,30,15);
            var toggle=ActionButton(content.transform,name+" Switch","",
                356,y,104,33,()=>{change();needsBuild=true;});
            // Flat, native two-state switch, no invented graphic glyphs.
            var image=toggle.targetGraphic;
            image.color=enabled?Accent:Hover;
            Panel(toggle.transform,"Toggle thumb",enabled?32:-32,0,34,25,White);
            Text(content.transform,name+" switch state",enabled?"OUI":"NON",
                265,y,66,30,13,true,enabled?Accent:TextGray,true);
        }
        private static int StepSize()
        {
            // Shift+click = 10, a simple discoverable acceleration.
            return (GetAsyncKeyState(0x10)&0x8000)!=0?10:1;
        }
        private static void Stepper(string name,float y,Func<int> read,Action<int> set,int low,int high)
        {
            Text(content.transform,name,name,-370,y,390,30,15);
            ActionButton(content.transform,name+" minus","-",214,y,45,35,()=>{
                set(Math.Max(low,read()-StepSize()));needsBuild=true;
            });
            Text(content.transform,name+" number",read().ToString(I),305,y,112,34,16,true,null,true);
            ActionButton(content.transform,name+" plus","+",414,y,45,35,()=>{
                set(Math.Min(high,read()+StepSize()));needsBuild=true;
            });
        }
        private static void PersistSlider(string key,string title,float y)
        {
            float original=TrainerSettings.Chances.TryGetValue(key,out float val)?val:.35f;
            float current=original;
            Text(content.transform,key+" head",title,-430,y,240,31,15);
            var output=Text(content.transform,key+" percent",
                TrainerSettings.Chances.ContainsKey(key)?(original*100f).ToString("0",I)+"%":"--",
                345,y,85,30,15,true,null,true);
            var sliderGo=RectObject(key+" range",content.transform,80,y,365,31);
            var slider=Add<Slider>(sliderGo);
            slider.minValue=0;slider.maxValue=1;
            var rail=Panel(sliderGo.transform,"Rail",0,0,341,6,new Color(.31f,.31f,.31f,1));
            // The native filled Image used to stretch into an opaque white
            // rectangle. Never attach a full-width Image to fillRect.
            var thumb=Panel(sliderGo.transform,"Thumb",0,0,16,23,Accent,true);
            slider.fillRect=null;slider.handleRect=thumb.rectTransform;
            slider.targetGraphic=thumb;
            slider.SetValueWithoutNotify(original);
            var managed=new Action<float>(v=>{
                current=v;
                TrainerSettings.SetChance(key,current);
                output.text=(current*100).ToString("0",I)+"%";
            });
            var native=DelegateSupport.ConvertDelegate<UnityEngine.Events.UnityAction<float>>(managed);
            if(buildingPage){pageCallbacks.Add(managed);pageCallbacks.Add(native);}
            else {callbackRoots.Add(managed);callbackRoots.Add(native);}
            slider.onValueChanged.AddListener(native);
        }
        private static void ActionResult(Func<string> call,bool refresh=false)
        {
            Status(call());
            if(refresh)needsBuild=true;
        }
        private static void BuildPage()
        {
            needsBuild=false;
            if(content==null)return;
            // Protect callbacks for objects scheduled to be destroyed at the
            // end of the frame before allowing the IL2CPP bridge to collect them.
            retiredCallbacks.AddRange(pageCallbacks);
            pageCallbacks.Clear();
            retireCallbacksAt=Time.realtimeSinceStartup+1f;
            buildingPage=true;
            try
            {
            // Il2Cpp Transform enumerators can yield generic Component wrappers.
            // GetChild(index) returns a strongly typed Transform and avoids that cast.
            for(int childIndex=content.transform.childCount-1;childIndex>=0;childIndex--)
            {
                var child=content.transform.GetChild(childIndex);
                if(child!=null)UnityEngine.Object.Destroy(child.gameObject);
            }
            switch(tab)
            {
                case 0:PlayerPage();break;
                case 1:CreaturesPage();break;
                case 2:CanPage();break;
                case 3:WeaponsPage();break;
                case 4:ChancesPage();break;
                case 5:WorldPage();break;
                case 6:ProgressPage();break;
                case 7:SavePage();break;
                case 8:CasinoPage();break;
            }
            for(int i=0;i<navigationImages.Count;i++)
            {
                bool active=i==tab;
                navigationImages[i].color=active?White:Surface;
                navigationLabels[i].color=active?Black:White;
            }
            if(smokeMode)MelonLogger.Msg("NATIVE_UI_SMOKE_TAB_"+tab+"="+Tabs[tab]);
            }
            finally{buildingPage=false;}
        }
        private static void PlayerPage()
        {
            Head("Joueur","Argent et modifications du personnage");
            var cash=MoneyManager.Instance;
            if(string.IsNullOrEmpty(moneyInput))
                moneyInput=(TrainerSettings.PinnedMoney??(cash!=null?cash.Money:0)).ToString(I);
            Text(content.transform,"Money caption","Argent",-410,95,220,30);
            ActionButton(content.transform,"Money clickable field","",145,95,550,43,()=>{
                moneyEditing=true;
                Array.Clear(KeyDown,0,KeyDown.Length);
                Status("Chiffres, Ctrl+A/V, Retour, Entree pour appliquer.");
                if(moneyLabel!=null)moneyLabel.text=moneyInput+"|";
            });
            moneyLabel=Text(content.transform,"Money text",moneyInput,145,95,533,39,19,true,null,true);
            ActionButton(content.transform,"Money apply","APPLIQUER",345,50,170,40,()=>{
                if(long.TryParse(moneyInput,NumberStyles.None,I,out long v))Status(NativeActions.SetMoney(v));
                else Status("Montant invalide.");
            },true);
            Switch("Endurance infinie",-6,()=>AdminPanel.InfiniteStamina,()=>AdminPanel.InfiniteStamina=!AdminPanel.InfiniteStamina);
            Range(content.transform,"Speed","Vitesse",-80,-73,840,1,5,AdminPanel.Speed,v=>AdminPanel.Speed=(float)Math.Round(v,1),"x");
            Range(content.transform,"Jump","Saut",-80,-129,840,1,5,AdminPanel.Jump,v=>AdminPanel.Jump=(float)Math.Round(v,1),"x");
            Range(content.transform,"Damage","Degats",-80,-185,840,1,10,AdminPanel.Damage,v=>AdminPanel.Damage=(float)Math.Round(v,1),"x");
        }
        private static void CreaturesPage()
        {
            Head("Creatures","Selectionner un modele disponible, puis le faire apparaitre.");
            var truck=Truck.Instance;
            var items=truck!=null?truck.allEnemyPrefabs:null;
            if(items==null||items.Count==0){Text(content.transform,"Empty","Aucun modele charge.",-320,65,600,45);return;}
            int count=items.Count;
            enemyIndex=Math.Max(0,Math.Min(enemyIndex,count-1));
            int pages=Math.Max(1,(count+6)/7);
            page=Math.Min(page,pages-1);
            for(int i=page*7;i<Math.Min(count,(page+1)*7);i++)
            {
                int index=i;
                var prefab=items[i];
                var enemyButton=ActionButton(content.transform,"Enemy "+i,NativeActions.Name(prefab),
                    -251,118-(i-page*7)*47,400,40,()=>{
                        enemyIndex=index;needsBuild=true;
                },enemyIndex==i);
                GameIcon(enemyButton.transform,"Creature art "+i,EnemyIcon(NativeActions.Name(prefab)),
                    -171,0,30,30);
            }
            Text(content.transform,"Selection","Selection : "+NativeActions.Name(items[enemyIndex]),100,118,340,38,15,true);
            ActionButton(content.transform,"Spawn enemy","FAIRE APPARAITRE",255,54,300,48,
                ()=>ActionResult(()=>NativeActions.SpawnEnemy(enemyIndex)),true);
            ActionButton(content.transform,"Previous","PREC.",-371,-226,125,34,()=>{
                if(page>0){page--;needsBuild=true;}
            });
            Text(content.transform,"Pages",(page+1)+" / "+pages,-233,-226,100,30,14);
            ActionButton(content.transform,"Next","SUIV.",-96,-226,125,34,()=>{
                if(page+1<pages){page++;needsBuild=true;}
            });
        }
        private static void CanPage()
        {
            Head("Canettes","Creer une canette standard a l'endroit vise.");
            GameIcon(content.transform,"Can art","can2",399,94,72,72);
            Text(content.transform,"Can description","Creer une canette standard devant le joueur.",
                0,83,730,36,16,false,White,true);
            Text(content.transform,"Can note","Les variantes dependent des machines et ameliorations du jeu.",
                0,46,800,30,14,false,TextGray,true);
            ActionButton(content.transform,"Spawn can","FAIRE APPARAITRE",0,-37,340,52,
                ()=>ActionResult(NativeActions.SpawnCan),true);

        }
        private static void WeaponsPage()
        {
            Head("Arsenal","Debloquer une arme precise, puis choisir les munitions.");
            var inv=Inventory.Instance;
            var items=inv!=null?inv.allWeapons:null;
            Switch("Reserve auto (armes a feu)",133,()=>AdminPanel.InfiniteAmmo,()=>AdminPanel.InfiniteAmmo=!AdminPanel.InfiniteAmmo);
            if(items==null||items.Count==0){Text(content.transform,"Empty","Arsenal indisponible.",-320,48,650,35);return;}
            int count=items.Count;
            weaponIndex=Math.Max(0,Math.Min(weaponIndex,count-1));
            int pages=Math.Max(1,(count+5)/6);
            page=Math.Min(page,pages-1);
            for(int i=page*6;i<Math.Min(count,(page+1)*6);i++)
            {
                int index=i;
                var weaponButton=ActionButton(content.transform,"Weapon "+i,NativeActions.Name(items[i]),
                   -249,79-(i-page*6)*49,400,42,()=>{
                       weaponIndex=index;needsBuild=true;
                   },weaponIndex==i);
                GameIcon(weaponButton.transform,"Weapon art "+i,WeaponIcon(NativeActions.Name(items[i])),
                    -170,0,30,30);
            }
            Text(content.transform,"Equipped",NativeActions.Name(items[weaponIndex]),100,70,335,36,16,true);
            ActionButton(content.transform,"Unlock weapon","DEBLOQUER / EQUIPER",250,15,310,42,
                ()=>ActionResult(()=>NativeActions.UnlockWeapon(weaponIndex)),true);
            ActionButton(content.transform,"Add ammo","+100 MUNITIONS",250,-41,310,42,
                ()=>ActionResult(()=>NativeActions.AddAmmo(weaponIndex,StepSize()==10?1000:100)));
            Text(content.transform,"Ammo step help","MAJ + CLIC : +1000",136,-75,300,21,12,false,TextGray);
            ActionButton(content.transform,"Refill all","RECHARGER TOUT",250,-118,310,42,
                ()=>ActionResult(NativeActions.Refill));
            ActionButton(content.transform,"Prev weapon","PREC.",-361,-233,115,35,()=>{
                if(page>0){page--;needsBuild=true;}
            });
            Text(content.transform,"Weapon pages",(page+1)+" / "+pages,-229,-233,90,27,13);
            ActionButton(content.transform,"Next weapon","SUIV.",-100,-233,115,35,()=>{
                if(page+1<pages){page++;needsBuild=true;}
            });
        }
        private static void ChancesPage()
        {
            Head("Probabilites","Valeurs du combat et des recompenses. -- : valeur d'origine.");
            string[] keys={"crit","instant","free","explosive","shock","bonus","shiny","double"};
            string[] names={"Coup critique","Mort instantanee","Balle gratuite","Balle explosive",
                "Electrocution","Canette bonus","Creature brillante","Recompense x2"};
            for(int i=0;i<keys.Length;i++)PersistSlider(keys[i],names[i],117-i*48);
        }
        private static void WorldPage()
        {
            if(worldEquipment){EquipmentPage();return;}
            Head("Monde","Duree des cycles et production.");
            ActionButton(content.transform,"Equipment view","EQUIPEMENT",337,174,195,33,()=>{
                worldEquipment=true;needsBuild=true;
            });
            var gm=GameManager.Instance;
            if(gm==null){Text(content.transform,"Empty","Aucune partie active.",-320,88,600,35);return;}
            Switch("Figer l'horloge",125,()=>AdminPanel.FreezeClock,()=>AdminPanel.FreezeClock=!AdminPanel.FreezeClock);
            Range(content.transform,"Conveyor","Convoyeur",-50,59,800,1,5,AdminPanel.Conveyor,
                v=>AdminPanel.Conveyor=(float)Math.Round(v,1),"x");
            Range(content.transform,"Ammo","Capacite munitions",-50,3,800,1,10,AdminPanel.Ammo,
                v=>AdminPanel.Ammo=(float)Math.Round(v,1),"x");
            Stepper("Duree jour (sec)",-57,()=>gm.dayDuration,
                v=>{gm.dayDuration=v;TrainerSettings.RecordDay(v);},30,3600);
            Stepper("Duree nuit (sec)",-102,()=>gm.nightShiftDuration,
                v=>{gm.nightShiftDuration=v;TrainerSettings.RecordNight(v);},15,1800);
            var truck=Truck.Instance;
            if(truck!=null)Stepper("Creatures / camion",-147,()=>truck.enemyCount,
                v=>{truck.enemyCount=v;TrainerSettings.RecordTruck(v);},1,60);
            Text(content.transform,"Stepper help","MAJ + CLIC : +/- 10",-355,-186,325,24,12,false,TextGray);
            if(killConfirm)
            {
                ActionButton(content.transform,"Confirm kill","CONFIRMER",231,-205,160,38,()=>{
                    killConfirm=false;ActionResult(NativeActions.KillAll,true);
                },true);
                ActionButton(content.transform,"Cancel kill","ANNULER",410,-205,126,38,()=>{
                    killConfirm=false;needsBuild=true;
                });
            }
            else ActionButton(content.transform,"Kill all","ELIMINER TOUS LES MONSTRES",318,-205,311,40,()=>{
                killConfirm=true;needsBuild=true;
            });
        }
        private static void EquipmentPage()
        {
            Head("Equipement","Ajustements d'armes automatiques et de production.");
            ActionButton(content.transform,"Back to world","RETOUR AU MONDE",336,174,200,35,()=>{
                worldEquipment=false;needsBuild=true;
            });
            Switch("Tourelles ravitaillees",112,()=>TrainerSettings.TurretAmmo,
                ()=>TrainerSettings.TurretAmmo=!TrainerSettings.TurretAmmo);
            Range(content.transform,"Vacuum capacity","Capacite du vacuum",0,45,800,1,10,
                TrainerSettings.VacuumCapacity,v=>TrainerSettings.VacuumCapacity=(float)Math.Round(v,1),"x");
            Range(content.transform,"Vacuum fire","Cadence du vacuum",0,-15,800,1,3,
                TrainerSettings.VacuumFire,v=>TrainerSettings.VacuumFire=(float)Math.Round(v,1),"x");
            Range(content.transform,"Flame range","Portee lance-flammes",0,-75,800,1,5,
                TrainerSettings.FlameRange,v=>TrainerSettings.FlameRange=(float)Math.Round(v,1),"x");
            Text(content.transform,"Equipment note",
                "Pas de rechargement force du lance-roquettes ni des armes de melee.",
                -389,-159,800,27,13,false,TextGray);
            Text(content.transform,"Equipment note 2",
                "Les options actives sont conservees entre les sessions.",
                -389,-189,800,27,13,false,TextGray);
        }
        private static void ProgressPage()
        {
            Head("Progression","Armes et arbre de recherche du jeu complet.");
            Text(content.transform,"Research hint","Debloquer les competences disponibles.",-222,85,650,34);
            if(researchConfirm)
            {
                ActionButton(content.transform,"Confirm research","CONFIRMER",-165,23,220,48,()=>{
                    researchConfirm=false;ActionResult(NativeActions.UnlockAllResearch,true);
                },true);
                ActionButton(content.transform,"Cancel research","ANNULER",165,23,220,48,()=>{
                    researchConfirm=false;needsBuild=true;
                });
            }
            else ActionButton(content.transform,"Unlock research","TOUT DEBLOQUER",-50,23,460,49,()=>{
                researchConfirm=true;needsBuild=true;
            },true);
            Text(content.transform,"Weapon hint","Debloquer toutes les armes.",-222,-92,650,32);
            ActionButton(content.transform,"All weapons","TOUTES LES ARMES",-50,-155,460,48,
                ()=>ActionResult(NativeActions.UnlockAllWeapons),true);
        }
        private static void SavePage()
        {
            Head("Sauvegarde","Consultation des statistiques de la partie, sans modification de fichier.");
            var save=SaveManager.Instance;
            var data=save!=null?save.CurrentSaveData:null;
            if(data==null){Text(content.transform,"Empty","Aucune sauvegarde chargee.",-320,70,620,35);return;}
            string[] labels={"Jour","Argent","Creatures vaincues","Canettes produites"};
            string[] values={data.day.ToString(I),data.money.ToString("N0",I),data.totalKilled.ToString(I),data.totalCans.ToString(I)};
            for(int i=0;i<labels.Length;i++)
            {
                Text(content.transform,"Save "+i,labels[i],-325,92-i*62,245,35);
                Text(content.transform,"Value "+i,values[i],241,92-i*62,285,35,17,true,null,true);
            }
        }
        private static void CasinoPage()
        {
            Head("Casino","Probabilites natives de la machine a sous.");
            GameIcon(content.transform,"Casino art","gamblingicon",407,113,43,43);
            if(!TrainerSettings.CasinoAvailable)
            {
                Text(content.transform,"Unavailable","Machine indisponible dans cette scene.",0,52,690,40,16,false,null,true);
                return;
            }
            float win=TrainerSettings.Chances.TryGetValue("casino.win",out float customWin)
                ?customWin:TrainerSettings.CasinoObservedWin;
            float near=TrainerSettings.Chances.TryGetValue("casino.near",out float customNear)
                ?customNear:TrainerSettings.CasinoObservedNear;
            PersistSlider("casino.win","Chance de gagner",94);
            PersistSlider("casino.near","Quasi-gain",10);
            Text(content.transform,"Machine state",TrainerSettings.CasinoSpinning?"Tirage en cours":"Machine au repos",
                -202,-87,500,38,15,false,TextGray);
            Text(content.transform,"Casino note","Les valeurs sont appliquees en dehors de l'interface.",
                -202,-134,700,30,13,false,TextGray);
        }
        private static string Clipboard()
        {
            try
            {
                if(!OpenClipboard(IntPtr.Zero))return "";
                try
                {
                    IntPtr h=GetClipboardData(13);
                    if(h==IntPtr.Zero)return "";
                    IntPtr p=GlobalLock(h);
                    if(p==IntPtr.Zero)return "";
                    try{return Marshal.PtrToStringUni(p)??"";}
                    finally{GlobalUnlock(h);}
                }
                finally{CloseClipboard();}
            }
            catch{return "";}
        }
        private static void ReadMoneyKeys()
        {
            bool ctrl=(GetAsyncKeyState(0x11)&0x8000)!=0;
            foreach(int vk in MoneyKeys)
            {
                bool down=(GetAsyncKeyState(vk)&0x8000)!=0;
                bool trigger=down&&!KeyDown[vk];
                KeyDown[vk]=down;
                if(!trigger)continue;
                if(vk==0x09||vk==0x1B){moneyEditing=false;break;}
                if(vk==0x0D)
                {
                    moneyEditing=false;
                    if(long.TryParse(moneyInput,NumberStyles.None,I,out long number))
                        Status(NativeActions.SetMoney(number));
                    else Status("Montant invalide.");
                    break;
                }
                if(vk==0x08)
                {
                    if(moneyInput.Length>0)moneyInput=moneyInput.Substring(0,moneyInput.Length-1);
                    continue;
                }
                if(vk==0x2e||ctrl&&vk==0x41){moneyInput="";continue;}
                if(ctrl&&vk==0x56)
                {
                    foreach(char ch in Clipboard())if(ch>='0'&&ch<='9'&&moneyInput.Length<13)moneyInput+=ch;
                    continue;
                }
                int digit=vk>=0x30&&vk<=0x39?vk-0x30:vk>=0x60&&vk<=0x69?vk-0x60:-1;
                if(!ctrl&&digit>=0&&moneyInput.Length<13)moneyInput+=digit.ToString(I);
            }
            if(moneyLabel!=null)moneyLabel.text=moneyInput+(moneyEditing?"|":"");
        }
    }
}
