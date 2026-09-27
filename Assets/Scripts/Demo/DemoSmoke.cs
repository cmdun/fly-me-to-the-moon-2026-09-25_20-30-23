#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace FlyMeToTheMoon.Demo
{
    // Runs only with the explicit -demoSmoke argument. Normal play has no test controls.
    public sealed class DemoSmoke : MonoBehaviour
    {
        string output; int errors; Keyboard keyboard; Mouse mouse;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-demoSmoke");
            if(index>=0 && index+1<args.Length) new GameObject("Local validation").AddComponent<DemoSmoke>().output=args[index+1];
        }
        void Check(bool condition,string message){if(!condition){errors++;Debug.LogError("DEMO_SMOKE: "+message);}}
        IEnumerator Capture(string name){yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return null;}
        IEnumerator Press(params Key[] keys){InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));yield return null;yield return new WaitForFixedUpdate();}
        public static IEnumerator Keys(Keyboard keyboard, params Key[] keys)
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));yield return null;yield return new WaitForFixedUpdate();
        }
        public static IEnumerator Walk(DemoGame game, WorldEncounter encounter, Keyboard keyboard, float position, bool call=false)
        {
            float deadline=Time.time+14;
            while(game.State==DemoState.Challenge && Time.time<deadline)
            {
                float difference=encounter.ArcDelta(encounter.Along(game.Player.Body.position),position);
                if(Mathf.Abs(difference)<.3f)break;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(call?new[]{difference>0?Key.D:Key.A,Key.Q}:new[]{difference>0?Key.D:Key.A}));
                yield return null;
            }
            yield return Keys(keyboard,call?new[]{Key.Q}:new Key[0]);yield return new WaitForSeconds(.15f);
        }
        public static IEnumerator WalkSurface(DemoGame game, Keyboard keyboard, GravityBody body, Vector2 up)
        {
            float deadline=Time.time+20;
            while(Time.time<deadline)
            {
                float angle=-Vector2.SignedAngle(body.UpAt(game.Player.Body.position),up);
                if(Mathf.Abs(angle)*(body.radius+.29f)*Mathf.Deg2Rad<.3f)break;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(angle>0?Key.D:Key.A));yield return null;
            }
            yield return Keys(keyboard);yield return new WaitForSeconds(.15f);
        }
        public static IEnumerator Fire(DemoGame game, Mouse mouse, Vector2 position)
        {
            Vector2 screen=Camera.main.WorldToScreenPoint(position);
            InputSystem.QueueStateEvent(mouse,new MouseState{position=screen}.WithButton(MouseButton.Left));yield return null;yield return null;
            InputSystem.QueueStateEvent(mouse,new MouseState{position=screen});yield return new WaitForSeconds(.3f);
        }
        public static void AdvanceAll(DemoGame game)
        {
            for(int i=0;i<16 && game.State==DemoState.Dialogue;i++)game.AdvanceDialogue();
        }
        public static IEnumerator SolveChallenge(DemoGame g, Keyboard keyboard, Mouse mouse)
        {
            var c=g.Challenge;var encounter=c.Encounter;
            yield return new WaitForSeconds(.2f);
            if(encounter is EchoEncounter echo)
            {
                for(int stage=0;stage<2;stage++)
                {
                    yield return Keys(keyboard,Key.C);yield return Keys(keyboard);
                    yield return Walk(g,echo,keyboard,2.8f);
                    yield return Fire(g,mouse,g.Player.Body.position+g.Player.Up*5);
                    while(echo.Recording && echo.Clock<7)yield return null;
                    yield return Keys(keyboard,Key.E);yield return Keys(keyboard);
                    yield return Walk(g,echo,keyboard,8.1f);
                    float end=Time.time+12;
                    while(g.State==DemoState.Challenge && echo.Stage==stage && Time.time<end)
                    {
                        if(echo.GhostOnPlate)yield return Fire(g,mouse,echo.ReceiverTarget.transform.position);else yield return null;
                    }
                }
            }
            else if(encounter is ShepherdEncounter shepherd)
            {
                yield return Walk(g,shepherd,keyboard,1.4f);yield return Keys(keyboard,Key.E);yield return Keys(keyboard);
                yield return Walk(g,shepherd,keyboard,6.3f,true);
                float deadline=Time.time+9;while(shepherd.Rescued<1 && Time.time<deadline)yield return null;
                yield return Walk(g,shepherd,keyboard,1.1f,true);
                yield return Keys(keyboard,Key.Q);yield return new WaitForSeconds(1);
                yield return Keys(keyboard);yield return new WaitForSeconds(4);
                yield return Walk(g,shepherd,keyboard,6.3f,true);
                deadline=Time.time+10;while(!shepherd.Creatures[1].Rescued && Time.time<deadline)yield return null;
                yield return Keys(keyboard);deadline=Time.time+10;
                while(g.State==DemoState.Challenge && Time.time<deadline)yield return null;
            }
            else if(encounter is StormEncounter storm)
            {
                float timeout=Time.time+50;
                while(g.State==DemoState.Challenge && Time.time<timeout)
                {
                    int stage=storm.Stage;
                    if(stage!=1)
                    {
                        while(storm.Cycle<2.1f && Time.time<timeout)yield return null;
                        yield return Keys(keyboard,Key.Space);yield return Keys(keyboard);
                    }
                    while(!storm.Exposed && Time.time<timeout)yield return null;
                    yield return Walk(g,storm,keyboard,3.9f);
                    yield return Fire(g,mouse,storm.Switches[storm.NextSwitch].transform.position);
                    yield return Fire(g,mouse,storm.Switches[storm.NextSwitch].transform.position);
                    if(g.State==DemoState.Challenge && storm.Stage!=stage)yield return Walk(g,storm,keyboard,0);
                    if(g.State==DemoState.Challenge && storm.Stage==stage){g.Challenge.Retry();yield return new WaitForSeconds(.2f);}
                }
            }
            else if(encounter is GiantEncounter giant)
            {
                yield return Walk(g,giant,keyboard,-1.6f);yield return Fire(g,mouse,giant.Bells[0].transform.position);
                yield return Walk(g,giant,keyboard,GiantEncounter.SealPositions[0]);
                yield return Keys(keyboard,Key.E);yield return Keys(keyboard);yield return new WaitForSeconds(.8f);
                yield return Fire(g,mouse,giant.Bells[1].transform.position);
                yield return Walk(g,giant,keyboard,GiantEncounter.SealPositions[1]);
                float quietDeadline=Time.time+2.5f;
                while(giant.Awareness>.48f && Time.time<quietDeadline)yield return null;
                if(giant.LureRemaining<2)yield return Fire(g,mouse,giant.Bells[1].transform.position);
                yield return Keys(keyboard,Key.E);yield return Keys(keyboard);yield return new WaitForSeconds(.8f);
                yield return Walk(g,giant,keyboard,GiantEncounter.PagePosition);
                yield return Keys(keyboard,Key.E);yield return Keys(keyboard);
            }
            else if(encounter is RelayEncounter relay)
            {
                for(int leg=0;leg<3;leg++)
                {
                    var from=relay.Route[leg];var to=relay.Route[leg+1];
                    yield return WalkSurface(g,keyboard,from,(to.Center-from.Center).normalized);
                    yield return Keys(keyboard,Key.Space);yield return Keys(keyboard);
                    if(leg==1)yield return new WaitForSeconds(.15f);
                    g.RecoilToward(g.Player.Body.position-(relay.GateCenters[leg]-g.Player.Body.position).normalized*8);
                    float deadline=Time.time+10;
                    while(!g.Player.IsGrounded && Time.time<deadline)
                    {
                        Vector2 target=relay.Passed<=leg?relay.GateCenters[leg]:relay.Shrines[leg+1];
                        Vector2 direction=(target-g.Player.Body.position).normalized;
                        var keys=new System.Collections.Generic.List<Key>();
                        if(Mathf.Abs(direction.x)>.2f)keys.Add(direction.x>0?Key.D:Key.A);
                        if(Mathf.Abs(direction.y)>.2f)keys.Add(direction.y>0?Key.W:Key.S);
                        yield return Keys(keyboard,keys.ToArray());
                    }
                    yield return Keys(keyboard);
                    if(g.Player.gravityManager.CurrentBody!=to || relay.Passed<=leg)yield break;
                    yield return WalkSurface(g,keyboard,to,(relay.Shrines[leg+1]-to.Center).normalized);
                    yield return new WaitForSeconds(.2f);
                    if(leg==0)
                    {
                        // Exercise the public retry key after a real landing checkpoint.
                        if(relay.Checkpoint!=1)yield break;
                        yield return Keys(keyboard,Key.R);yield return Keys(keyboard);
                        yield return new WaitForSeconds(.2f);
                        if(relay.Passed!=1 || !relay.Carrying || g.Player.FluteUsed || g.Player.gravityManager.CurrentBody!=to)yield break;
                    }
                }
                yield return Keys(keyboard,Key.E);yield return Keys(keyboard);
            }
        }
        IEnumerator CaptureEncounterLater(int index)
        {
            var encounter=FindAnyObjectByType<DemoGame>().Challenge.Encounter;
            float deadline=Time.time+15;
            while(Time.time<deadline)
            {
                bool ready=encounter is EchoEncounter echo ? echo.Playing && echo.GhostOnPlate
                    : encounter is StormEncounter storm ? storm.Cycle>2.2f
                    : encounter is ShepherdEncounter shepherd ? shepherd.Rescued>0
                    : encounter is GiantEncounter giant ? giant.LureRemaining>0
                    : encounter is RelayEncounter relay && relay.Carrying && FindAnyObjectByType<DemoGame>().Player.FluteUsed;
                if(ready)break;
                yield return null;
            }
            yield return Capture("08-playing-"+index);
        }
        IEnumerator Start()
        {
            Directory.CreateDirectory(output);yield return null;yield return null;
            var g=FindAnyObjectByType<DemoGame>();
            if(g==null){File.WriteAllText(Path.Combine(output,"FAILED.txt"),"Demo did not initialize");Application.Quit(1);yield break;}
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            Application.runInBackground=true;
            keyboard=InputSystem.AddDevice<Keyboard>();InputSystem.EnableDevice(keyboard);
            mouse=InputSystem.AddDevice<Mouse>();InputSystem.EnableDevice(mouse);
            yield return Capture("01-title");g.StartGame();yield return new WaitForSeconds(.3f);
            g.Interact();yield return new WaitForSecondsRealtime(.5f);yield return Capture("02-typed-dialogue");
            Check(!g.Hud.DialogueComplete,"Dialogue reveals progressively");g.AdvanceDialogue();g.AdvanceDialogue();
            g.Pause();yield return Capture("03-pause-controls");g.Resume();
            g.ToggleMap();yield return Capture("04-expanded-world");g.ToggleMap();
            var target=g.Player.gravityManager.bodies[1];
            g.Player.Respawn(g.Planets.respawnPlanet,(target.Center-g.Planets.respawnPlanet.Center).normalized);yield return new WaitForSeconds(.25f);
            yield return Press(Key.Space);yield return Press();yield return Press(Key.Space);yield return Press();
            float deadline=Time.time+8;
            while(!g.Player.IsGrounded && Time.time<deadline)
            {
                Vector2 direction=(target.Center-g.Player.Body.position).normalized;
                var keys=new System.Collections.Generic.List<Key>();
                if(Mathf.Abs(direction.x)>.25f)keys.Add(direction.x>0?Key.D:Key.A);
                if(Mathf.Abs(direction.y)>.25f)keys.Add(direction.y>0?Key.W:Key.S);
                yield return Press(keys.ToArray());
            }
            yield return Press();Check(g.Planets.CurrentPlanet==target,"Starter flute actual flight");
            yield return Capture("05-moon-landing");
            yield return new WaitForSeconds(.3f);yield return Press(Key.Space);yield return Press();
            Vector2 aim=g.Player.Up;int boosts=g.Player.FluteJumpCount;
            g.RecoilToward(g.Player.Body.position+aim*10);
            Check(g.Player.FluteJumpCount==boosts+1,"Right click uses second jump");
            Check(Vector2.Dot(g.Player.Body.linearVelocity,-aim)>0,"Shot recoil opposes aim");
            yield return Press(Key.Space);yield return Press();Check(g.Player.FluteJumpCount==boosts+1,"No third jump");
            deadline=Time.time+4;
            while(!g.Player.IsGrounded && Time.time<deadline)yield return null;
            Check(g.Player.IsGrounded && g.Planets.CurrentPlanet==target,"Inward recoil returns to launch moon");
            var q=g.Melody;
            for(int index=0;index<DemoQuest.FragmentCount;index++)
            {
                var station=q.Stations[index];
                g.Player.Respawn(station.Body,(Vector2)station.transform.position-station.Body.Center);
                yield return new WaitForSeconds(.3f);
                yield return Capture("06-beacon-"+index);
                g.Interact();Check(g.State==DemoState.Dialogue,"Station dialogue");AdvanceAll(g);
                Check(g.State==DemoState.Challenge,"Station begins challenge");
                yield return Capture("07-challenge-"+index);
                StartCoroutine(CaptureEncounterLater(index));
                yield return SolveChallenge(g,keyboard,mouse);
                if(!g.Challenge.Success)
                {
                    string detail="Encounter "+index+" failed; "+g.Challenge.Encounter.Status+"; position="+g.Challenge.Encounter.Along(g.Player.Body.position)+"; seed="+g.Seed;
                    if(g.Challenge.Encounter is GiantEncounter failedGiant)detail+="; awareness="+failedGiant.Awareness+"; seals="+failedGiant.SealsOpened+"; wakes="+failedGiant.Wakes+"; message="+g.Message;
                    File.WriteAllText(Path.Combine(output,"encounter-failure.txt"),detail);
                }
                Check(g.Challenge.Success,"Challenge "+index+" completed through input");
                Check(q.Fragments[index],"Page "+index+" awarded");
                if(!g.Challenge.Success)break;
            }
            Check(q.Count==5,"Exactly five pages from five moons");
            if(q.Count!=5){File.WriteAllText(Path.Combine(output,"FAILED.txt"),"Page challenges failed");Application.Quit(1);yield break;}
            Check(!g.BeginRhythm(q),"No remote performance");
            g.Player.Respawn(g.Planets.respawnPlanet,Vector2.up);yield return new WaitForSeconds(.3f);
            g.Interact();g.AdvanceDialogue();g.AdvanceDialogue();Check(g.MelodyRepaired,"Home altar repairs score");
            g.AdvanceDialogue();g.AdvanceDialogue();Check(g.State==DemoState.Rhythm,"Home performance");
            for(int i=0;i<DemoRhythm.NoteCount;i++)
            {
                while(g.SongTime<g.Rhythm.TimeOf(i))yield return null;
                yield return Press(new[]{Key.D,Key.F,Key.J,Key.K}[g.Rhythm.Lane(i)]);yield return Press();
                if(i==3)yield return Capture("07-rhythm");
            }
            while(g.State==DemoState.Rhythm)yield return null;
            Check(g.LastPassed && g.Equipped==1 && g.Completed==1,"Piano unlocked after performance");yield return Capture("08-piano-unlocked");
            g.Continue();yield return new WaitForSeconds(.3f);
            for(int i=0;i<7;i++){yield return Press(new[]{Key.Digit1,Key.Digit2,Key.Digit3,Key.Digit4,Key.Digit5,Key.Digit6,Key.Digit7}[i]);yield return Press();yield return new WaitForSeconds(.1f);}
            yield return Capture("09-pocket-piano");
            g.CycleInstrument();Check(g.Equipped==0,"Only flute and piano available");
            InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(mouse);
            File.WriteAllText(Path.Combine(output,"result.json"),"{\"passed\":"+(errors==0?"true":"false")+",\"errors\":"+errors+",\"seed\":"+g.Seed+",\"moons\":24,\"pages\":"+q.Count+",\"pianoUnlocked\":"+(q.Unlocked?"true":"false")+"}");
            Application.Quit(errors==0?0:1);
        }
    }
}
#endif
