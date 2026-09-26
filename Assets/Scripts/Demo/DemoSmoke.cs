#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace FlyMeToTheMoon.Demo
{
    // Opt-in executable smoke test. Inactive in normal play; no saved progress or network access.
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
            yield return Capture("01-title");g.StartGame();
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            keyboard=InputSystem.AddDevice<Keyboard>();
            InputSystem.EnableDevice(keyboard);
            yield return new WaitForSeconds(.5f);
            yield return Press(Key.Space);yield return Press();yield return Press(Key.Space);yield return Press();
            float deadline=Time.time+15;
            while(!g.Player.IsGrounded && Time.time<deadline)yield return null;
            Check(g.Planets.CurrentPlanet==g.Quests[0].Body,"Actual double jump must land on Piano moon");
            yield return Capture("02-piano-landing");
            foreach(var q in g.Quests)
            {
                g.Player.Respawn(q.Body,(Vector2)q.Walk.transform.position-q.Body.Center);
                yield return new WaitForSeconds(.3f);Check(q.Fragments[0],"Walk page "+q.Index);
                g.Player.Respawn(q.Body,(Vector2)q.Shot.transform.position-q.Body.Center);
                yield return new WaitForSeconds(.3f);g.ShootToward(q.Shot.transform.position);
                yield return new WaitForSeconds(.3f);Check(q.Fragments[1],"Shot page "+q.Index);
                g.Player.Respawn(q.Body,q.Pedestal-q.Body.Center);yield return new WaitForSeconds(.3f);
                g.Interact();yield return new WaitForSeconds(2);
                foreach(int i in new[]{1,0,2})
                {g.ShootToward(q.Resonators[i].transform.position);yield return new WaitForSeconds(.4f);}
                yield return new WaitForSeconds(.3f);
                Check(q.PuzzleSolved,"Projectile resonator sequence "+q.Index);Check(q.Count==3,"All pages "+q.Index);
                if(q.Count!=3){File.WriteAllText(Path.Combine(output,"FAILED.txt"),"Quest "+q.Index+" could not finish; inspect Player.log");Application.Quit(1);yield break;}
                g.Player.Respawn(q.Body,q.Altar-q.Body.Center);yield return new WaitForSeconds(.2f);g.Interact();
                Check(g.State==DemoState.Rhythm,"Altar starts performance");
                for(int i=0;i<DemoRhythm.NoteCount;i++)
                {
                    while(g.SongTime<g.Rhythm.TimeOf(i))yield return null;
                    yield return Press(new[]{Key.A,Key.S,Key.D,Key.F}[g.Rhythm.Lane(i)]);yield return Press();
                    if(i==3 && q.Index==0)yield return Capture("03-rhythm");
                }
                while(g.State==DemoState.Rhythm)yield return null;
                Check(g.LastPassed,"Performance pass "+q.Index);Check(g.Completed==q.Index+1,"Progression "+q.Index);
                if(q.Index==4)yield return Capture("04-ending");
                g.Continue();
            }
            g.CycleInstrument();Check(g.Equipped==0,"Instrument cycle");g.ToggleMap();yield return Capture("05-map");
            InputSystem.RemoveDevice(keyboard);
            File.WriteAllText(Path.Combine(output,"result.json"),"{\"passed\":"+(errors==0?"true":"false")+",\"errors\":"+errors+",\"moonsRestored\":"+g.Completed+"}");
            Application.Quit(errors==0?0:1);
        }
    }
}
#endif
