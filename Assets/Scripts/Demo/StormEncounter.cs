using UnityEngine;

namespace FlyMeToTheMoon.Demo
{
    public sealed class StormEncounter : WorldEncounter
    {
        public float Cycle { get; private set; }
        public readonly bool[] Repaired = new bool[2];
        public readonly EncounterShotTarget[] Switches = new EncounterShotTarget[2];
        public bool EvadedLow { get; private set; }
        public bool EvadedHigh { get; private set; }
        public bool ReadyForRepair => (Stage==1 || EvadedLow) && (Stage==0 || EvadedHigh);
        public bool Exposed => Cycle>=5.8f && Cycle<9f;
        public int NextSwitch => Repaired[Stage%2] ? 1-Stage%2 : Stage%2;
        public bool Sheltered => Near(0,.9f,.85f);
        public int HitsTaken { get; private set; }
        public override Vector2 Goal => Exposed ? (Vector2)Switches[NextSwitch].transform.position : At(0);
        readonly PrototypeShape shelter, lowWave, highWave, warning;
        float grace;
        public StormEncounter(DemoChallenge session) : base(session)
        {
            shelter=Surface("High-wave shelter",0,.95f,new Vector2(1.8f,.12f),new Color(.35f,.75f,1));Label(shelter.transform,"SHELTER");
            var machine=Surface("Damaged shrine",4.1f,.3f,new Vector2(1.8f,.5f),new Color(.35f,.4f,.55f));Label(machine.transform,"SHRINE");
            for(int i=0;i<2;i++){int index=i;Switches[i]=Receiver("SWITCH "+(i+1),3.5f+i*1.1f,.85f,new Color(.4f,.4f,.45f),echo=>HitSwitch(index,echo));}
            lowWave=Surface("Low wave",-8,.4f,new Vector2(.35f,.7f),new Color(1,.55f,.3f));
            highWave=Surface("High wave",8,2.1f,new Vector2(.4f,3.7f),new Color(.65f,.5f,1));
            warning=Surface("Storm warning",0,2.2f,Vector2.one*.3f,new Color(1,.65f,.3f),true);Label(warning.transform,"!");
        }
        public override void Reset(){Cycle=0;Repaired[0]=Repaired[1]=false;EvadedLow=EvadedHigh=false;grace=1;Spawn();}
        void HitSwitch(int index, bool echo)
        {
            if(echo || !Exposed || !ReadyForRepair || Repaired[index] || index!=NextSwitch || !OnMoon || !Game.Player.IsGrounded || Vector2.Distance(Game.Player.Body.position,Switches[index].transform.position)>2.4f)return;
            Repaired[index]=true;Game.Audio.Note(0,index+4);
            if(Repaired[0] && Repaired[1])
            {
                Stage++;if(Stage==3){Complete=true;return;}
                Cycle=0;Repaired[0]=Repaired[1]=false;EvadedLow=EvadedHigh=false;grace=1;Game.Tell(Stage==1?"Direction reversed — find shelter":"Low, then high — combine both defenses");
            }
        }
        public override void Tick(float delta, bool calling)
        {
            Cycle+=delta;if(Cycle>=9){Cycle%=9;EvadedLow=EvadedHigh=false;}grace=Mathf.Max(0,grace-delta);
            bool low=Stage!=1, high=Stage!=0;
            int direction=Stage==2?-1:1;
            bool lowHit=Wave(lowWave,low,1.4f,direction,false,delta);
            bool highHit=Wave(highWave,high,Stage==2?3.2f:1.4f,Stage==2?1:-1,true,delta);
            if((lowHit || highHit) && grace<=0)
            {
                HitsTaken++;Repaired[0]=Repaired[1]=false;EvadedLow=EvadedHigh=false;Cycle=0;grace=1.2f;
                Vector2 up=Game.Player.Up;Game.Player.Body.linearVelocity=up*2.5f+new Vector2(up.y,-up.x)*(lowHit?direction:-direction)*3;
                Game.Tell("Hit — this circuit reset");
            }
            warning.gameObject.SetActive(Cycle<1.4f);
            Colorize(shelter,Sheltered?DemoGame.Accent:new Color(.35f,.75f,1));
            for(int i=0;i<2;i++)Colorize(Switches[i].GetComponent<PrototypeShape>(),Repaired[i]?DemoGame.Accent:Exposed && ReadyForRepair && i==NextSwitch?new Color(1,.85f,.3f):new Color(.32f,.35f,.45f));
            string pattern=Stage==0?"LOW clockwise":Stage==1?"HIGH counterclockwise":"LOW counterclockwise / HIGH clockwise";
            Status="Circuit "+(Stage+1)+" / 3  ·  "+(Exposed?(ReadyForRepair?"REPAIR "+(NextSwitch+1)+" — approach and shoot":"EVADE THE PULSE TO CHARGE THE SHRINE"):Cycle<1.4f?pattern+" in "+(1.4f-Cycle).ToString("0.0")+"s":pattern);
        }
        bool Wave(PrototypeShape shape, bool enabled, float launch, int direction, bool high, float delta)
        {
            float age=Cycle-launch;bool active=enabled && age>=0 && age<=2.6f;shape.gameObject.SetActive(active);if(!active)return false;
            float s=direction*(-8+age*6.5f);Place(shape,s,high?2.1f:.4f);
            float previous=s-direction*6.5f*delta;
            bool crossed=PlayerS>=Mathf.Min(previous,s)-.4f && PlayerS<=Mathf.Max(previous,s)+.4f;
            if(OnMoon && crossed)
            {
                if(high && Sheltered && Game.Player.IsGrounded)EvadedHigh=true;
                if(!high && !Game.Player.IsGrounded && PlayerHeight>=1.1f && PlayerHeight<4.2f)EvadedLow=true;
            }
            return OnMoon && crossed && PlayerHeight<(high?4.2f:1.1f) && !(high && Sheltered);
        }
    }
}
