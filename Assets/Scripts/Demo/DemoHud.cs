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
        private readonly TextMeshProUGUI[] resonatorNumbers = new TextMeshProUGUI[3];
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
            for(int i=0;i<3;i++) resonatorNumbers[i]=Text(root,"Resonator number",new Vector2(.5f,.5f),Vector2.zero,new Vector2(30,30),16);
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
            BuildMap(); BuildRhythm(); BuildMenu(); BuildDialogue();
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
            foreach (var element in explorationHud) element.SetActive(game.State == DemoState.Explore && !game.MapVisible);
            bool modal = game.State == DemoState.Title || game.State == DemoState.Paused || game.State == DemoState.Result || game.State == DemoState.Win;
            menu.gameObject.SetActive(modal);
            dialogue.gameObject.SetActive(game.State == DemoState.Dialogue);
            rhythm.gameObject.SetActive(game.State == DemoState.Rhythm);
            map.gameObject.SetActive(game.MapVisible && game.State == DemoState.Explore);
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
                menuBody.text = "A/D walk   |   Space jump, then Space to boost   |   WASD steer\nMouse: aim and shoot; in the air, recoil uses your second jump\nE: interact / advance dialogue   |   1-7: C D E F G A B\nTab: instrument (land first)   |   M: map   |   R: recover\nRhythm: A/S/D/F   |   A paused song restarts on resume\n\nFind 3 pages on the moons; repair and perform at the home altar.";
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
        private void BuildMap()
        {
            map = Panel(root,"Route map",new Vector2(0.5f,0.5f),Vector2.zero,new Vector2(760,555),background,true);
            Text(map,"Map title",new Vector2(0.5f,1),new Vector2(0,-35),new Vector2(730,50),27).text = "THE SCATTERED SCORE";
            for (int i = 0; i < game.Player.gravityManager.bodies.Length; i++)
            {
                var body = game.Player.gravityManager.bodies[i];
                Vector2 position = (body.Center-game.Planets.respawnPlanet.Center) * 6f;
                Panel(map,body.planetId,new Vector2(0.5f,0.5f),position,Vector2.one*(i==0?28:12),i==0?new Color(0.25f,0.5f,0.6f):DemoGame.Accent,false);
                Text(map,"World name",new Vector2(0.5f,0.5f),position+Vector2.down*18,new Vector2(80,24),13).text = i==0?"612-B":i.ToString();
            }
            mapPlayer = Panel(map,"You",new Vector2(0.5f,0.5f),Vector2.zero,Vector2.one*10,new Color(1,0.75f,0.2f),false);
            var q=game.Melody;
            Text(map,"Route hint",new Vector2(.5f,0),new Vector2(0,40),new Vector2(720,65),17).text =
                "Ruins: " + q.FragmentBodies[0].planetId + "   /   Floating page: " + q.FragmentBodies[1].planetId + "   /   Echo: " + q.FragmentBodies[2].planetId + "\nAltar: 612-B   /   Gold: you";
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
            for(int i=0;i<3;i++)
            {
                bool visible=exploring && Vector2.Distance(game.Player.Body.position,q.Pedestal)<3.2f && !q.PuzzleSolved;
                resonatorNumbers[i].gameObject.SetActive(visible);
                if(visible)
                {
                    Vector3 screen=Camera.main.WorldToScreenPoint(q.Resonators[i].transform.position);
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(root,screen,null,out var point);
                    resonatorNumbers[i].rectTransform.anchoredPosition=point+Vector2.up*17;
                    resonatorNumbers[i].text=(i+1).ToString();
                }
            }
            string place=game.Player.gravityManager.CurrentBody != null ? game.Player.gravityManager.CurrentBody.planetId : "Space";
            objective.text=place+"\n"+(q.Unlocked?"Melody restored":game.MelodyRepaired?"Score repaired": "Score "+q.Count+" / 3");
            status.text=DemoGame.Instruments[game.Equipped]; toast.text=game.Message;
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
            if(map.gameObject.activeSelf) mapPlayer.anchoredPosition=(game.Player.Body.position-game.Planets.respawnPlanet.Center)*6f;
        }
    }
}
