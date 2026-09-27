using System.Collections.Generic;
using UnityEngine;

namespace FlyMeToTheMoon.Demo
{
    /// <summary>Three physical activities, with four escalating variations each. No reward can be collected by walking around its puzzle.</summary>
    public sealed class MoonDiscovery : MonoBehaviour
    {
        public int Id { get; private set; }
        public int Moon { get; private set; }
        public int Kind { get; private set; }
        public int Rank { get; private set; }
        public GravityBody Body { get; private set; }
        public Vector2 Up { get; private set; }
        public bool Solved { get; private set; }
        public bool Started { get; private set; }
        public int Progress { get; private set; }
        public int[] Melody { get; private set; }
        public int[] Turns { get; private set; }
        public int[] CorrectTurns { get; private set; }
        public int LitMirrors { get; private set; }
        public float CreatureArc { get; private set; } = -3.5f;
        public int Shelters { get; private set; }
        public bool HasFood { get; private set; }
        public bool Fed { get; private set; }
        public bool Listening => listenAt>=0;
        public bool Complete => owner.Found[Id];
        public string Title => Kind==0?"Bell garden":Kind==1?"Prism orchard":"Lantern walk";
        public string Status => Complete?"Discovery packed for HOME":!Started?"E: discover":Kind==0?
            Listening?"Listen to the glowing bells":"Shoot the bells  "+Progress+" / "+Melody.Length+"   ·   E: listen again":Kind==1?
            LitMirrors==Turns.Length?"Crystal awake · E at the crystal":"E: turn a reflector  ·  Light "+LitMirrors+" / "+Turns.Length:
            Time.time<calmAt?"Quiet... let your friend settle":!Fed?(HasFood?"E: offer food to the little singer":"E: gather food beneath the arch"):
            Shelters<3?"Q: guide gently through the lanterns  "+Shelters+" / 3":"Lead your friend to the arch";
        public Vector2 RewardPosition => At(4.25f,.5f);
        public IReadOnlyList<Transform> Controls => controls;
        DemoDiscoveries owner;DemoGame game;
        readonly List<Transform> controls=new List<Transform>();
        readonly List<SpriteRenderer> bells=new List<SpriteRenderer>();
        readonly List<Transform> mirrors=new List<Transform>();
        readonly List<LineRenderer> beams=new List<LineRenderer>();
        readonly List<SpriteRenderer> lanterns=new List<SpriteRenderer>();
        SpriteRenderer creature, reward;Transform bellConsole, foodBasket;
        float listenAt=-1, flashUntil, nextHit, nextCall, calmAt, settle;
        int listenIndex, flashed=-1;
        float[] shelterArcs={-1.5f,.4f,2.3f};
        public Vector2 At(float arc,float height=0)
        {Vector2 up=Quaternion.Euler(0,0,-arc/Body.radius*Mathf.Rad2Deg)*Up;return Body.Center+up*(Body.radius+height);}
        public float Arc(Vector2 point)=>-Vector2.SignedAngle(Up,point-Body.Center)*Mathf.Deg2Rad*Body.radius;
        Transform Mount(string label,float arc,float height=0)
        {
            var t=new GameObject(label).transform;t.SetParent(transform,false);t.position=At(arc,height);
            t.rotation=Quaternion.FromToRotation(Vector3.up,t.position-(Vector3)Body.Center);return t;
        }
        SpriteRenderer Art(string label,string key,float arc,float height,float size)
        {
            var art=PixelArtLibrary.Create(Mount(label,arc,height),label,key,4);PixelArtLibrary.Height(art,size);return art;
        }
        public void Build(DemoDiscoveries discoveries,int id,int moon,GravityBody body,Vector2 up,int kind,int rank)
        {
            owner=discoveries;game=owner.Game;Id=id;Moon=moon;Body=body;Up=up;Kind=kind;Rank=rank;
            Solved=Complete;Started=Complete;
            reward=Art("Discovery seed","07/object/0",4.25f,0,.6f);
            if(Kind==0)BuildBells();else if(Kind==1)BuildPrisms();else BuildLanterns();
            reward.gameObject.SetActive(Kind!=2);
            if(Complete && Kind==0){reward.sprite=PixelArtLibrary.Get("07/object/5");PixelArtLibrary.Height(reward,.8f);}
        }
        void BuildBells()
        {
            bellConsole=Art("Listen pedestal","prop/woodwinds",-4.2f,0,.8f).transform.parent;controls.Add(bellConsole);
            for(int i=0;i<3;i++)
            {
                var bell=Art("Answer bell "+i,"01/object/0",-2.3f+i*2.3f,0,1.25f);
                bell.color=BellColor(i);bells.Add(bell);
            }
            Melody=new int[3+Rank];var random=new System.Random(game.Seed^Id*151^0x7781);
            for(int i=0;i<Melody.Length;i++)Melody[i]=i==0?random.Next(3):(Melody[i-1]+1+random.Next(2))%3;
        }
        static Color BellColor(int i)=>i==0?new Color(.55f,1,1):i==1?new Color(1,.72f,.85f):new Color(1,.93f,.55f);
        void BuildPrisms()
        {
            int count=Rank<2?3:4;
            Turns=new int[count];CorrectTurns=new int[count];
            var lamp=Art("Beam source","prop/crystals",-4.3f,0,.85f);lamp.color=new Color(.6f,1,1);
            controls.Add(lamp.transform.parent);
            var random=new System.Random(game.Seed^Id*937^0x4541);
            for(int i=0;i<count;i++)
            {
                float arc=Mathf.Lerp(-2.55f,2.55f,count==1?.5f:i/(float)(count-1));
                var mount=Mount("Turnable reflector "+i,arc);
                var stand=DemoWorld.Shape(mount,"Brass pedestal",Vector2.zero,new Vector2(.38f,.16f),new Color(.78f,.5f,.23f),game.Material);
                stand.transform.localPosition=Vector3.up*.08f;
                var shaft=DemoWorld.Shape(mount,"Reflector stand",Vector2.zero,new Vector2(.07f,.55f),new Color(.67f,.46f,.24f),game.Material);
                shaft.transform.localPosition=Vector3.up*.33f;
                var mirror=DemoWorld.Shape(mount,"Reflecting face",Vector2.zero,new Vector2(.65f,.1f),new Color(.6f,.94f,1),game.Material);
                mirror.transform.localPosition=Vector3.up*.72f+Vector3.back*.2f;mirrors.Add(mirror.transform);
                var handle=DemoWorld.Shape(mount,"Turn handle",Vector2.zero,new Vector2(.15f,.13f),new Color(.98f,.75f,.3f),game.Material,true);
                handle.transform.localPosition=new Vector3(.28f,.26f,-.2f);
                controls.Add(mount);CorrectTurns[i]=random.Next(4);Turns[i]=Complete?CorrectTurns[i]:(CorrectTurns[i]+1+random.Next(3))%4;
            }
            reward.sprite=PixelArtLibrary.Get("prop/crystals");PixelArtLibrary.Height(reward,.9f);controls.Add(reward.transform.parent);
            for(int i=0;i<=count;i++)
            {
                var line=new GameObject("Visible light path").AddComponent<LineRenderer>();line.transform.SetParent(transform,false);
                line.sharedMaterial=game.Material;line.positionCount=2;line.startWidth=line.endWidth=.1f;
                line.startColor=line.endColor=new Color(.4f,.88f,1,.85f);line.sortingOrder=8;beams.Add(line);
            }
            UpdateBeams();
        }
        void BuildLanterns()
        {
            foodBasket=Art("Trail food","07/object/2",4.25f,0,.55f).transform.parent;controls.Add(foodBasket);
            var arch=Art("Sanctuary arch","prop/arch",4.25f,0,1.35f);arch.sortingOrder=1;
            creature=Art("Lost singer","06/object/0",CreatureArc,0,.55f);controls.Add(creature.transform.parent);
            for(int i=0;i<3;i++)
            {
                var lamp=Art("Trail lantern "+i,"prop/crystals",shelterArcs[i],0,.45f);
                lamp.color=Complete?new Color(.55f,1,.7f):new Color(.8f,.85f,1);lanterns.Add(lamp);
            }
            if(Complete){Shelters=3;creature.transform.parent.gameObject.SetActive(false);foodBasket.gameObject.SetActive(false);}
        }
        public Transform NearestControl()
        {
            if(!owner.Available || game.Player.gravityManager.CurrentBody!=Body || Complete)return null;
            Transform best=null;float distance=1.15f;
            foreach(var control in controls)
            {
                if(!control.gameObject.activeInHierarchy)continue;
                float d=Vector2.Distance(game.Player.Body.position,control.position);
                if(d<distance){best=control;distance=d;}
            }
            return best;
        }
        public bool Interact()
        {
            var control=NearestControl();if(control==null)return false;
            if(!Started)
            {
                string instructions=Kind==0?"Listen here, then shoot the three colored bells in the order they glow. You may walk between shots. A wrong bell starts the phrase over. Return to this pedestal whenever you need to hear it again.":Kind==1?
                    "Turn the brass-handled mirrors with E. Watch where each beam reflects, and connect the light from the blue source to the crystal. When every mirror carries light, approach the crystal and press E to wake its seed.":
                    "Gather food beneath the arch and offer it to the hungry singer. Hold Q while walking a little ahead to guide it through the three lanterns. Stay close and on the ground. Shots frighten it; later walks have lanterns that fade, so wait with your friend in each pool of light.";
                game.OpenDialogue(Title,instructions,"Try it",()=>{Started=true;if(Kind==0)Listen();});return true;
            }
            if(Kind==0){Listen();return true;}
            if(Kind==1)
            {
                int index=controls.IndexOf(control)-1;
                if(index>=0 && index<Turns.Length){Turns[index]=(Turns[index]+1)%4;UpdateBeams();game.Audio.Note(5,index,.2f);}
                else if(index==Turns.Length && LitMirrors==Turns.Length){Solved=true;owner.Award(this);reward.color=Color.white;}
                else if(index==Turns.Length)game.Tell("Guide every beam into the crystal first.");
                return true;
            }
            if(control==foodBasket){HasFood=true;game.Tell(Fed?"Hold Q and stay close to your friend.":"Food packed. Offer it to the singer with E.");}
            else if(!Fed && HasFood){Fed=true;calmAt=Time.time+.3f;game.Audio.Note(0,4,.3f);game.Tell("Trust earned. Hold Q and walk ahead gently.");}
            else if(!Fed)game.Tell("Food grows under the sanctuary arch.");
            return true;
        }
        public void Listen()
        {
            if(Kind!=0 || Complete || !Started)return;
            game.ClearShots();Progress=0;listenIndex=0;listenAt=Time.time+.45f;nextHit=float.PositiveInfinity;
        }
        public bool NotePassed(Vector2 from,Vector2 to)
        {
            if(!Started || game.State!=DemoState.Explore || game.Player.gravityManager.CurrentBody!=Body)return false;
            if(Kind==2)
            {
                if(Complete)return false;
                if(Vector2.Distance(from,creature.transform.position)<2.5f || OnSegment(creature.transform.position,from,to,.8f))
                {calmAt=Time.time+2;settle=0;creature.color=new Color(1,.65f,.65f);}
                return false;
            }
            if(Kind!=0 || Listening || Time.time<nextHit)return false;
            for(int i=0;i<bells.Count;i++)
            {
                Vector2 center=(Vector2)bells[i].transform.position+(Vector2)bells[i].transform.up*.8f;
                if(!OnSegment(center,from,to,.45f))continue;
                nextHit=Time.time+.22f;Ring(i);
                if(Complete)return true;
                if(i==Melody[Progress])Progress++;
                else {Progress=0;game.Tell("The phrase slipped. E at the pipes to listen again.");}
                if(Progress==Melody.Length){Solved=true;owner.Award(this);reward.sprite=PixelArtLibrary.Get("07/object/5");PixelArtLibrary.Height(reward,.8f);}
                return true;
            }
            return false;
        }
        static bool OnSegment(Vector2 p,Vector2 a,Vector2 b,float radius)
        {var line=b-a;float t=line.sqrMagnitude<.00001f?0:Mathf.Clamp01(Vector2.Dot(p-a,line)/line.sqrMagnitude);return Vector2.Distance(p,a+t*line)<radius;}
        void Ring(int i)
        {
            flashed=i;flashUntil=Time.time+.45f;game.Audio.Note(2,i*2,.4f);
            if(game.Art!=null)game.Art.Effect("01",bells[i].transform.position+bells[i].transform.up,.45f,bells[i].transform.rotation);
        }
        public void Tick(float dt,bool call)
        {
            if(game.State!=DemoState.Explore || game.MapVisible)return;
            if(Kind==0)
            {
                if(Listening && Time.time>=listenAt)
                {
                    if(listenIndex<Melody.Length){Ring(Melody[listenIndex++]);listenAt=Time.time+.85f;}
                    else {listenAt=-1;nextHit=Time.time;}
                }
                for(int i=0;i<3;i++)
                {
                    bool ring=i==flashed && Time.time<flashUntil;
                    bells[i].sprite=PixelArtLibrary.Get("01/object/"+(ring?2:Complete?5:0));PixelArtLibrary.Height(bells[i],1.25f);
                    bells[i].color=ring?Color.white:BellColor(i);
                }
            }
            else if(Kind==2 && Started && !Complete)TickSinger(dt,call);
        }
        public bool LanternLit(int index)=>Rank==0 || Mathf.Repeat(Time.time+index*.65f,5.8f-Rank*.45f)<3.25f-Rank*.25f;
        void TickSinger(float dt,bool call)
        {
            for(int i=0;i<3;i++)lanterns[i].color=i<Shelters?new Color(.55f,1,.7f):LanternLit(i)?new Color(1,.88f,.55f):new Color(.3f,.35f,.5f);
            if(!Fed)return;
            var p=game.Player;
            bool close=p.gravityManager.CurrentBody==Body && p.IsGrounded && Vector2.Distance(p.Body.position,creature.transform.position)<2.35f-Rank*.12f;
            float playerArc=Arc(p.Body.position),distance=playerArc-CreatureArc;
            bool calm=Time.time>=calmAt;
            if(call && close && calm && Mathf.Abs(distance)<4)
            {
                if(Time.time>=nextCall){nextCall=Time.time+.65f;game.Audio.Note(0,4,.12f);game.Art.PlayerArt.PlayNote(4);}
                float destination=playerArc-Mathf.Sign(distance)*.55f;
                // Unvisited lanterns are physical rest stops: never follow a player around the back to bypass them.
                if(Shelters<3)destination=Mathf.Min(destination,shelterArcs[Shelters]);
                CreatureArc=Mathf.MoveTowards(CreatureArc,destination,dt*(.95f-Rank*.07f));
            }
            if(Shelters<3 && Mathf.Abs(CreatureArc-shelterArcs[Shelters])<.12f && close && calm && LanternLit(Shelters))
            {settle+=dt;if(settle>=.65f){Shelters++;settle=0;game.Audio.Note(4,Shelters,.25f);}}
            else settle=0;
            creature.transform.parent.position=At(CreatureArc);
            creature.transform.parent.rotation=Quaternion.FromToRotation(Vector3.up,(Vector2)creature.transform.parent.position-Body.Center);
            creature.sprite=PixelArtLibrary.Get("06/object/"+(!calm?0:call && close?2+(int)(Time.time*5)%3:1));PixelArtLibrary.Height(creature,.55f);
            creature.color=calm?Color.white:new Color(1,.65f,.65f);
            if(Shelters==3 && CreatureArc>=3.6f && close && calm)
            {
                Solved=true;owner.Award(this);creature.transform.parent.gameObject.SetActive(false);foodBasket.gameObject.SetActive(false);
            }
        }
        public void UpdateBeams()
        {
            if(Kind!=1)return;
            int previousLit=LitMirrors;
            var normals=new Vector2[mirrors.Count];
            for(int i=0;i<mirrors.Count;i++)
            {
                Vector2 here=mirrors[i].position;
                Vector2 wantedPrevious=i==0?At(-4.3f,.72f):(Vector2)mirrors[i-1].position;
                Vector2 next=i==mirrors.Count-1?At(4.25f,.72f):(Vector2)mirrors[i+1].position;
                Vector2 idealIn=(here-wantedPrevious).normalized,idealOut=(next-here).normalized;
                Vector2 idealNormal=(idealIn-idealOut).normalized;
                if(idealNormal.sqrMagnitude<.01f)idealNormal=new Vector2(-idealIn.y,idealIn.x);
                normals[i]=Quaternion.Euler(0,0,(Turns[i]-CorrectTurns[i])*45)*idealNormal;
                mirrors[i].rotation=Quaternion.FromToRotation(Vector3.up,normals[i]);
            }
            Vector2 previous=At(-4.3f,.72f);Vector2 incoming=((Vector2)mirrors[0].position-previous).normalized;bool connected=true;LitMirrors=0;
            for(int i=0;i<mirrors.Count;i++)
            {
                Vector2 here=mirrors[i].position;
                Vector2 next=i==mirrors.Count-1?At(4.25f,.72f):(Vector2)mirrors[i+1].position;
                Vector2 normal=normals[i];
                var beam=beams[i];beam.enabled=connected;
                SetLine(beam,previous,here);
                if(connected)
                {
                    Vector2 outgoing=Vector2.Reflect(incoming,normal).normalized;
                    bool aligned=Vector2.Dot(outgoing,(next-here).normalized)>.995f;
                    if(aligned){LitMirrors++;previous=here;incoming=outgoing;}
                    else {SetLine(beams[i+1],here,here+outgoing*MissDistance(here,outgoing));beams[i+1].enabled=true;connected=false;}
                }
                // The outgoing miss remains visible; later, unlit mirrors carry no beam.
                if(!connected){for(int j=i+2;j<beams.Count;j++)beams[j].enabled=false;break;}
            }
            if(connected){beams[beams.Count-1].enabled=true;SetLine(beams[beams.Count-1],previous,At(4.25f,.72f));}
            reward.color=LitMirrors==mirrors.Count?Color.white:new Color(.38f,.42f,.55f);
            if(Started && !Complete && LitMirrors==mirrors.Count && previousLit<mirrors.Count)
                game.Tell("Light connected. Wake the crystal with E.");
        }
        float MissDistance(Vector2 origin,Vector2 direction)
        {
            Vector2 offset=origin-Body.Center;float b=Vector2.Dot(offset,direction);
            float discriminant=b*b-offset.sqrMagnitude+Body.radius*Body.radius;
            if(discriminant<0)return 1.25f;
            float near=-b-Mathf.Sqrt(discriminant);
            return near>0?Mathf.Min(1.25f,near):1.25f;
        }
        static void SetLine(LineRenderer line,Vector2 from,Vector2 to)
        {line.SetPosition(0,new Vector3(from.x,from.y,-.7f));line.SetPosition(1,new Vector3(to.x,to.y,-.7f));}
    }
}
