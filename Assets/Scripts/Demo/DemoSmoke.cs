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
        string output; int errors; Keyboard keyboard;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-demoSmoke");
            if(index>=0 && index+1<args.Length) new GameObject("Local validation").AddComponent<DemoSmoke>().output=args[index+1];
        }
        void Check(bool condition,string message){if(!condition){errors++;Debug.LogError("DEMO_SMOKE: "+message);}}
        IEnumerator Capture(string name){yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return null;}
        IEnumerator Press(params Key[] keys){InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));yield return null;yield return new WaitForFixedUpdate();}
        IEnumerator Start()
        {
            Directory.CreateDirectory(output);yield return null;yield return null;
            var g=FindAnyObjectByType<DemoGame>();
            if(g==null){File.WriteAllText(Path.Combine(output,"FAILED.txt"),"Demo did not initialize");Application.Quit(1);yield break;}
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            keyboard=InputSystem.AddDevice<Keyboard>();InputSystem.EnableDevice(keyboard);
            yield return Capture("01-title");g.StartGame();yield return new WaitForSeconds(.3f);
            g.Interact();yield return new WaitForSecondsRealtime(.5f);yield return Capture("02-typed-dialogue");
            Check(!g.Hud.DialogueComplete,"Dialogue reveals progressively");g.AdvanceDialogue();g.AdvanceDialogue();
            g.Pause();yield return Capture("03-pause-controls");g.Resume();
            g.ToggleMap();yield return Capture("04-three-rings");g.ToggleMap();
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
            g.ShootToward(g.Player.Body.position+aim*10);
            Check(g.Player.FluteJumpCount==boosts+1,"Air shot uses second jump");
            Check(Vector2.Dot(g.Player.Body.linearVelocity,-aim)>0,"Shot recoil opposes aim");
            yield return Press(Key.Space);yield return Press();Check(g.Player.FluteJumpCount==boosts+1,"No third jump");
            deadline=Time.time+4;
            while(!g.Player.IsGrounded && Time.time<deadline)yield return null;
            Check(g.Player.IsGrounded && g.Planets.CurrentPlanet==target,"Inward recoil returns to launch moon");
            var q=g.Melody;
            g.Player.Respawn(q.FragmentBodies[0],(Vector2)q.Walk.transform.position-q.FragmentBodies[0].Center);
            yield return new WaitForSeconds(.3f);Check(q.Fragments[0],"Ruins page");
            g.Player.Respawn(q.FragmentBodies[1],(Vector2)q.Shot.transform.position-q.FragmentBodies[1].Center);
            yield return new WaitForSeconds(.3f);g.ShootToward(q.Shot.transform.position);yield return new WaitForSeconds(.3f);Check(q.Fragments[1],"Floating page");
            g.Player.Respawn(q.FragmentBodies[2],q.Pedestal-q.FragmentBodies[2].Center);yield return new WaitForSeconds(.3f);
            yield return Capture("06-context-arrow");g.Interact();g.AdvanceDialogue();g.AdvanceDialogue();yield return new WaitForSeconds(2);
            foreach(int i in new[]{1,0,2}){g.ShootToward(q.Resonators[i].transform.position);yield return new WaitForSeconds(.4f);}
            yield return null;Check(q.Count==3,"Three pages from three different moons");
            if(q.Count!=3){File.WriteAllText(Path.Combine(output,"FAILED.txt"),"Page collection failed");Application.Quit(1);yield break;}
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
            InputSystem.RemoveDevice(keyboard);
            File.WriteAllText(Path.Combine(output,"result.json"),"{\"passed\":"+(errors==0?"true":"false")+",\"errors\":"+errors+",\"seed\":"+g.Seed+",\"moons\":18,\"pages\":"+q.Count+",\"pianoUnlocked\":"+(q.Unlocked?"true":"false")+"}");
            Application.Quit(errors==0?0:1);
        }
    }
}
#endif
