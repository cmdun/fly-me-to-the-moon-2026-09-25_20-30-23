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
        public const float CycleDistance = 1.8f;
        public string UpperPose => torsoArt.sprite.name;
        const float HipHeight = .485f, ThighLength = .225f, ShinLength = .175f;
        Transform root, torso;
        readonly Transform[] thighs = new Transform[2], shins = new Transform[2], boots = new Transform[2];
        readonly Sprite[] parts = new Sprite[4];
        SpriteRenderer source, torsoArt;
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
            if(!value)Phase=0;
        }
        public void Step(float distance,bool facingLeft)
        {
            SetVisible(true);
            Phase=Mathf.Repeat(Phase+Mathf.Abs(distance)/CycleDistance,1);
            root.localPosition=source.transform.localPosition;
            root.localRotation=source.transform.localRotation;
            root.localScale=new Vector3(facingLeft?-1:1,1,1);
            // Two footfalls per cycle; the shoulders dip slightly over each planted foot.
            float bob=-.012f*(1-Mathf.Cos(Phase*Mathf.PI*4));
            if(!upperPoses.TryGetValue(source.sprite,out var upper))
            {
                var pose=source.sprite;int cut=Mathf.RoundToInt(104f/250*pose.pixelsPerUnit);
                upper=Crop(pose,0,cut,(int)pose.rect.width,(int)pose.rect.height-cut,new Vector2(.5f,0),"Walk torso "+pose.name);
                upperPoses.Add(pose,upper);
            }
            torsoArt.sprite=upper;
            torso.localPosition=new Vector3(0,104f/250+bob,0);
            BackFoot=AnimateLeg(0,Mathf.Repeat(Phase+.5f,1),bob);
            FrontFoot=AnimateLeg(1,Phase,bob);
        }
        Vector2 AnimateLeg(int leg,float phase,float bob)
        {
            // Stance: the planted foot travels backward relative to the body. Swing: lift and pass it forward.
            const float stride=.18f;
            bool stance=phase<.5f;float t=stance?phase*2:(phase-.5f)*2;
            float x=stance?Mathf.Lerp(stride,-stride,t):Mathf.Lerp(-stride,stride,t*t*(3-2*t));
            float lift=stance?0:Mathf.Sin(t*Mathf.PI)*.12f;
            Vector2 sole=new Vector2(.085f+x,lift);
            Vector2 ankle=sole+Vector2.up*(44f/250*.82f);
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
            boots[leg].localPosition=ankle;boots[leg].localRotation=Quaternion.identity;
            return sole;
        }
        void OnDestroy()
        {
            foreach(var sprite in upperPoses.Values)if(sprite!=null && sprite!=parts[0])Destroy(sprite);
            foreach(var sprite in parts)if(sprite!=null)Destroy(sprite);
        }
    }
}
