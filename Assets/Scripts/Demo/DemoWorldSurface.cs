using UnityEngine;
namespace FlyMeToTheMoon.Demo
{
    /// <summary>A small authored palette and consistent pixel grid replace oversized baked scenery.</summary>
    public sealed class DemoWorldSurface : MonoBehaviour
    {
        Material material;
        public static readonly Color[] Palette = {
            new Color(.15f,.5f,.48f), new Color(.19f,.34f,.63f), new Color(.26f,.48f,.3f),
            new Color(.65f,.32f,.22f), new Color(.53f,.26f,.44f), new Color(.4f,.28f,.62f)
        };
        public void Build(SpriteRenderer world, GravityBody body, int index, int seed)
        {
            material=new Material(Resources.Load<Shader>("PixelArt/WorldSurface")){name="Illustrated moon surface"};
            var rect=world.sprite.rect;var texture=world.sprite.texture;
            material.SetVector("_UVRect",new Vector4(rect.x/texture.width,rect.y/texture.height,rect.width/texture.width,rect.height/texture.height));
            var color=index==0?new Color(.28f,.33f,.4f):Palette[(index-1)%6];
            material.SetColor("_Base",color);
            material.SetColor("_Light",Color.Lerp(color,new Color(.83f,.92f,.78f),.4f));
            material.SetColor("_Dark",Color.Lerp(color,new Color(.04f,.07f,.14f),.55f));
            material.SetColor("_Rim",index==0?new Color(.3f,.63f,.32f):Color.Lerp(color,new Color(.66f,.88f,.74f),.35f));
            material.SetFloat("_Diameter",body.radius*2);material.SetFloat("_Home",index==0?1:0);
            material.SetFloat("_Seed",(seed&1023)+index*37);world.sharedMaterial=material;
        }
        void OnDestroy(){if(material!=null)Destroy(material);}
    }
}
