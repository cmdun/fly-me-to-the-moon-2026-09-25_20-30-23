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
        public static IEnumerator SolveChallenge(DemoGame g, Keyboard keyboard, Mouse mouse)
        {
            var c=g.Challenge;
            var noteKeys=new[]{Key.Digit1,Key.Digit2,Key.Digit3,Key.Digit4,Key.Digit5,Key.Digit6,Key.Digit7};
            if(c.Kind==FragmentChallenge.Echo || c.Kind==FragmentChallenge.Code)
            {
                while(c.Previewing)yield return null;
                while(!c.Finished)
                {
                    InputSystem.QueueStateEvent(keyboard,new KeyboardState(noteKeys[c.Sequence[c.Step]]));yield return null;yield return null;
                    InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;yield return null;
                }
            }
            else if(c.Kind==FragmentChallenge.Beat)
            {
                for(int i=0;i<12 && !c.Finished;i++)
                {
                    while(c.Elapsed<DemoChallenge.BeatTime(i) && !c.Finished)yield return null;
                    InputSystem.QueueStateEvent(keyboard,new KeyboardState(new[]{Key.A,Key.S,Key.D,Key.F}[c.Sequence[i]]));yield return null;yield return null;
                    InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;yield return null;
                }
                while(!c.Finished)yield return null;
            }
            else if(c.Kind==FragmentChallenge.Targets)
            {
                while(!c.Finished)
                {
                    Vector2 screen=g.GetComponent<DemoChallengeHud>().TargetScreenPosition;
                    InputSystem.QueueStateEvent(mouse,new MouseState { position=screen }.WithButton(MouseButton.Left));yield return null;yield return null;
                    InputSystem.QueueStateEvent(mouse,new MouseState { position=screen });yield return null;yield return null;
                }
            }
            else
            {
                var route=new[]{new Vector2(-.88f,.48f),new Vector2(-.25f,.48f),new Vector2(-.25f,-.48f),
                    new Vector2(.24f,-.48f),new Vector2(.24f,.48f),new Vector2(.88f,.48f),new Vector2(.88f,.75f)};
                foreach(var waypoint in route)
                {
                    while(!c.Finished && Mathf.Max(Mathf.Abs(c.MazePosition.x-waypoint.x),Mathf.Abs(c.MazePosition.y-waypoint.y))>.025f)
                    {
                        Vector2 delta=waypoint-c.MazePosition;var keys=new System.Collections.Generic.List<Key>();
                        if(Mathf.Abs(delta.x)>.025f)keys.Add(delta.x>0?Key.D:Key.A);
                        if(Mathf.Abs(delta.y)>.025f)keys.Add(delta.y>0?Key.W:Key.S);
                        InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys.ToArray()));yield return null;
                    }
                    InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
                }
            }
        }
        IEnumerator Start()
        {
            Directory.CreateDirectory(output);yield return null;yield return null;
            var g=FindAnyObjectByType<DemoGame>();
            if(g==null){File.WriteAllText(Path.Combine(output,"FAILED.txt"),"Demo did not initialize");Application.Quit(1);yield break;}
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
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
                g.Interact();Check(g.State==DemoState.Dialogue,"Station dialogue");g.AdvanceDialogue();g.AdvanceDialogue();
                Check(g.State==DemoState.Challenge,"Station begins challenge");
                yield return Capture("07-challenge-"+index);
                yield return SolveChallenge(g,keyboard,mouse);
                Check(g.Challenge.Success,"Challenge "+index+" completed through input");
                g.FinishChallenge();Check(q.Fragments[index],"Page "+index+" awarded");
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
                yield return Press(new[]{Key.A,Key.S,Key.D,Key.F}[g.Rhythm.Lane(i)]);yield return Press();
                if(i==3)yield return Capture("07-rhythm");
            }
            while(g.State==DemoState.Rhythm)yield return null;
            Check(g.LastPassed && g.Equipped==1 && g.Completed==1,"Piano unlocked after performance");yield return Capture("08-piano-unlocked");
            g.Continue();yield return new WaitForSeconds(.3f);
            for(int i=0;i<7;i++){yield return Press(new[]{Key.Digit1,Key.Digit2,Key.Digit3,Key.Digit4,Key.Digit5,Key.Digit6,Key.Digit7}[i]);yield return Press();yield return new WaitForSeconds(.1f);}
            g.CycleInstrument();Check(g.Equipped==0,"Only flute and piano available");
            InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(mouse);
            File.WriteAllText(Path.Combine(output,"result.json"),"{\"passed\":"+(errors==0?"true":"false")+",\"errors\":"+errors+",\"seed\":"+g.Seed+",\"moons\":24,\"pages\":"+q.Count+",\"pianoUnlocked\":"+(q.Unlocked?"true":"false")+"}");
            Application.Quit(errors==0?0:1);
        }
    }
}
#endif
