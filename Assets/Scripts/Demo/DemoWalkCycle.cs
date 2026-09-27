using System.Collections.Generic;
using UnityEngine;

namespace FlyMeToTheMoon.Demo
{
    /// <summary>Animates the existing traveler artwork as two independent, alternating legs.</summary>
    public sealed class DemoWalkCycle : MonoBehaviour
    {
        public bool Visible => root != null && root.gameObject.activeSelf;
        public float Phase { get; private set; }
        public Vector2 FrontFoot { get; private set; }
        public Vector2 BackFoot { get; private set; }
        public const float CycleDistance = 3.6f;
        public string UpperPose => torsoArt.sprite.name;
        const float HipHeight = .485f, ThighLength = .225f, ShinLength = .175f;
        Transform root, torso;
        readonly Transform[] thighs = new Transform[2], shins = new Transform[2], boots = new Transform[2];
        readonly Sprite[] parts = new Sprite[4];
        SpriteRenderer source, torsoArt;
        float walkWeight, visualSpeed;
        readonly Dictionary<Sprite, Sprite> upperPoses = new Dictionary<Sprite, Sprite>();

        public void Build(SpriteRenderer traveler)
        {
            source = traveler;
            var sprite = PixelArtLibrary.Get("hero/0");
            // Crops reference the existing sheet directly: no image resampling, imports, or new textures.
            parts[0] = Crop(sprite, 0, 104, 194, 253, new Vector2(.5f,0), "Walk torso");
            parts[1] = Crop(sprite, 110, 60, 41, 64, new Vector2(13f/41,1), "Walk thigh");
            parts[2] = Crop(sprite, 110, 38, 38, 26, new Vector2(13f/38,1), "Walk shin");
            parts[3] = Crop(sprite, 107, 0, 63, 44, new Vector2(16f/63,.82f), "Walk boot");
            root = new GameObject("Alternating walk cycle").transform; root.SetParent(transform,false);
            for(int leg=0;leg<2;leg++)
            {
                int order=leg==0?18:19;
                thighs[leg]=Part(parts[1],order,leg==0);
                shins[leg]=Part(parts[2],order,leg==0);
                boots[leg]=Part(parts[3],order,leg==0);
                thighs[leg].localScale=new Vector3(1,ThighLength/parts[1].bounds.size.y,1);
                shins[leg].localScale=new Vector3(1,ShinLength/parts[2].bounds.size.y,1);
            }
            torso=Part(parts[0],20,false);torsoArt=torso.GetComponent<SpriteRenderer>();
            upperPoses.Add(sprite,parts[0]);
            SetVisible(false);
        }
        static Sprite Crop(Sprite original,int x,int y,int width,int height,Vector2 pivot,string name)
        {
            var rect=original.rect;
            var sprite=Sprite.Create(original.texture,new Rect(rect.x+x,rect.y+y,width,height),pivot,original.pixelsPerUnit,0,SpriteMeshType.FullRect);
            sprite.name=name;return sprite;
        }
        Transform Part(Sprite sprite,int order,bool back)
        {
            var go=new GameObject((back?"Far ":"Near ")+sprite.name);go.transform.SetParent(root,false);
            var renderer=go.AddComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.sharedMaterial=source.sharedMaterial;
            renderer.sortingOrder=order;renderer.color=back?new Color(.74f,.78f,.86f):Color.white;
            return go.transform;
        }
        public void SetVisible(bool value)
        {
            if(root==null)return;
            root.gameObject.SetActive(value);source.enabled=!value;
            if(!value){Phase=0;walkWeight=0;visualSpeed=0;}
        }
        public void UpdateMovement(float speed,float deltaTime,bool facingLeft)
        {
            speed=Mathf.Abs(speed);
            // Ease the stride in/out independently of the controller, so input stays responsive.
            float targetWeight=Mathf.Clamp01((speed-.15f)/1.65f);
            walkWeight=Mathf.MoveTowards(walkWeight,targetWeight,deltaTime/.12f);
            if(walkWeight<=0){SetVisible(false);return;}
            visualSpeed=Mathf.Lerp(visualSpeed,speed,1-Mathf.Exp(-12*deltaTime));
            Phase=Mathf.Repeat(Phase+Mathf.Min(visualSpeed/CycleDistance,1.3f)*deltaTime,1);
            ApplyPose(facingLeft,Mathf.SmoothStep(0,1,walkWeight));
        }
        public void Step(float distance,bool facingLeft)
        {
            Phase=Mathf.Repeat(Phase+Mathf.Abs(distance)/CycleDistance,1);
            ApplyPose(facingLeft,1);
        }
        void ApplyPose(bool facingLeft,float weight)
        {
            SetVisible(true);
            root.localPosition=source.transform.localPosition;
            // Lean belongs to the torso; planted boots stay parallel to the local ground.
            root.localRotation=Quaternion.identity;
            root.localScale=new Vector3(facingLeft?-1:1,1,1);
            float bob=-.004f*(1-Mathf.Cos(Phase*Mathf.PI*4))*weight;
            if(!upperPoses.TryGetValue(source.sprite,out var upper))
            {
                var pose=source.sprite;int cut=Mathf.RoundToInt(104f/250*pose.pixelsPerUnit);
                upper=Crop(pose,0,cut,(int)pose.rect.width,(int)pose.rect.height-cut,new Vector2(.5f,0),"Walk torso "+pose.name);
                upperPoses.Add(pose,upper);
            }
            torsoArt.sprite=upper;
            torso.localPosition=new Vector3(0,104f/250+bob,0);
            float lean=Mathf.DeltaAngle(0,source.transform.localEulerAngles.z)*(facingLeft?-1:1);
            torso.localRotation=Quaternion.Euler(0,0,lean*.45f*weight);
            BackFoot=AnimateLeg(0,Mathf.Repeat(Phase+.5f,1),bob,weight);
            FrontFoot=AnimateLeg(1,Phase,bob,weight);
        }
        Vector2 AnimateLeg(int leg,float phase,float bob,float weight)
        {
            // A longer contact period gives a brief double-support pose. Both ends of the
            // low swing arc have zero vertical velocity, rather than snapping off the ground.
            const float stride=.16f,stanceFraction=.6f;
            bool stance=phase<stanceFraction;
            float t=stance?phase/stanceFraction:(phase-stanceFraction)/(1-stanceFraction);
            float x=(stance?1:-1)*stride*Mathf.Cos(t*Mathf.PI);
            float arc=stance?0:Mathf.Sin(t*Mathf.PI);
            float lift=arc*arc*.065f*weight;
            Vector2 sole=new Vector2(.085f+Mathf.Lerp(leg==0?-.035f:.035f,x,weight),lift);
            float roll=stance?0:-Mathf.Sin(t*Mathf.PI*2)*9*arc*weight;
            var bootRotation=Quaternion.Euler(0,0,roll);
            Vector2 ankle=sole+(Vector2)(bootRotation*Vector3.up)*(44f/250*.82f);
            Vector2 hip=new Vector2(.085f+(leg==0?-.025f:.025f),HipHeight+bob);
            Vector2 delta=ankle-hip;
            float distance=Mathf.Clamp(delta.magnitude,.001f,ThighLength+ShinLength-.001f);
            Vector2 direction=delta.normalized;
            float along=(ThighLength*ThighLength-ShinLength*ShinLength+distance*distance)/(2*distance);
            float bend=Mathf.Sqrt(Mathf.Max(0,ThighLength*ThighLength-along*along));
            Vector2 knee=hip+direction*along+new Vector2(-direction.y,direction.x)*bend;
            thighs[leg].localPosition=hip;thighs[leg].localRotation=Quaternion.FromToRotation(Vector3.down,knee-hip);
            shins[leg].localPosition=knee;shins[leg].localRotation=Quaternion.FromToRotation(Vector3.down,ankle-knee);
            // Boots remain level with the local surface during contact, including under a moon.
            boots[leg].localPosition=ankle;boots[leg].localRotation=bootRotation;
            return sole;
        }
        void OnDestroy()
        {
            foreach(var sprite in upperPoses.Values)if(sprite!=null && sprite!=parts[0])Destroy(sprite);
            foreach(var sprite in parts)if(sprite!=null)Destroy(sprite);
        }
    }
}
