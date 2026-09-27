using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FlyMeToTheMoon.Demo
{
    /// <summary>Optional, persistent moon adventures. Rewards are unique, and can be brought home together.</summary>
    public sealed class DemoDiscoveries : MonoBehaviour
    {
        public const int Count = 12;
        public readonly List<MoonDiscovery> Sites = new List<MoonDiscovery>();
        public bool[] Found { get; private set; } = new bool[Count];
        public bool[] Delivered { get; private set; } = new bool[Count];
        public bool Festival { get; private set; }
        public int BestPerformance { get; private set; }
        public Transform Garden { get; private set; }
        public int FoundCount => Total(Found);
        public int HomeCount => Total(Delivered);
        public int PackedCount => FoundCount - HomeCount;
        public MoonDiscovery Tracked { get; private set; }
        public DemoGame Game { get; private set; }
        Transform gardenLife; int gardenVersion=-1; float concertUntil, nextNote; bool concertQueued;
        readonly List<SpriteRenderer> residents = new List<SpriteRenderer>();
        public static int Total(bool[] values) { int n=0;foreach(bool value in values)if(value)n++;return n; }
        public void Build(DemoGame game, GravityBody[] bodies, DemoSave.Data saved)
        {
            Game=game;
            Restore(saved);
            var candidates=new List<int>();
            for(int i=1;i<bodies.Length;i++)
            {
                bool quest=false;foreach(var station in game.Melody.Stations)quest|=station.Body==bodies[i];
                if(!quest)candidates.Add(i);
            }
            // Mix activities through near and distant moons. No positional pattern is imposed on the galaxy.
            candidates.Sort((a,b)=>game.MoonLayout[a-1].Depth.CompareTo(game.MoonLayout[b-1].Depth));
            var random=new System.Random(game.Seed^0x194fc2);
            for(int i=0;i<Count;i++)
            {
                int start=i*candidates.Count/Count, end=(i+1)*candidates.Count/Count;
                int moon=candidates[random.Next(start,end)];
                var go=new GameObject("Moon discovery "+(i+1));go.transform.SetParent(bodies[moon].transform,false);
                var site=go.AddComponent<MoonDiscovery>();
                site.Build(this,i,moon,bodies[moon],FindDirection(bodies[moon],random),i%3,i/3);
                Sites.Add(site);
            }
            var mount=new GameObject("Home seed garden").transform;mount.SetParent(bodies[0].transform,false);
            Vector2 up=Quaternion.Euler(0,0,-135)*Vector2.up;
            mount.localPosition=up*bodies[0].radius;mount.localRotation=Quaternion.FromToRotation(Vector3.up,up);
            Garden=mount;
            var arch=PixelArtLibrary.Create(mount,"Garden gathering","prop/arch",2);PixelArtLibrary.Height(arch,1.6f);
            gardenLife=new GameObject("Returned life").transform;gardenLife.SetParent(bodies[0].transform,false);
            RefreshGarden();
        }
        public void Restore(DemoSave.Data saved)
        {
            Found=new bool[Count];Delivered=new bool[Count];
            if(saved==null)return;
            for(int i=0;i<Count;i++)
            {
                Found[i]=saved.discoveries!=null && i<saved.discoveries.Length && saved.discoveries[i];
                Delivered[i]=Found[i] && saved.garden!=null && i<saved.garden.Length && saved.garden[i];
            }
            BestPerformance=Mathf.Clamp(saved.bestPerformance,0,DemoRhythm.NoteCount);
            Festival=saved.festival && HomeCount==Count && saved.piano;
        }
        Vector2 FindDirection(GravityBody body,System.Random random)
        {
            Vector2 best=Vector2.up;float score=-1;
            for(int a=0;a<72;a++)
            {
                Vector2 up=Quaternion.Euler(0,0,a*5+random.Next(5))*Vector2.up;
                float clearance=100;
                foreach(var target in Game.Targets)if(target.Body==body)
                    clearance=Mathf.Min(clearance,Mathf.Abs(Vector2.SignedAngle(up,(Vector2)target.transform.position-body.Center))*Mathf.Deg2Rad*body.radius);
                if(clearance>score){score=clearance;best=up;}
            }
            return best;
        }
        public bool Reserved(GravityBody body,Vector2 point,float extra=0)
        {
            foreach(var site in Sites)if(site.Body==body && Mathf.Abs(site.Arc(point))<5.3f+extra)return true;
            return false;
        }
        public bool HasSite(GravityBody body)=>Sites.Exists(s=>s.Body==body);
        public MoonDiscovery Current => Sites.Find(s=>s.Body==Game.Player.gravityManager.CurrentBody);
        public bool Available => Game.State==DemoState.Explore && !Game.MapVisible && Game.Player.IsGrounded;
        public bool NearGarden => Available && Game.Player.gravityManager.CurrentBody==Game.Planets.respawnPlanet && Vector2.Distance(Game.Player.Body.position,Garden.position)<1.65f;
        public Transform NearbyInteraction()
        {
            if(!Available)return null;
            if(NearGarden)return Garden;
            return Current?.NearestControl();
        }
        public bool Interact()
        {
            if(!Available)return false;
            if(NearGarden){GardenDialogue();return true;}
            var site=Current;
            return site!=null && site.Interact();
        }
        public bool NotePassed(Vector2 from,Vector2 to,bool echo)
        {
            if(echo || Game.State!=DemoState.Explore)return false;
            return Current!=null && Current.NotePassed(from,to);
        }
        public bool Award(MoonDiscovery site)
        {
            if(site==null || !Sites.Contains(site) || Found[site.Id] || !site.Solved)return false;
            Found[site.Id]=true;
            Game.Audio.Note(1,site.Id%7,.5f);
            Game.Art.Effect("07",site.RewardPosition,1.1f,Quaternion.FromToRotation(Vector3.up,site.Up));
            Game.Tell(site.Kind==2?"A new friend! Visit the HOME garden.":"Seed safely packed. Plant it at HOME.");
            DemoSave.Write(Game);return true;
        }
        public void Track(int id){Tracked=id>=0 && id<Sites.Count?Sites[id]:null;}
        void GardenDialogue()
        {
            if(PackedCount>0)
                Game.OpenDialogue("The listening garden",PackedCount+" discoveries are ready to join HOME. Plant the seeds and welcome your companions together.","Bring them home",()=>Deliver());
            else if(HomeCount==Count && Game.Melody.Unlocked)
                Game.OpenDialogue("A sky full of friends",Festival?"Your garden remembers its first concert. Gather everyone for another song.":"Every seed and singer is home. Perform at the altar to begin the garden festival.","Play together",Celebrate);
            else Game.OpenDialogue("The listening garden","Bell gardens hold musical seeds. Turn moon reflectors to wake crystal bulbs. Lead lost singers through their lanterns. Bring discoveries here whenever you return.\n\nHome: "+HomeCount+" / 12. Press J for your field journal.","Explore");
        }
        public bool Deliver()
        {
            if(!NearGarden || PackedCount==0)return false;
            for(int i=0;i<Count;i++)Delivered[i]=Found[i];
            RefreshGarden();Celebrate();DemoSave.Write(Game);
            Game.Tell(HomeCount==Count?(Game.Melody.Unlocked?"Garden complete! Play an encore at the altar.":"Garden complete! Restore the score at the altar."):"HOME is growing: "+HomeCount+" / 12");return true;
        }
        public void PerformanceFinished(bool passed,int perfect)
        {
            BestPerformance=Mathf.Max(BestPerformance,Mathf.Clamp(perfect,0,DemoRhythm.NoteCount));
            if(passed && HomeCount==Count && Game.Melody.Unlocked){Festival=true;Celebrate();}
        }
        public void Celebrate(){concertQueued=true;}
        void RefreshGarden()
        {
            if(gardenVersion==HomeCount)return;gardenVersion=HomeCount;
            foreach(Transform child in gardenLife)Destroy(child.gameObject);residents.Clear();
            var home=Game.Planets.respawnPlanet;
            for(int i=0;i<Count;i++)
            {
                float angle=119+i*3;
                Vector2 up=Quaternion.Euler(0,0,-angle)*Vector2.up;
                var mount=new GameObject("Garden place "+i).transform;mount.SetParent(gardenLife,false);
                mount.position=home.Center+up*home.radius;mount.rotation=Quaternion.FromToRotation(Vector3.up,up);
                var art=PixelArtLibrary.Create(mount,Delivered[i]?"Home discovery":"Empty planter",Delivered[i]?(i%3==2?"06/object/5":i%3==1?"prop/crystals":"07/object/5"):"07/object/0",3);
                PixelArtLibrary.Height(art,Delivered[i]?(i%3==2?.48f:.72f):.22f);
                art.color=Color.Lerp(Color.white,DemoWorldSurface.Palette[i%6],.25f);
                if(Delivered[i])residents.Add(art);
            }
        }
        void Update()
        {
            if(Game==null)return;
            bool visible=Game.State!=DemoState.Challenge;
            foreach(var site in Sites)if(site.gameObject.activeSelf!=visible)site.gameObject.SetActive(visible);
            if(Game.State!=DemoState.Explore || Game.MapVisible)return;
            Current?.Tick(Time.deltaTime,Keyboard.current!=null && Keyboard.current.qKey.isPressed);
            if(concertQueued){concertQueued=false;concertUntil=Time.time+12;nextNote=Time.time;}
            if(Time.time<concertUntil && Time.time>=nextNote)
            {
                nextNote=Time.time+.42f;
                int n=Mathf.FloorToInt(Time.time/.42f)%7;Game.Audio.Note(1,n,.13f);
                if(residents.Count>0)
                {
                    var r=residents[n%residents.Count];
                    Game.Art.Effect("03",r.transform.position+r.transform.up*.45f,.45f,r.transform.rotation);
                }
            }
        }
    }
}
