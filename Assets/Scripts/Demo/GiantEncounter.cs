using UnityEngine;

namespace FlyMeToTheMoon.Demo
{
    public sealed class GiantEncounter : WorldEncounter
    {
        public float Awareness { get; private set; }
        public float LureRemaining { get; private set; }
        public float Attention { get; private set; }
        public float SleepingPosition { get; private set; }
        public int Wakes { get; private set; }
        public readonly EncounterShotTarget[] Bells = new EncounterShotTarget[2];
        public const float PagePosition = 7.7f;
        public static readonly float[] SealPositions = {1.4f,5.5f};
        public int SealsOpened { get; private set; }
        public int LuredBell { get; private set; } = -1;
        readonly PrototypeShape[] seals = new PrototypeShape[2];
        float openingTime; bool opening;
        public override float Meter => Awareness;
        public override string MeterLabel => "AWARENESS";
        public override Vector2 Goal => SealsOpened==2 ? At(PagePosition,.65f) : LureRemaining>0 && LuredBell==SealsOpened ? At(SealPositions[SealsOpened]) : (Vector2)Bells[SealsOpened].transform.position;
        readonly PrototypeShape[] giant = new PrototypeShape[4];
        readonly PrototypeShape page, eye;
        int jumps;
        float lureAt;
        public GiantEncounter(DemoChallenge session) : base(session)
        {
            for(int i=0;i<giant.Length;i++)giant[i]=Surface("Sleeping giant",4.5f+i*.8f,.45f,new Vector2(1.2f,.95f),new Color(.5f,.42f,.68f),true);
            Label(giant[1].transform,"Z z z");
            eye=Surface("Listening eye",5.8f,.95f,new Vector2(.17f,.06f),new Color(.9f,.85f,1));
            page=Surface("Guarded page",PagePosition,.65f,new Vector2(.55f,.7f),new Color(1,.85f,.3f));Label(page.transform,"E: PAGE");
            for(int i=0;i<2;i++)
            {
                float s=i==0?-3:2.7f;
                int bellIndex=i;
                Bells[i]=Receiver("BELL",s,1.05f,new Color(.5f,.9f,1),echo=>{if(!echo)Ring(s,bellIndex);});
                seals[i]=Surface("Nest seal",SealPositions[i],.15f,new Vector2(.55f,.3f),new Color(.65f,.55f,.9f));Label(seals[i].transform,"E");
            }
            foreach(float s in new[]{2.2f,5.2f}){var patch=Surface("Noisy gravel",s,.07f,new Vector2(1.3f,.15f),new Color(.9f,.4f,.4f));Label(patch.transform,"GRAVEL");}
        }
        public override void Reset(){Awareness=LureRemaining=openingTime=0;SealsOpened=0;LuredBell=-1;opening=false;jumps=Game.Player.NormalJumpCount+Game.Player.FluteJumpCount;Spawn();}
        public override void Shot(Vector2 position, Vector2 direction)
        {
            float distance=Mathf.Abs(ArcDelta(Along(position),SleepingPosition));
            Awareness=Mathf.Clamp01(Awareness+(distance<4?.28f:.11f));
        }
        void Ring(float s,int index)
        {
            lureAt=s;LureRemaining=6;LuredBell=index;Awareness=Mathf.Max(0,Awareness-.25f);Game.Audio.Note(0,6);Game.Tell("The giant listens to the bell");
        }
        public override void Interact()
        {
            if(SealsOpened<2 && Near(SealPositions[SealsOpened],.8f,.7f) && Game.Player.IsGrounded)
            {
                if(LureRemaining>1 && LuredBell==SealsOpened && Awareness<.55f)
                { opening=true;openingTime=0;Game.Tell("Stay still while the seal unwinds"); }
                else Game.Tell(Awareness>=.55f?"Too restless — stand quietly until awareness falls":"Ring the marked bell, then quietly open this seal");
            }
            if(Near(PagePosition,.95f,1.2f))
            {
                if(SealsOpened==2 && Game.Player.IsGrounded && LureRemaining>0 && Awareness<.55f && Mathf.Abs(ArcDelta(Attention,PagePosition))>1.8f) Complete=true;
                else Game.Tell(SealsOpened<2?"The page is bound by two nest seals":"Distract the giant before taking the page");
            }
        }
        public override void Tick(float delta, bool calling)
        {
            SleepingPosition=5.5f+Mathf.Sin(Session.Elapsed*.28f)*1.05f;
            LureRemaining=Mathf.Max(0,LureRemaining-delta);Attention=LureRemaining>0?lureAt:SleepingPosition;
            int count=Game.Player.NormalJumpCount+Game.Player.FluteJumpCount;
            if(count!=jumps){if(OnMoon)Awareness+=.2f;jumps=count;}
            bool gravel=(Mathf.Abs(ArcDelta(PlayerS,2.2f))<.85f || Mathf.Abs(ArcDelta(PlayerS,5.2f))<.85f) && Game.Player.IsGrounded;
            bool exposed=OnMoon && Mathf.Abs(ArcDelta(Attention,PlayerS))<2.3f && PlayerHeight<3;
            bool walking=Game.Player.Body.linearVelocity.magnitude>.3f;
            float rate=exposed?.32f:-.14f;
            if(OnMoon && walking && Game.Player.IsGrounded)rate+=gravel?.42f:.025f;
            if(calling && OnMoon)rate+=.28f;
            Awareness=Mathf.Clamp01(Awareness+rate*delta);
            if(Awareness>=1){Wakes++;Reset();Game.Tell("The giant woke — try another approach");}
            if(opening)
            {
                bool safe=SealsOpened<2 && Near(SealPositions[SealsOpened],.8f,.7f) && Game.Player.IsGrounded && !walking && Awareness<.55f && LureRemaining>0 && LuredBell==SealsOpened;
                if(!safe){opening=false;openingTime=0;Game.Tell(walking?"Keep still, then press E again":Awareness>=.55f?"Too restless — wait for quiet":"The distraction ended — ring the bell again");}
                else if((openingTime+=delta)>=.65f)
                { SealsOpened++;opening=false;Game.Audio.Note(1,SealsOpened+3);Game.Tell(SealsOpened==2?"Both seals released — take the page":"One seal released — use the other bell"); }
            }
            for(int i=0;i<seals.Length;i++)Colorize(seals[i],i<SealsOpened?DemoGame.Accent:new Color(.65f,.55f,.9f));
            for(int i=0;i<giant.Length;i++){Place(giant[i],SleepingPosition+(i-1.5f)*.8f,.45f+Mathf.Sin(Session.Elapsed*1.5f)*.035f);Colorize(giant[i],Color.Lerp(new Color(.5f,.42f,.68f),new Color(1,.4f,.3f),Awareness));}
            Place(eye,SleepingPosition+Mathf.Sign(ArcDelta(SleepingPosition,Attention))*.4f,.94f);
            Status="Seals "+SealsOpened+" / 2  ·  "+(opening?"Opening…":LureRemaining>0?"Listening to the bell  ·  "+LureRemaining.ToString("0.0")+"s":Awareness>.6f?"RESTLESS — find quiet ground":"SLEEPING — walk quietly");
        }
    }
}
