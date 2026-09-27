using UnityEngine;

namespace FlyMeToTheMoon.Demo
{
    public sealed class ShepherdEncounter : WorldEncounter
    {
        public sealed class Singer
        {
            public float Position, Target, Fear, FearDirection, Settled, Guided;
            public bool Heard, Responded;
            public bool Rescued, Awake;
            public PrototypeShape Shape;
            public EncounterLabel Label;
        }
        public readonly Singer[] Creatures = new Singer[3];
        public EncounterGate Door { get; private set; }
        public int Rescued { get; private set; }
        public float Sustained { get; private set; }
        public const float Sanctuary = 5.5f;
        public override Vector2 Goal => At(Door.Open?Sanctuary:1.5f);
        readonly PrototypeShape lever, sanctuary, callRing;
        float noteTimer;
        public ShepherdEncounter(DemoChallenge session) : base(session)
        {
            Door=Gate("Sanctuary gate",2.5f,2.2f);
            lever=Surface("Gate lever",1.4f,.6f,new Vector2(.2f,.8f),DemoGame.Accent);Label(lever.transform,"E: GATE");
            sanctuary=Surface("Sanctuary",Sanctuary,.04f,new Vector2(2,.12f),DemoGame.Accent);Label(sanctuary.transform,"SANCTUARY");
            foreach(float s in new[]{-7.5f,8.7f}){var hazard=Surface("Thorn patch",s,.1f,new Vector2(1.1f,.24f),new Color(1,.3f,.35f));Label(hazard.transform,"THORNS");}
            for(int i=0;i<3;i++)
            {
                var shape=Surface("Singer "+i,-2.5f-i*1.2f,.4f,new Vector2(.42f,.48f),CreatureColor(i),true);
                Creatures[i]=new Singer{Shape=shape,Label=Label(shape.transform,new[]{"Q","HOLD Q","SILENCE"}[i])};
                var eye=DemoWorld.Shape(shape.transform,"Eye",Vector2.zero,Vector2.one*.07f,new Color(.07f,.08f,.15f),Game.Material,true);
                eye.transform.localPosition=new Vector3(.1f,.07f,-.04f);
            }
            callRing=Shape("Flute call",At(0),Vector2.one*.7f,new Color(.2f,.75f,.8f,.3f),true);
        }
        static Color CreatureColor(int i) => new[]{new Color(.45f,.9f,1),new Color(1,.8f,.35f),new Color(.85f,.6f,1)}[i];
        public override void Reset()
        {
            Rescued=0;Sustained=0;Door.SetOpen(false);noteTimer=0;
            for(int i=0;i<3;i++){var c=Creatures[i];c.Position=c.Target=-2.5f-i*1.2f;c.Fear=c.Settled=c.Guided=0;c.Heard=c.Responded=false;c.Rescued=false;c.Awake=i==0;}
            Spawn();
        }
        public override void Interact(){if(Near(1.4f,1.15f,1.5f)){Door.SetOpen(!Door.Open);Game.Audio.Note(0,4);}}
        public override void Shot(Vector2 position, Vector2 direction)
        {
            foreach(var c in Creatures)if(c.Awake && !c.Rescued && Vector2.Distance(position,At(c.Position))<3)
            {c.Fear=1.2f;c.FearDirection=Mathf.Sign(ArcDelta(Along(position),c.Position));if(c.FearDirection==0)c.FearDirection=-1;}
        }
        public override void Tick(float delta, bool calling)
        {
            calling &= OnMoon;Sustained=calling?Sustained+delta:0;
            callRing.gameObject.SetActive(calling);callRing.transform.position=(Vector3)Game.Player.Body.position+Vector3.forward*.03f;
            if(calling){noteTimer-=delta;if(noteTimer<=0){Game.Audio.Note(0,4,.12f);noteTimer=.6f;}callRing.transform.localScale=Vector3.one*(1+Mathf.Repeat(Session.Elapsed*2,1));}
            for(int i=0;i<3;i++)
            {
                var c=Creatures[i];c.Awake=i==0 || Rescued>0;
                c.Label.Text=c.Rescued?"":new[]{"Q","HOLD Q","SILENCE"}[i];
                if(c.Rescued){Place(c.Shape,Sanctuary+(i-1)*.4f,.35f);continue;}
                if(!c.Awake){Colorize(c.Shape,new Color(.28f,.3f,.4f));continue;}
                bool heard=calling && Mathf.Abs(ArcDelta(c.Position,PlayerS))<9;
                if(heard){c.Target=PlayerS;c.Heard=true;}
                bool moving=i==0?heard:i==1?heard && Sustained>.7f:!calling && Mathf.Abs(ArcDelta(c.Position,c.Target))>.6f;
                float step=0;
                if(c.Fear>0){c.Fear-=delta;step=c.FearDirection*2.1f*delta;}
                else if(moving && Mathf.Abs(ArcDelta(c.Position,c.Target))>.6f)step=Mathf.Sign(ArcDelta(c.Position,c.Target))*1.65f*delta;
                float next=c.Position+step;
                float gateDistance=ArcDelta(c.Position,2.5f);
                if(!Door.Open && Mathf.Sign(step)==Mathf.Sign(gateDistance) && Mathf.Abs(gateDistance)<=Mathf.Abs(step)+.25f)
                    next=c.Position+Mathf.Sign(step)*Mathf.Max(0,Mathf.Abs(gateDistance)-.25f);
                if(moving && c.Fear<=0 && c.Heard && Mathf.Abs(next-c.Position)>.000001f)
                {c.Guided+=Mathf.Abs(next-c.Position);c.Responded=true;}
                c.Position=next;
                if(Mathf.Abs(ArcDelta(c.Position,-7.5f))<.6f || Mathf.Abs(ArcDelta(c.Position,8.7f))<.6f)
                {c.Position=c.Target=-2.5f-i*1.2f;c.Fear=c.Settled=c.Guided=0;c.Heard=c.Responded=false;Game.Tell("A singer scattered — call from nearer ground");}
                bool safe=Door.Open && c.Heard && c.Responded && c.Guided>2 && c.Fear<=0 && Mathf.Abs(ArcDelta(c.Position,Sanctuary))<.8f;
                c.Settled=safe?c.Settled+delta:0;
                if(c.Settled>=.8f){c.Rescued=true;Rescued++;Game.Audio.Note(0,i+3);}
                Place(c.Shape,c.Position,.38f+(step!=0?Mathf.Abs(Mathf.Sin(Session.Elapsed*10+i))*.15f:0));
                Colorize(c.Shape,c.Fear>0?new Color(1,.3f,.3f):CreatureColor(i));
            }
            Status=Rescued+" / 3 safe  ·  "+(calling?"CALLING":"SILENCE");Complete=Rescued==3;
        }
    }
}
