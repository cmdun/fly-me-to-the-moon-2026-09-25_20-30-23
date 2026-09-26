using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace FlyMeToTheMoon.Demo
{
    public sealed class DemoHud : MonoBehaviour
    {
        private DemoGame game;
        private RectTransform root, menu, rhythm, board, map, dialogue, contextArrow;
        private TextMeshProUGUI dialogueTitle, dialogueBody, dialogueButtonText;
        private UnityEngine.UI.Button dialogueButton;
        private float dialogueStarted;
        public bool DialogueComplete => dialogueBody != null && dialogueBody.maxVisibleCharacters >= game.DialogueText.Length;
        private TextMeshProUGUI objective, status, toast, menuTitle, menuBody, rhythmTitle, rhythmScore;
        private readonly UnityEngine.UI.Button[] buttons = new UnityEngine.UI.Button[4];
        private readonly RectTransform[] notes = new RectTransform[DemoRhythm.NoteCount];
        private RectTransform mapPlayer;
        private RectTransform minimap, minimapPlayer;
        private RectTransform[] nearbyBodies, nearbyPages, mapPages;
        private float mapScale;
        private Sprite circleSprite;
        internal Sprite CircleSprite => circleSprite;
        private Texture2D circleTexture;
        public const float MinimapRange = 32;
        public static Vector2 MinimapOffset(Vector2 world, Vector2 player) => (world - player) * (105f / MinimapRange);
        public Vector2 MinimapPlayerPosition => minimapPlayer.anchoredPosition;
        public Vector2 MinimapBodyPosition(int index) => nearbyBodies[index].anchoredPosition;

        private readonly System.Collections.Generic.List<GameObject> explorationHud = new System.Collections.Generic.List<GameObject>();
        private readonly Color background = new Color(0.025f, 0.045f, 0.085f, 0.95f);
        public void Build(DemoGame owner)
        {
            game = owner;
            var canvasGo = new GameObject("Demo HUD", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = 0.5f;
            root = (RectTransform)canvasGo.transform;
            contextArrow = Panel(root,"Nearby interaction",new Vector2(.5f,.5f),Vector2.zero,new Vector2(24,24),Color.clear,false);
            for(int i=0;i<2;i++)
            {
                var arm=Panel(contextArrow,"Arrow arm",new Vector2(.5f,.5f),new Vector2(i==0?-4:4,0),new Vector2(12,3),DemoGame.Accent,false);
                arm.localRotation=Quaternion.Euler(0,0,i==0?-45:45);
            }

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var events = new GameObject("Demo input", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            var left = Panel(root, "Journey HUD", new Vector2(0,1), new Vector2(20,-20), new Vector2(300,76), background, false);
            objective = Text(left,"Journey",new Vector2(.5f,.5f),Vector2.zero,new Vector2(272,64),19,TextAlignmentOptions.MidlineLeft);
            var right = Panel(root,"Instrument HUD",new Vector2(1,1),new Vector2(-20,-20),new Vector2(125,40),background,false);
            status=Text(right,"Instrument",new Vector2(.5f,.5f),Vector2.zero,new Vector2(110,34),18);
            toast=Text(root,"Collection",new Vector2(.5f,1),new Vector2(0,-45),new Vector2(300,35),18);
            toast.color=DemoGame.Accent;
            explorationHud.Add(left.gameObject); explorationHud.Add(right.gameObject); explorationHud.Add(toast.gameObject);
            BuildMap(); BuildMinimap(); BuildRhythm(); BuildMenu(); BuildDialogue();
            gameObject.AddComponent<DemoChallengeHud>().Build(game, root);
        }
        private RectTransform Panel(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, Color color, bool raycast)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = position; rect.sizeDelta = size;
            var image = go.GetComponent<UnityEngine.UI.Image>(); image.color = color; image.raycastTarget = raycast;
            return rect;
        }
        private RectTransform FullPanel(string name)
        {
            var rect = Panel(root, name, Vector2.zero, Vector2.zero, Vector2.zero, background, true);
            rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }
        private TextMeshProUGUI Text(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, float fontSize,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform; rect.anchorMin = rect.anchorMax = anchor; rect.pivot = new Vector2(0.5f,0.5f);
            rect.anchoredPosition = position; rect.sizeDelta = size;
            var text = go.AddComponent<TextMeshProUGUI>(); text.font = game.Font; text.fontSize = fontSize;
            text.color = Color.white; text.alignment = alignment; text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }
        private UnityEngine.UI.Button Button(Transform parent, string name, Vector2 position)
        {
            var rect = Panel(parent, name, new Vector2(0.5f,0.5f), position, new Vector2(380,46), new Color(0.12f,0.3f,0.35f), true);
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = rect.GetComponent<UnityEngine.UI.Image>();
            Text(rect, "Label", new Vector2(0.5f,0.5f), Vector2.zero, new Vector2(366,42), 21).text = name;
            return button;
        }
        private void BuildDialogue()
        {
            dialogue=Panel(root,"Conversation",new Vector2(.5f,0),new Vector2(0,35),new Vector2(900,230),background,true);
            dialogueTitle=Text(dialogue,"Speaker",new Vector2(.5f,1),new Vector2(0,-30),new Vector2(840,35),24);
            dialogueTitle.color=DemoGame.Accent;
            dialogueBody=Text(dialogue,"Typed words",new Vector2(.5f,.5f),new Vector2(0,5),new Vector2(820,115),22,TextAlignmentOptions.TopLeft);
            dialogueButton=Button(dialogue,"Continue",new Vector2(0,-83));
            dialogueButtonText=dialogueButton.GetComponentInChildren<TextMeshProUGUI>();
            dialogueButton.onClick.AddListener(game.AdvanceDialogue);
        }
        public void BeginDialogue()
        {
            dialogueTitle.text=game.DialogueTitle; dialogueBody.text=game.DialogueText;
            dialogueBody.maxVisibleCharacters=0; dialogueStarted=Time.unscaledTime;
            dialogueButtonText.text="Reveal";
        }
        public void RevealDialogue() { dialogueBody.maxVisibleCharacters=int.MaxValue; dialogueButtonText.text=game.DialogueChoice; }
        private void BuildMenu()
        {
            menu = FullPanel("Menu");
            menuTitle = Text(menu, "Heading", new Vector2(0.5f,0.5f), new Vector2(0,230), new Vector2(1100,80), 42);
            menuTitle.color = DemoGame.Accent;
            menuBody = Text(menu, "Body", new Vector2(0.5f,0.5f), new Vector2(0,95), new Vector2(950,185), 22);
            for (int i = 0; i < buttons.Length; i++) buttons[i] = Button(menu, "Action " + i, new Vector2(0,-55-i*58));
        }
        private void Bind(int index, string text, UnityEngine.Events.UnityAction action)
        {
            buttons[index].gameObject.SetActive(true);
            buttons[index].GetComponentInChildren<TextMeshProUGUI>().text = text;
            buttons[index].onClick.RemoveAllListeners(); buttons[index].onClick.AddListener(action);
        }
        public void RefreshPanels()
        {
            if (menu == null) return;
            foreach (var element in explorationHud) element.SetActive(game.PlayingWorld && !game.MapVisible);
            bool modal = game.State == DemoState.Title || game.State == DemoState.Paused || game.State == DemoState.Result || game.State == DemoState.Win;
            menu.gameObject.SetActive(modal);
            dialogue.gameObject.SetActive(game.State == DemoState.Dialogue);
            rhythm.gameObject.SetActive(game.State == DemoState.Rhythm);
            map.gameObject.SetActive(game.MapVisible && game.PlayingWorld);
            if (!modal) return;
            menuBody.fontSize = game.State == DemoState.Paused ? 19 : 22;
            string audio = game.Audio.Muted ? "Sound: OFF - turn on" : "Sound: ON - mute";
            foreach (var button in buttons) button.gameObject.SetActive(false);
            if (game.State == DemoState.Title)
            {
                menuTitle.text = "FLY ME TO THE MOON";
                menuBody.text = "Find the scattered score. Bring the melody home.";
                Bind(0,"Start journey  [Enter]",game.StartGame);
                Bind(1,audio,game.ToggleAudio); Bind(2,"Quit",game.Quit);
            }
            else if (game.State == DemoState.Paused)
            {
                menuTitle.text = "PAUSED";
                menuBody.text = "A/D walk   |   Space jump, then Space to boost   |   WASD steer\nLeft click: shoot notes   |   Right click: airborne recoil (uses boost)\nE: interact / advance dialogue   |   1-7: C D E F G A B\nTab: instrument (land first)   |   M: map   |   R: recover\nEncounters: Q call, C record, E act, R retry   |   Rhythm: A/S/D/F\n\nFind 5 challenge pages on the moons; repair and perform at the home altar.";
                Bind(0,"Resume  [Esc]",game.Resume); Bind(1,audio,game.ToggleAudio);
                Bind(2,"New journey (resets progress)",game.Restart); Bind(3,"Quit",game.Quit);
            }
            else if (game.State == DemoState.Win)
            {
                menuTitle.text = "THE MELODY IS HOME";
                menuBody.text = "The piano is yours.\nKeep exploring. Keep playing.";
                Bind(0,"Keep exploring  [Enter]",game.Continue); Bind(1,"New journey",game.Restart);
                Bind(2,audio,game.ToggleAudio); Bind(3,"Quit",game.Quit);
            }
            else
            {
                menuTitle.text = game.LastPassed ? game.Performing.Instrument.ToUpperInvariant() + " AWAKENED" : "ONE MORE TRY";
                menuBody.text = game.Rhythm.Hits + "/24 notes  |  " + game.Rhythm.Perfect + " perfect\n" +
                    (game.LastPassed ? "The piano is yours." : "17 hits restore the melody. Your score is safe.");
                if (!game.LastPassed) Bind(0,"Retry performance  [R]",()=>game.BeginRhythm(game.Performing));
                else Bind(0,"Continue journey  [Enter]",game.Continue);
                Bind(1,"Explore  [Enter]",game.Continue); Bind(2,"New journey",game.Restart); Bind(3,"Quit",game.Quit);
            }
        }
        private void BuildRhythm()
        {
            rhythm = FullPanel("Rhythm performance");
            rhythmTitle = Text(rhythm, "Song", new Vector2(0.5f,1), new Vector2(0,-65), new Vector2(1000,90), 26);
            board = Panel(rhythm, "Note lanes", new Vector2(0.5f,0.5f), new Vector2(0,-10), new Vector2(560,380), new Color(0.055f,0.09f,0.15f), false);
            for (int i=0; i<4; i++)
            {
                Panel(board,"Lane",new Vector2(0.5f,0.5f),new Vector2((i-1.5f)*120,20),new Vector2(106,315),new Color(0.1f,0.16f,0.23f),false);
                Text(board,"Key",new Vector2(0.5f,0.5f),new Vector2((i-1.5f)*120,-162),new Vector2(100,50),30).text = new[]{"A","S","D","F"}[i];
            }
            Panel(board,"Hit line",new Vector2(0.5f,0.5f),new Vector2(0,-120),new Vector2(500,4),Color.white,false);
            for(int i=0;i<notes.Length;i++)
                notes[i] = Panel(board,"Note "+i,new Vector2(0.5f,0.5f),Vector2.zero,new Vector2(98,16),DemoGame.Accent,false);
            rhythmScore = Text(rhythm,"Judgment",new Vector2(0.5f,0),new Vector2(0,100),new Vector2(1100,100),25);
            Text(rhythm,"Rhythm help",new Vector2(0.5f,0),new Vector2(0,30),new Vector2(1120,40),17).text = "17 / 24";
        }
        private RectTransform Circle(Transform parent, string name, Vector2 position, float diameter, Color color)
        {
            if (circleSprite == null)
            {
                circleTexture = new Texture2D(64,64,TextureFormat.RGBA32,false);
                for(int y=0;y<64;y++) for(int x=0;x<64;x++)
                    circleTexture.SetPixel(x,y,Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(32,32))<=31.5f?Color.white:Color.clear);
                circleTexture.Apply(); circleSprite=Sprite.Create(circleTexture,new Rect(0,0,64,64),Vector2.one*.5f);
            }
            var rect=Panel(parent,name,Vector2.one*.5f,position,Vector2.one*diameter,color,false);
            rect.GetComponent<UnityEngine.UI.Image>().sprite=circleSprite;return rect;
        }
        private void BuildMap()
        {
            map = Panel(root,"Route map",Vector2.one*.5f,Vector2.zero,new Vector2(900,620),background,true);
            Text(map,"Map title",new Vector2(.5f,1),new Vector2(0,-30),new Vector2(830,45),25).text="THE SCATTERED SCORE";
            var bodies=game.Player.gravityManager.bodies;
            float extent=1;
            foreach(var body in bodies)extent=Mathf.Max(extent,(body.Center-game.Planets.respawnPlanet.Center).magnitude+body.radius);
            mapScale=235f/extent;
            for(int i=0;i<bodies.Length;i++)
            {
                var body=bodies[i];Vector2 position=(body.Center-game.Planets.respawnPlanet.Center)*mapScale;
                Circle(map,body.planetId,position,body.radius*2*mapScale,i==0?new Color(.2f,.4f,.5f):new Color(.3f,.4f,.55f));
                Text(map,"World name",Vector2.one*.5f,position,new Vector2(55,20),12).text=i==0?"612-B":i.ToString();
            }
            mapPages=new RectTransform[DemoQuest.FragmentCount];
            for(int i=0;i<mapPages.Length;i++)
                mapPages[i]=Panel(map,"Page "+(i+1),Vector2.one*.5f,((Vector2)game.Melody.Stations[i].transform.position-game.Planets.respawnPlanet.Center)*mapScale,new Vector2(8,11),new Color(1,.85f,.35f),false);
            mapPlayer=Circle(map,"You",Vector2.zero,9,Color.white);
            Text(map,"Legend",new Vector2(.5f,0),new Vector2(0,27),new Vector2(820,40),17).text="Gold: score challenges   |   White: you   |   Altar: 612-B   |   M: close";
            Text(map,"North",new Vector2(1,1),new Vector2(-35,-65),new Vector2(40,35),19).text="N";
        }
        private void BuildMinimap()
        {
            var frame=Panel(root,"Nearby map",new Vector2(1,0),new Vector2(-20,20),new Vector2(230,250),background,false);
            Text(frame,"Fixed north",new Vector2(.5f,1),new Vector2(0,-15),new Vector2(200,24),15).text="N";
            minimap=Panel(frame,"Clipped nearby worlds",new Vector2(.5f,.5f),new Vector2(0,-10),new Vector2(210,210),new Color(.04f,.07f,.12f),false);
            minimap.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            var bodies=game.Player.gravityManager.bodies;nearbyBodies=new RectTransform[bodies.Length];
            for(int i=0;i<bodies.Length;i++)nearbyBodies[i]=Circle(minimap,bodies[i].planetId,Vector2.zero,bodies[i].radius*2*105/MinimapRange,i==0?new Color(.2f,.5f,.6f):new Color(.4f,.46f,.65f));
            nearbyPages=new RectTransform[DemoQuest.FragmentCount];
            for(int i=0;i<nearbyPages.Length;i++)nearbyPages[i]=Panel(minimap,"Nearby page "+i,Vector2.one*.5f,Vector2.zero,new Vector2(7,10),new Color(1,.85f,.35f),false);
            minimapPlayer=Circle(minimap,"Player center",Vector2.zero,7,Color.white);
            explorationHud.Add(frame.gameObject);
        }
        private void UpdateMaps()
        {
            Vector2 player=game.Player.Body.position;
            for(int i=0;i<nearbyBodies.Length;i++)nearbyBodies[i].anchoredPosition=MinimapOffset(game.Player.gravityManager.bodies[i].Center,player);
            for(int i=0;i<nearbyPages.Length;i++)
            {
                nearbyPages[i].anchoredPosition=MinimapOffset(game.Melody.Stations[i].transform.position,player);
                nearbyPages[i].gameObject.SetActive(!game.Melody.Fragments[i]);mapPages[i].gameObject.SetActive(!game.Melody.Fragments[i]);
            }
            mapPlayer.anchoredPosition=(player-game.Planets.respawnPlanet.Center)*mapScale;
        }
        private void OnDestroy()
        {
            if(circleSprite!=null)Destroy(circleSprite);if(circleTexture!=null)Destroy(circleTexture);
        }
        private void Update()
        {
            if (game == null || game.Player == null) return;
            bool exploring = game.State == DemoState.Explore && !game.MapVisible;
            contextArrow.gameObject.SetActive(exploring && game.NearbyTarget != null);
            if(contextArrow.gameObject.activeSelf)
            {
                Vector3 screen=Camera.main.WorldToScreenPoint(game.NearbyTarget.transform.position);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(root,screen,null,out var point);
                contextArrow.anchoredPosition=point+Vector2.up*(52+Mathf.Sin(Time.unscaledTime*4)*3);
            }
            var q=game.Melody;
            string place=game.Player.gravityManager.CurrentBody != null ? game.Player.gravityManager.CurrentBody.planetId : "Space";
            objective.text=place+"\n"+(q.Unlocked?"Melody restored":game.MelodyRepaired?"Score repaired": "Score "+q.Count+" / 5");
            status.text=DemoGame.Instruments[game.Equipped]; toast.text=game.State==DemoState.Challenge?"":game.Message;
            if(game.State==DemoState.Dialogue && !DialogueComplete)
            {
                dialogueBody.maxVisibleCharacters=Mathf.FloorToInt((Time.unscaledTime-dialogueStarted)*38);
                dialogueButtonText.text=DialogueComplete?game.DialogueChoice:"Reveal";
            }
            if (game.State==DemoState.Rhythm)
            {
                double elapsed=game.SongTime;
                rhythmTitle.text=game.Performing.Instrument+" / RESTORE THE MELODY\n"+(elapsed<DemoRhythm.CountIn?"Count in: "+Mathf.CeilToInt((float)(DemoRhythm.CountIn-elapsed)):"A     S     D     F");
                rhythmScore.text=game.Feedback+"\n"+game.Rhythm.Hits+"/24 hits   "+game.Rhythm.Perfect+" perfect   "+game.Rhythm.Misses+" missed";
                for(int i=0;i<notes.Length;i++)
                {
                    float y=-120+(float)(game.Rhythm.TimeOf(i)-elapsed)*110;
                    notes[i].gameObject.SetActive(game.Rhythm.Results[i]==0 && y>=-145 && y<=175);
                    notes[i].anchoredPosition=new Vector2((game.Rhythm.Lane(i)-1.5f)*120,y);
                }
            }
            UpdateMaps();
        }
    }
}
