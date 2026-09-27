using UnityEngine;

namespace FlyMeToTheMoon.Demo
{
    public sealed class RelayEncounter : WorldEncounter
    {
        public readonly GravityBody[] Route = new GravityBody[4];
        public readonly Vector2[] Shrines = new Vector2[4];
        public readonly Vector2[] GateCenters = new Vector2[3];
        public int Passed { get; private set; }
        public int Checkpoint { get; private set; }
        public bool Carrying { get; private set; }
        public override Vector2 Goal => !Carrying ? (Vector2)spark.transform.position : Passed<3?GateCenters[Passed]:Shrines[3];
        readonly PrototypeShape spark;
        readonly PrototypeShape[][] gates = new PrototypeShape[3][];
        readonly Vector2[] gateBases = new Vector2[3], sideways = new Vector2[3];
        Vector2 previous;
        public RelayEncounter(DemoChallenge session) : base(session)
        {
            var bodies=Game.Player.gravityManager.bodies;
            // Follow actual neighboring moons in the scattered field, including its bends.
            for(int i=0;i<4;i++)Route[i]=bodies[Game.Melody.RelayMoons[i]+1];
            Shrines[0]=At(0,.3f);
            for(int i=0;i<3;i++)
            {
                Vector2 direction=(Route[i+1].Center-Route[i].Center).normalized;
                float gap=Vector2.Distance(Route[i].Center,Route[i+1].Center)-Route[i].radius-Route[i+1].radius;
                sideways[i]=new Vector2(direction.y,-direction.x);
                gateBases[i]=Route[i].Center+direction*(Route[i].radius+gap*.5f)+(i==1?sideways[i]*.8f:Vector2.zero);
                GateCenters[i]=gateBases[i];gates[i]=new PrototypeShape[12];
                for(int j=0;j<12;j++)gates[i][j]=Shape("Relay gate "+(i+1),gateBases[i],Vector2.one*.16f,new Color(1,.83f,.25f),true);
                Label(gates[i][0].transform,"GATE "+(i+1));
                Shrines[i+1]=Route[i+1].Center-direction*(Route[i+1].radius+.3f);
                var shrine=Shape(i==2?"Spark receiver":"Stabilizing shrine",Shrines[i+1],new Vector2(1,.16f),i==2?DemoGame.Accent:new Color(.35f,.65f,1));
                shrine.transform.rotation=Quaternion.FromToRotation(Vector3.up,Route[i+1].UpAt(Shrines[i+1]));
                Label(shrine.transform,i==2?"E: RECEIVER":"LAND: SAVE");
                // Route breadcrumbs show the current launch direction even when the next moon is offscreen.
                for(int j=1;j<=8;j++)Shape("Relay route",Vector2.Lerp(Route[i].Center+direction*(Route[i].radius+.5f),Shrines[i+1],j/9f),Vector2.one*.065f,new Color(.6f,.65f,.3f),true);
            }
            spark=Shape("Musical spark",Shrines[0]+Up*.5f,Vector2.one*.32f,new Color(1,.9f,.4f),true);Label(spark.transform,"SPARK");
        }
        public override void Reset()
        {
            Passed=Checkpoint;Carrying=Checkpoint>0;
            Game.Player.Respawn(Route[Checkpoint],Shrines[Checkpoint]-Route[Checkpoint].Center);Game.Planets.cameraController.SnapToPlayer();previous=Game.Player.Body.position;
            if(!Carrying)spark.transform.position=Shrines[0]+Up*.5f;
        }
        public override void Interact()
        {
            if(Carrying && Passed==3 && Game.Player.IsGrounded && Game.Player.gravityManager.CurrentBody==Route[3] && Vector2.Distance(Game.Player.Body.position,Shrines[3])<1.3f)Complete=true;
        }
        public override void Tick(float delta, bool calling)
        {
            GateCenters[2]=gateBases[2]+sideways[2]*Mathf.Sin(Session.Elapsed*1.1f)*.7f;
            for(int i=0;i<3;i++)for(int j=0;j<12;j++)
            {
                float angle=j*Mathf.PI/6;
                Vector2 point=GateCenters[i]+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*1.2f;
                gates[i][j].transform.position=new Vector3(point.x,point.y,-.55f);
                Colorize(gates[i][j],i<Passed?DemoGame.Accent:new Color(1,.83f,.25f));
            }
            Vector2 position=Game.Player.Body.position;
            if(!Carrying && Vector2.Distance(position,spark.transform.position)<1)Carrying=true;
            if(Carrying)
            {
                spark.transform.position=(Vector3)(position+Game.Player.Up*.8f)+Vector3.back*.7f;
                if(Passed<3 && !Game.Player.IsGrounded && delta>0 && Vector2.Distance(previous,position)<=Game.Player.maxFlightSpeed*delta+1f
                    && Vector2.Dot(position-previous,Route[Passed+1].Center-Route[Passed].Center)>0
                    && SegmentDistance(GateCenters[Passed],previous,position)<1.05f){Passed++;Game.Audio.Note(0,Passed+2);}
                for(int i=Checkpoint+1;i<=Mathf.Min(Passed,2);i++)
                    if(Game.Player.IsGrounded && Game.Player.gravityManager.CurrentBody==Route[i] && Vector2.Distance(position,Shrines[i])<1.3f)
                    {Checkpoint=i;Game.Tell("Spark stabilized — checkpoint "+i);}
            }
            previous=position;Status="Gates "+Passed+" / 3  ·  Shrine "+Checkpoint+" / 2";
        }
    }
}
