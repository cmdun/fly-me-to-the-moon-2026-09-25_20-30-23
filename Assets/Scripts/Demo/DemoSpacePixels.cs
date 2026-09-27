using UnityEngine;

namespace FlyMeToTheMoon.Demo
{
    /// <summary>Small seeded sky textures, painted once. No frame-time textures or global random state.</summary>
    public static class DemoSpacePixels
    {
        public static Texture2D Nebula(int seed)
        {
            const int size = 512;
            var pixels = new Color32[size * size];
            float offset = (seed & 1023) * .13f;
            for (int y=0;y<size;y++) for (int x=0;x<size;x++)
            {
                float u=x/(float)size, v=y/(float)size;
                // Integer-frequency waves close cleanly across all four tile boundaries.
                float ribbon = Mathf.Sin((v-u)*Mathf.PI*2 + .8f*Mathf.Sin(u*Mathf.PI*2));
                float coarse = PeriodicNoise(u,v,3,offset);
                float detail = PeriodicNoise(u,v,9,offset+51);
                float clouds = Mathf.Clamp01((1-Mathf.Abs(ribbon))*(.5f+coarse)*1.35f-.35f);
                clouds *= .5f + detail;
                float shade = Mathf.Floor(Mathf.Clamp01(clouds)*7)/7;
                Color hue=Color.Lerp(new Color(.075f,.20f,.24f),new Color(.23f,.12f,.32f),
                    .5f+.5f*Mathf.Sin(u*Mathf.PI*2+offset));
                Color color=Color.Lerp(new Color(.016f,.027f,.061f),hue,shade*.75f);
                // Sparse dither softens palette boundaries without bloom or blurred texture filtering.
                if (((x+y*3)&7)==0 && clouds>shade+.06f) color=Color.Lerp(color,hue,.065f);
                pixels[y*size+x]=color;
            }
            return Texture("Space / indigo and teal nebula",size,pixels);
        }

        static float PeriodicNoise(float u,float v,float scale,float offset)
        {
            float a=Mathf.PerlinNoise(u*scale+offset,v*scale+offset);
            float b=Mathf.PerlinNoise((u-1)*scale+offset,v*scale+offset);
            float c=Mathf.PerlinNoise(u*scale+offset,(v-1)*scale+offset);
            float d=Mathf.PerlinNoise((u-1)*scale+offset,(v-1)*scale+offset);
            return Mathf.Lerp(Mathf.Lerp(a,b,u),Mathf.Lerp(c,d,u),v);
        }

        public static Texture2D Stars(int seed,int size,int count,int depth,bool galaxies)
        {
            var random=new System.Random(seed);
            var pixels=new Color32[size*size];
            if(galaxies)
            {
                Galaxy(pixels,size,.574f,.54f,33,15,-.4f,random);
                Galaxy(pixels,size,.19f,.77f,50,17,.55f,random);
            }
            for(int i=0;i<count;i++)
            {
                int x=random.Next(size),y=random.Next(size);
                Color color=Color.Lerp(new Color(.34f,.5f,.68f),new Color(.76f,.66f,.49f),(float)random.NextDouble());
                color*=depth==1?.35f+(float)random.NextDouble()*.45f:.55f+(float)random.NextDouble()*.35f;
                Dot(pixels,size,x,y,color);
                if(depth>1 && i%4==0)
                {
                    int radius=depth==3?3:2;
                    for(int k=1;k<=radius;k++)
                    {
                        Color ray=color*(1-k/(float)(radius+1))*.6f;
                        Dot(pixels,size,x+k,y,ray);Dot(pixels,size,x-k,y,ray);
                        Dot(pixels,size,x,y+k,ray);Dot(pixels,size,x,y-k,ray);
                    }
                    Dot(pixels,size,x,y,Color.Lerp(color,new Color(.72f,.81f,.83f),.4f));
                }
            }
            return Texture("Space / star depth "+depth,size,pixels);
        }

        static void Galaxy(Color32[] pixels,int size,float u,float v,int rx,int ry,float angle,System.Random random)
        {
            int cx=Mathf.RoundToInt(u*size),cy=Mathf.RoundToInt(v*size);
            for(int y=-rx;y<=rx;y++) for(int x=-rx;x<=rx;x++)
            {
                float xx=(x*Mathf.Cos(angle)-y*Mathf.Sin(angle))/rx;
                float yy=(x*Mathf.Sin(angle)+y*Mathf.Cos(angle))/ry;
                float r=Mathf.Sqrt(xx*xx+yy*yy);
                if(r>1)continue;
                float arm=.5f+.5f*Mathf.Cos(Mathf.Atan2(yy,xx)*2-r*12);
                float light=Mathf.Exp(-r*6)*.45f + Mathf.Pow(1-r,2)*arm*.12f;
                if(random.NextDouble()>.55 && r>.15f)light*=.35f;
                Color tint=Color.Lerp(new Color(.45f,.43f,.73f),new Color(.82f,.72f,.58f),Mathf.Exp(-r*7));
                Dot(pixels,size,cx+x,cy+y,tint*light);
            }
        }

        static void Dot(Color32[] pixels,int size,int x,int y,Color color)
        {
            x=(x%size+size)%size;y=(y%size+size)%size;
            color.a=1;pixels[y*size+x]=color;
        }
        static Texture2D Texture(string name,int size,Color32[] pixels)
        {
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false) {
                name=name,filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Repeat,anisoLevel=0
            };
            texture.SetPixels32(pixels);texture.Apply(false,true);return texture;
        }
    }
}
