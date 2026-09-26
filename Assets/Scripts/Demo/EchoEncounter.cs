using System.Collections.Generic;
using UnityEngine;

namespace FlyMeToTheMoon.Demo
{
    public sealed class EchoEncounter : WorldEncounter
    {
        public struct EchoFrame { public float Time, Rotation; public Vector2 Position; public bool Grounded; }
        public struct EchoShot { public float Time; public Vector2 Position, Direction; }
        public readonly List<EchoFrame> Frames = new List<EchoFrame>();
        public readonly List<EchoShot> Shots = new List<EchoShot>();
        public bool Recording { get; private set; }
        public bool Playing { get; private set; }
        public bool GhostOnPlate { get; private set; }
        public bool PlayerOnSecondPlate => Near(8.1f,.7f,.65f) && Game.Player.IsGrounded;
        public float Clock { get; private set; }
        public float Duration { get; private set; }
        public const float RecordLimit = 8;
        public EncounterGate Door { get; private set; }
        public EncounterShotTarget ReceiverTarget { get; private set; }
        public Vector2 GhostPosition => ghost.transform.position;
        public override Vector2 Goal => Recording ? At(2.8f) : Playing ? (Stage==0 ? (Vector2)ReceiverTarget.transform.position : At(8.1f)) : At(0);
        public override float Meter => Recording ? Clock/RecordLimit : Playing ? Clock/Duration : 0;
        public override string MeterLabel => (Recording ? "REC " : Playing ? "ECHO " : "C: RECORD ") + Mathf.CeilToInt((Recording?RecordLimit:Duration)-Clock)+"s";
        readonly PrototypeShape ghost, plate, secondPlate;
        readonly PrototypeShape[] trail = new PrototypeShape[48];
        int shotCursor;
        public EchoEncounter(DemoChallenge session) : base(session)
        {
            plate=Surface("Echo plate",2.8f,.08f,new Vector2(1.2f,.15f),Color.cyan);Label(plate.transform,"CYAN PLATE");
            secondPlate=Surface("Duet plate",8.1f,.08f,new Vector2(1.2f,.15f),new Color(1,.8f,.3f));Label(secondPlate.transform,"GOLD PLATE");
            Door=Gate("Echo gate",5.7f);
            ReceiverTarget=Receiver("RECEIVER",9.1f,.9f,DemoGame.Accent,HitReceiver);
            ghost=Shape("Recorded ghost",At(0),new Vector2(.45f,.65f),new Color(.35f,.9f,1,.5f));
            for(int i=0;i<trail.Length;i++)trail[i]=Shape("Recorded route",At(0),Vector2.one*.065f,new Color(.2f,.8f,1),true);
        }
        public override void Reset()
        {
            Recording=Playing=GhostOnPlate=false;Clock=Duration=0;Frames.Clear();Shots.Clear();shotCursor=0;
            ghost.gameObject.SetActive(false);foreach(var dot in trail)dot.gameObject.SetActive(false);
            secondPlate.gameObject.SetActive(Stage==1);Door.SetOpen(false);Spawn();
            Status=Stage==0?"C: record a route onto the cyan plate":"C: record again; both plates must overlap";
        }
        public override void Record()
        {
            Reset();Recording=true;Status="Walk onto the cyan plate and wait. E: playback";
        }
        public override void Interact()
        {
            if(!Recording || Frames.Count<2)return;
            Duration=Mathf.Max(.25f,Clock);Recording=false;Playing=true;Clock=0;shotCursor=0;Spawn();ghost.gameObject.SetActive(true);
            Status=Stage==0?"Cross the gate and shoot while your echo holds the plate":"Hold the gold plate and shoot while your echo holds cyan";
        }
        public override void Shot(Vector2 position, Vector2 direction)
        {
            if(Recording)Shots.Add(new EchoShot{Time=Clock,Position=position,Direction=direction});
        }
        public override void Tick(float delta, bool calling)
        {
            if(Recording)
            {
                Clock=Mathf.Min(RecordLimit,Clock+delta);
                Frames.Add(new EchoFrame{Time=Clock,Position=Game.Player.Body.position,Rotation=Game.Player.Body.rotation,Grounded=Game.Player.IsGrounded});
                UpdateTrail();if(Clock>=RecordLimit)Interact();
            }
            else if(Playing)
            {
                Clock+=delta;
                if(Clock>=Duration){Clock%=Duration;shotCursor=0;}
                int i=0;while(i+1<Frames.Count && Frames[i+1].Time<Clock)i++;
                var a=Frames[i];var b=Frames[Mathf.Min(i+1,Frames.Count-1)];
                float blend=b.Time>a.Time?Mathf.Clamp01((Clock-a.Time)/(b.Time-a.Time)):0;
                Vector2 pos=Vector2.Lerp(a.Position,b.Position,blend);
                ghost.transform.position=new Vector3(pos.x,pos.y,-.75f);ghost.transform.rotation=Quaternion.Euler(0,0,Mathf.LerpAngle(a.Rotation,b.Rotation,blend));
                GhostOnPlate=a.Grounded && Vector2.Distance(pos,At(2.8f))<.7f;
                while(shotCursor<Shots.Count && Shots[shotCursor].Time<=Clock)
                {
                    var shot=Shots[shotCursor++];Game.EmitEchoShot(shot.Position,shot.Direction);
                }
            }
            Door.SetOpen(GhostOnPlate || (Near(2.8f,.7f,.65f) && Game.Player.IsGrounded));
            Colorize(plate,GhostOnPlate?Color.white:Color.cyan);
            Colorize(secondPlate,PlayerOnSecondPlate?Color.white:new Color(1,.8f,.3f));
        }
        void UpdateTrail()
        {
            for(int i=0;i<trail.Length;i++)
            {
                bool shown=i<Frames.Count;trail[i].gameObject.SetActive(shown);if(!shown)continue;
                var frame=Frames[Mathf.RoundToInt(i/(float)(trail.Length-1)*(Frames.Count-1))];
                trail[i].transform.position=new Vector3(frame.Position.x,frame.Position.y,-.65f);
            }
        }
        void HitReceiver(bool echo)
        {
            if(echo || !Playing || !GhostOnPlate || !OnMoon || PlayerS<6.1f || (Stage==1 && !PlayerOnSecondPlate))return;
            if(Stage==0){Stage=1;Game.ClearShots();Reset();Game.Tell("Now hold both plates together");}
            else Complete=true;
        }
    }
}
