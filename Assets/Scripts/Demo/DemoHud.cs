using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace FlyMeToTheMoon.Demo
{
    public sealed class DemoHud : MonoBehaviour
    {
        private DemoGame game;
        private RectTransform root, menu, rhythm, board, map;
        private TextMeshProUGUI objective, status, toast, menuTitle, menuBody, rhythmTitle, rhythmScore;
        private readonly UnityEngine.UI.Button[] buttons = new UnityEngine.UI.Button[4];
        private readonly RectTransform[] notes = new RectTransform[DemoRhythm.NoteCount];
        private RectTransform mapPlayer;
        private readonly System.Collections.Generic.Dictionary<DemoWorldLabel, TextMeshProUGUI> captions = new System.Collections.Generic.Dictionary<DemoWorldLabel, TextMeshProUGUI>();
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
            foreach (var label in FindObjectsByType<DemoWorldLabel>(FindObjectsInactive.Include))
                captions.Add(label, Text(root, "World caption", new Vector2(.5f,.5f), Vector2.zero, new Vector2(240,55), 16));
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var events = new GameObject("Demo input", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            var left = Panel(root, "Quest HUD", new Vector2(0,1), new Vector2(18,-18), new Vector2(510,125), background, false);
            objective = Text(left, "Objective", new Vector2(0.5f,0.5f), Vector2.zero, new Vector2(478,105), 19, TextAlignmentOptions.MidlineLeft);
            var right = Panel(root, "Status HUD", new Vector2(1,1), new Vector2(-18,-18), new Vector2(360,100), background, false);
            status = Text(right, "Status", new Vector2(0.5f,0.5f), Vector2.zero, new Vector2(332,85), 18, TextAlignmentOptions.MidlineRight);
            var control = Panel(root, "Controls", new Vector2(0.5f,0), new Vector2(0,14), new Vector2(1160,70), background, false);
            Text(control, "Keys", new Vector2(0.5f,0.5f), Vector2.zero, new Vector2(1130,60), 17).text =
                "A / D walk around a world   |   Space jump, then Space to boost   |   WASD steer in space\nMouse: aim + shoot   |   E interact   |   1-4 play notes   |   Tab instrument   |   M map   |   R recover   |   Esc pause";
            toast = Text(root, "Message", new Vector2(0.5f,0), new Vector2(0,125), new Vector2(970,70), 20);
            toast.color = new Color(1,0.9f,0.58f);
            explorationHud.Add(left.gameObject); explorationHud.Add(right.gameObject);
            explorationHud.Add(control.gameObject); explorationHud.Add(toast.gameObject);
            BuildMap(); BuildRhythm(); BuildMenu();
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
            rhythm.gameObject.SetActive(game.State == DemoState.Rhythm);
            map.gameObject.SetActive(game.MapVisible && game.State == DemoState.Explore);
            if (!modal) return;
            string audio = game.Audio.Muted ? "Sound: OFF - turn on" : "Sound: ON - mute";
            foreach (var button in buttons) button.gameObject.SetActive(false);
            if (game.State == DemoState.Title)
            {
                menuTitle.text = "FLY ME TO THE MOON";
                menuBody.text = "The galaxy has lost its melody.\nTake your flute from 612-B to five silent moons.\nFind 3 pages, echo the resonators, and perform each score.\n\nPlaceholder visuals. Complete local gameplay demo.\nSession progress resets when you start a new journey.";
                Bind(0,"Start journey  [Enter]",game.StartGame);
                Bind(1,audio,game.ToggleAudio); Bind(2,"Quit",game.Quit);
            }
            else if (game.State == DemoState.Paused)
            {
                menuTitle.text = "PAUSED";
                menuBody.text = "Your fragments and instruments are safe.\nIf you paused a performance, Resume starts it again with a count-in.\n\nSurface: A/D + Space   |   Flight: WASD\nShoot: mouse   |   Interact: E   |   Recover: R";
                Bind(0,"Resume  [Esc]",game.Resume); Bind(1,audio,game.ToggleAudio);
                Bind(2,"New journey (resets progress)",game.Restart); Bind(3,"Quit",game.Quit);
            }
            else if (game.State == DemoState.Win)
            {
                menuTitle.text = "THE GALAXY SINGS AGAIN";
                menuBody.text = "Five moons restored. Six instruments to play.\nYou brought music back to 612-B and its neighbors.\n\nKeep exploring, switch instruments with Tab,\nand play your own melody with 1 / 2 / 3 / 4.";
                Bind(0,"Keep exploring  [Enter]",game.Continue); Bind(1,"New journey",game.Restart);
                Bind(2,audio,game.ToggleAudio); Bind(3,"Quit",game.Quit);
            }
            else
            {
                menuTitle.text = game.LastPassed ? game.Performing.Instrument.ToUpperInvariant() + " AWAKENED" : "ONE MORE TRY";
                menuBody.text = game.Rhythm.Hits + "/24 notes hit  |  " + game.Rhythm.Perfect + " perfect\nNeed 17 hits (70%) to awaken the instrument.\n\n" +
                    (game.LastPassed ? "The moon sings again! Your new instrument is equipped.\nNext destination: " + game.ActiveQuest.Instrument + ". Use M for the route." : "Your three music pages are still collected.\nPress A / S / D / F as notes cross the line.");
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
            Text(rhythm,"Rhythm help",new Vector2(0.5f,0),new Vector2(0,30),new Vector2(1120,40),17).text = "Hit A / S / D / F when a note crosses the white line. 17/24 hits to pass. Esc pauses and restarts this attempt on resume.";
        }
        private void BuildMap()
        {
            map = Panel(root,"Route map",new Vector2(0.5f,0.5f),Vector2.zero,new Vector2(760,555),background,true);
            Text(map,"Map title",new Vector2(0.5f,1),new Vector2(0,-35),new Vector2(730,50),27).text = "ROUTE MAP  [M to close]";
            for (int i = 0; i < game.Player.gravityManager.bodies.Length; i++)
            {
                var body = game.Player.gravityManager.bodies[i];
                Vector2 position = body.Center * 7f;
                Panel(map,body.planetId,new Vector2(0.5f,0.5f),position,Vector2.one*(i==0?60:25),i==0?new Color(0.25f,0.5f,0.6f):DemoGame.Accent,false);
                Text(map,"World name",new Vector2(0.5f,0.5f),position+Vector2.down*30,new Vector2(140,35),16).text = i==0?"612-B":i+". "+DemoGame.Instruments[i];
            }
            mapPlayer = Panel(map,"You",new Vector2(0.5f,0.5f),Vector2.zero,Vector2.one*10,new Color(1,0.75f,0.2f),false);
            Text(map,"Route hint",new Vector2(0.5f,0),new Vector2(0,45),new Vector2(720,65),17).text = "Restore moons in numbered order. Gold square = you.\nWalk around 612-B beneath your destination, then double jump outward.\nThe map stays live. R returns to your last safe landing.";
        }
        private void Update()
        {
            if (game == null || game.Player == null) return;
            foreach (var pair in captions)
            {
                var label = pair.Key; var text = pair.Value;
                Vector3 screen = Camera.main.WorldToScreenPoint(label.transform.position);
                bool visible = game.State == DemoState.Explore && !game.MapVisible && label.gameObject.activeInHierarchy
                    && screen.z > 0 && screen.x > 30 && screen.x < Screen.width - 30 && screen.y > Screen.height * .14f && screen.y < Screen.height * .96f;
                text.gameObject.SetActive(visible);
                if (visible)
                {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, null, out var point);
                    text.rectTransform.anchoredPosition = point; text.text = label.Caption;
                }
            }
            var q = game.ActiveQuest;
            objective.text = q == null ? "ALL FIVE MOONS RESTORED\nTab: change instrument   |   1-4: play notes\nExplore freely, or Esc to start a new journey." :
                "NEXT: "+q.Instrument.ToUpperInvariant()+"  /  MOON "+(q.Index+1)+"\n"+
                (q.Fragments[0]?"[x]":"[ ]")+" Ruins page   "+(q.Fragments[1]?"[x]":"[ ]")+" Shoot page   "+(q.Fragments[2]?"[x]":"[ ]")+" Echo page\n"+
                (q.Count==3?"Return to the instrument altar and press E.":"Explore the surface. E: hear resonators (2 - 1 - 3).");
            string place = game.Player.gravityManager.CurrentBody != null ? game.Player.gravityManager.CurrentBody.planetId : "Space";
            string direction = "";
            if (q != null)
            {
                Vector2 d=q.Body.Center-game.Player.Body.position;
                string[] names={"E","NE","N","NW","W","SW","S","SE"};
                int compass=(Mathf.RoundToInt(Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg/45f)+8)%8;
                direction="  |  Next: "+names[compass]+" "+Mathf.Max(0,q.Body.SurfaceDistance(game.Player.Body.position)).ToString("0")+"m";
            }
            status.text=place+direction+"\n"+DemoGame.Instruments[game.Equipped]+" equipped  |  "+game.Completed+"/5 restored\n"+(game.Player.FluteUsed?"Flight - WASD thrust":"A/D surface movement")+"  |  M map";
            toast.text=game.Message;
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
            if(map.gameObject.activeSelf) mapPlayer.anchoredPosition=game.Player.Body.position*7f;
        }
    }
}
