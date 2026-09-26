using System;
using System.Collections.Generic;
using UnityEngine;

namespace FlyMeToTheMoon.Demo
{
    public sealed class EncounterShotTarget : MonoBehaviour
    {
        public Action<bool> Hit;
        public DemoChallenge Challenge;
    }
    public sealed class EncounterLabel
    {
        public Transform Target;
        public string Text;
    }
    public sealed class EncounterGate
    {
        public PrototypeShape Shape;
        public BoxCollider2D Collider;
        public bool Open { get; private set; }
        public void SetOpen(bool open)
        {
            if (Open == open && Collider.enabled == !open) return;
            Open = open; Collider.enabled = !open;
            Shape.color = open ? new Color(.2f,.65f,.55f,.25f) : new Color(.9f,.55f,.25f);
            Shape.Rebuild();
        }
    }
    public abstract class WorldEncounter
    {
        protected readonly DemoChallenge Session;
        protected DemoGame Game => Session.Game;
        public GravityBody Body { get; private set; }
        public Vector2 Up { get; private set; }
        public bool Complete { get; protected set; }
        public int Retries;
        public int Stage { get; protected set; }
        public string Status { get; protected set; }
        public virtual float Meter => -1;
        public virtual string MeterLabel => "";
        public virtual Vector2 Goal => At(0,.5f);
        public readonly List<EncounterLabel> Labels = new List<EncounterLabel>();
        protected float Circumference => 2 * Mathf.PI * (Body.radius + .29f);
        protected float PlayerS => Along(Game.Player.Body.position);
        protected float PlayerHeight => Body.SurfaceDistance(Game.Player.Body.position);
        protected bool OnMoon => Game.Player.gravityManager.CurrentBody == Body;
        protected WorldEncounter(DemoChallenge session)
        {
            Session = session; var station = Game.Melody.Stations[session.Index];
            Body = station.Body; Up = Body.UpAt(station.transform.position);
        }
        public Vector2 At(float distance, float height = .3f)
        {
            Vector2 direction = Quaternion.Euler(0,0,-distance/(Body.radius+.29f)*Mathf.Rad2Deg) * Up;
            return Body.Center + direction * (Body.radius + height);
        }
        public float Along(Vector2 position) => -Vector2.SignedAngle(Up, Body.UpAt(position)) * Mathf.Deg2Rad * (Body.radius + .29f);
        public float ArcDelta(float from, float to) => Mathf.DeltaAngle(from/Circumference*360,to/Circumference*360)/360*Circumference;
        protected bool Near(float s, float radius = .7f, float maxHeight = 1f) => OnMoon && Mathf.Abs(ArcDelta(PlayerS,s)) < radius && PlayerHeight < maxHeight;
        public abstract void Reset();
        public abstract void Tick(float delta, bool calling);
        public virtual void Interact() { }
        public virtual void Record() { }
        public virtual void Shot(Vector2 position, Vector2 direction) { }
        protected void Spawn(float s = 0)
        {
            Game.Player.Respawn(Body, At(s)-Body.Center); Game.Planets.cameraController.SnapToPlayer();
        }
        protected PrototypeShape Shape(string name, Vector2 position, Vector2 size, Color color, bool circle = false)
            => DemoWorld.Shape(Session.Root,name,position,size,color,Game.Material,circle);
        protected PrototypeShape Surface(string name, float s, float height, Vector2 size, Color color, bool circle = false)
        {
            var shape=Shape(name,At(s,height),size,color,circle);
            shape.transform.rotation=Quaternion.FromToRotation(Vector3.up,Body.UpAt(shape.transform.position));return shape;
        }
        protected void Place(PrototypeShape shape, float s, float height)
        {
            shape.transform.position=new Vector3(At(s,height).x,At(s,height).y,-.6f);
            shape.transform.rotation=Quaternion.FromToRotation(Vector3.up,Body.UpAt(shape.transform.position));
        }
        protected void Colorize(PrototypeShape shape, Color color)
        {
            if(shape.color==color)return;shape.color=color;shape.Rebuild();
        }
        protected EncounterLabel Label(Transform target, string text)
        {
            var label=new EncounterLabel {Target=target,Text=text};Labels.Add(label);return label;
        }
        protected EncounterShotTarget Receiver(string name, float s, float height, Color color, Action<bool> hit)
        {
            var shape=Surface(name,s,height,Vector2.one*.55f,color,true);
            var collider=shape.gameObject.AddComponent<CircleCollider2D>();collider.radius=.34f;collider.isTrigger=true;
            var target=shape.gameObject.AddComponent<EncounterShotTarget>();target.Hit=hit;target.Challenge=Session;Label(target.transform,name);return target;
        }
        protected EncounterGate Gate(string name, float s, float height = 3.2f)
        {
            var shape=Surface(name,s,height*.5f,new Vector2(.22f,height),new Color(.9f,.55f,.25f));
            var collider=shape.gameObject.AddComponent<BoxCollider2D>();collider.size=shape.size;
            var gate=new EncounterGate{Shape=shape,Collider=collider};gate.SetOpen(false);return gate;
        }
        protected static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 line=b-a;float t=line.sqrMagnitude<.0001f?0:Mathf.Clamp01(Vector2.Dot(p-a,line)/line.sqrMagnitude);
            return Vector2.Distance(p,a+t*line);
        }
    }
}
