using System.Collections.Generic;
using UnityEngine;

namespace FlyMeToTheMoon.Demo
{
    /// <summary>Small authored pixel motifs, shared across the entire world at 40 pixels per unit.</summary>
    public static class DemoAmbientSprites
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        static readonly Color32 Ink = new Color32(20, 30, 49, 255);
        static readonly Color32 Leaf = new Color32(75, 153, 100, 255);
        static readonly Color32 Stem = new Color32(40, 100, 82, 255);
        static readonly Color32 Cream = new Color32(242, 229, 181, 255);
        static readonly Color32 Brass = new Color32(181, 125, 69, 255);
        static readonly Color32[] accents = {
            new Color32(89,222,197,255), new Color32(116,173,243,255), new Color32(164,216,105,255),
            new Color32(242,151,101,255), new Color32(232,143,193,255), new Color32(178,151,242,255)
        };
        public static Color Accent(int theme) => accents[theme % 6];
        public static Sprite Get(string kind, int theme, int frame = 0)
        {
            string key = kind + "/" + theme + "/" + frame;
            if (cache.TryGetValue(key, out var cached) && cached != null) return cached;
            var p = new Pixels(kind == "shrine" ? 40 : 32, kind == "shrine" ? 56 : 40);
            Color32 color = accents[theme % 6];
            Color32 light = Color.Lerp(color, Cream, .55f);
            if (kind == "plant") Plant(p, theme, frame, color, light);
            else if (kind == "shrine") Shrine(p, theme, color, light);
            else if (kind == "creature") Creature(p, theme, frame, color, light);
            else Note(p, 11, 8, color, theme % 2 == 0);
            p.Outline();
            var texture = new Texture2D(p.Width, p.Height, TextureFormat.RGBA32, false) {
                name = "Musical scenery " + key, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels32(p.Data); texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0,0,p.Width,p.Height), new Vector2(.5f,0),40,0,SpriteMeshType.FullRect);
            sprite.name = "Ambient/" + key; cache[key] = sprite; return sprite;
        }
        static void Plant(Pixels p, int theme, int variant, Color32 color, Color32 light)
        {
            int h = 20 + variant * 4;
            p.Line(16,2,16,h,Stem,2);
            p.Line(16,7,8,13,Leaf,2); p.Line(16,12,23,19,Leaf,2);
            p.Rect(7,12,5,3,Leaf); p.Rect(22,18,5,3,Leaf);
            p.Rect(15,2,2,h-3,Leaf);
            switch(theme % 6)
            {
                case 0: // Drooping bell blossoms.
                    p.Line(16,h-1,10,h+5,Leaf,2);p.Line(10,h+5,6,h+1,Leaf,2);
                    p.Rect(3,h-5,7,5,color);p.Rect(2,h-6,9,2,light);p.Rect(6,h-8,2,2,Brass);
                    p.Rect(20,h-1,7,5,color);p.Rect(19,h-2,9,2,light);p.Rect(23,h-4,2,2,Brass);
                    p.Line(16,h,23,h+6,Leaf,2);p.Line(23,h+6,24,h+4,Leaf,2);break;
                case 1: // Alternating ivory and dark leaves suggest a keyboard.
                    for(int i=0;i<4;i++) { int y=8+i*5;int x=i%2==0?5:18;
                        p.Line(16,y-1,x+4,y+3,Stem,2);p.Rect(x,y,9,4,Cream);p.Rect(x+3,y+2,2,2,color);p.Rect(x+6,y+2,2,3,Ink); }
                    p.Ellipse(16,h+5,3,4,light);break;
                case 2: // Hollow panpipe reeds with finger holes.
                    for(int i=0;i<3;i++) { int x=7+i*8,top=h+5-(i-1)*(i-1)*6;
                        p.Rect(x,3,4,top,Brass);p.Rect(x,3,1,top,light);p.Rect(x+1,top+1,2,2,Ink);
                        for(int y=8;y<top-1;y+=5)p.Rect(x+2,y,1,2,Ink); }break;
                case 3: // Drumcap mushrooms: pale membrane, banded orange body.
                    p.Rect(13,2,5,14,Brass);p.Ellipse(16,18,11,6,color);p.Rect(6,16,20,5,color);
                    p.Ellipse(16,22,10,3,Cream);p.Line(7,22,25,22,light,1);
                    for(int x=8;x<26;x+=5)p.Rect(x,15,2,5,Brass);
                    p.Ellipse(5,6,4,3,color);break;
                case 4: // Lyre vine: open middle with three visible strings.
                    p.Line(7,9,7,h+4,Leaf,2);p.Line(25,9,25,h+4,Leaf,2);
                    p.Line(7,9,16,5,Leaf,2);p.Line(16,5,25,9,Leaf,2);p.Line(7,h+4,25,h+4,color,2);
                    for(int x=11;x<24;x+=5)p.Line(x,10,x,h+2,light,1);
                    p.Ellipse(7,h+6,3,3,color);p.Ellipse(25,h+6,3,3,color);break;
                default: // Crystal seed spikes, grounded by leaves.
                    for(int i=0;i<3;i++) {int x=6+i*9,top=h+8-(i-1)*(i-1)*9;
                        p.Line(x,6,x,top,color,5);p.Line(x-1,9,x-1,top-1,light,1);
                        p.Line(x,top,x,top+3,light,1); }
                    p.Line(16,3,5,8,Leaf,3);p.Line(16,3,26,8,Leaf,3);break;
            }
            p.Rect(10,1,14,2,Stem);
        }
        static void Shrine(Pixels p, int theme, Color32 color, Color32 light)
        {
            p.Rect(5,1,30,4,Brass);p.Rect(8,5,24,2,Cream);
            if(theme==0) // Wind chime rack.
            {
                p.Line(7,6,7,43,Stem,3);p.Line(32,6,32,43,Stem,3);p.Line(6,44,33,44,Brass,3);
                for(int i=0;i<4;i++){int x=11+i*6,y=20+(i%2)*5;p.Line(x,y+5,x,42,Brass,1);p.Rect(x-2,y,5,6,color);p.Rect(x-3,y-1,7,2,light);p.Rect(x,y-3,1,2,Cream);}
                Note(p,15,35,color,false);
            }
            else if(theme==1) // Fan of piano pipes.
            {
                for(int i=0;i<5;i++){int x=7+i*6,h=20+(2-Mathf.Abs(i-2))*7;
                    p.Rect(x,7,4,h,Cream);p.Rect(x+1,10,2,6,Ink);p.Rect(x,h+7,4,3,color);}
                p.Rect(6,8,29,3,Brass);
            }
            else if(theme==2) // Breath organ, surrounded by foliage.
            {
                for(int i=0;i<4;i++){int x=8+i*7,h=21+i*5;p.Rect(x,7,5,h,Brass);p.Rect(x+1,8,1,h,light);p.Rect(x+1,h+5,3,2,Ink);
                    for(int y=14;y<h;y+=7)p.Rect(x+3,y,2,2,Ink);}
                p.Line(18,8,3,20,Leaf,3);p.Line(22,11,36,27,Leaf,3);
            }
            else if(theme==3) // Little percussion collection.
            {
                p.Rect(7,7,12,16,color);p.Rect(21,7,12,24,color);
                p.Ellipse(13,24,7,3,Cream);p.Ellipse(27,32,7,3,Cream);
                for(int x=9;x<33;x+=6)p.Line(x,9,x+3,x<20?20:28,Brass,1);
                p.Line(10,31,28,45,Brass,2);p.Line(13,43,30,35,Brass,2);p.Ellipse(11,44,3,2,light);
            }
            else if(theme==4) // Curved harp with separate strings.
            {
                p.Line(8,8,8,41,Brass,4);p.Line(8,41,17,46,Brass,3);p.Line(17,46,31,38,Brass,3);p.Line(31,38,25,9,Brass,3);
                for(int i=0;i<4;i++)p.Line(13+i*4,11,13+i*4,43-i*2,light,1);
                p.Ellipse(8,42,3,3,color);p.Ellipse(29,37,3,3,color);
            }
            else // Crystal music box.
            {
                for(int i=0;i<3;i++){int x=10+i*10,h=i==1?36:24;p.Line(x,9,x,h,color,6);p.Line(x-2,10,x-2,h,light,2);p.Line(x,h,x,h+5,light,2);}
                p.Rect(9,7,23,5,Stem);Note(p,14,39,light,true);
            }
        }
        static void Creature(Pixels p, int theme, int frame, Color32 color, Color32 light)
        {
            int lift=frame==2?3:0;
            if(theme%3==0) // Round little songbird, quaver tail.
            {
                p.Line(8,5+lift,6,20+lift,color,2);p.Ellipse(6,9+lift,4,3,color);
                p.Ellipse(17,11+lift,8,7,color);p.Ellipse(21,18+lift,5,5,light);
                p.Rect(25,16+lift,4,3,Brass);p.Line(18,6+lift,17,3,Brass,2);p.Line(23,7+lift,23,3,Brass,2);
                p.Ellipse(14,12+lift,4,3,light);p.Rect(22,19+lift,2,frame==1?1:3,Ink);
                if(frame==3)p.Rect(26,16+lift,2,1,Ink);
            }
            else if(theme%3==1) // Chime beetle with antennae and tapping feet.
            {
                p.Ellipse(16,11+lift,10,7,color);p.Line(16,6+lift,16,17+lift,Brass,1);
                p.Ellipse(22,16+lift,5,4,light);p.Line(22,20+lift,23,26+lift,Stem,1);p.Ellipse(23,27+lift,2,2,color);
                p.Rect(23,17+lift,2,frame==1?1:2,Ink);
                for(int x=9;x<27;x+=5)p.Line(x,7+lift,x-2,3+(frame==2?2:0),Brass,2);
                p.Rect(9,12+lift,3,2,Cream);p.Rect(18,9+lift,2,2,Cream);
            }
            else // Leaf-eared seedling friend.
            {
                p.Ellipse(16,12+lift,8,9,color);p.Line(12,20+lift,9,29+lift,Leaf,3);p.Line(20,20+lift,24,27+lift,Leaf,3);
                p.Ellipse(9,29+lift,3,4,light);p.Ellipse(24,28+lift,3,3,light);
                p.Rect(11,15+lift,2,frame==1?1:3,Ink);p.Rect(20,15+lift,2,frame==1?1:3,Ink);
                p.Rect(15,10+lift,3,frame==3?3:1,Ink);p.Rect(9,2,5,3,Stem);p.Rect(19,2,5,3,Stem);
            }
        }
        static void Note(Pixels p,int x,int y,Color32 color,bool pair)
        {
            p.Ellipse(x,y,3,2,color);p.Rect(x+2,y,2,12,color);
            if(pair){p.Ellipse(x+10,y+2,3,2,color);p.Rect(x+12,y+2,2,12,color);p.Line(x+3,y+12,x+13,y+14,color,2);}
            else p.Line(x+3,y+11,x+8,y+8,color,2);
        }
        sealed class Pixels
        {
            public readonly int Width,Height;public Color32[] Data;
            public Pixels(int w,int h){Width=w;Height=h;Data=new Color32[w*h];}
            void Set(int x,int y,Color32 c){if(x>=0 && x<Width && y>=0 && y<Height)Data[y*Width+x]=c;}
            public void Rect(int x,int y,int w,int h,Color32 c){for(int a=x;a<x+w;a++)for(int b=y;b<y+h;b++)Set(a,b,c);}
            public void Ellipse(int x,int y,int rx,int ry,Color32 c){for(int a=-rx;a<=rx;a++)for(int b=-ry;b<=ry;b++)if(a*a/(float)(rx*rx)+b*b/(float)(ry*ry)<=1)Set(x+a,y+b,c);}
            public void Line(int x,int y,int xx,int yy,Color32 c,int thickness){int count=Mathf.Max(Mathf.Abs(xx-x),Mathf.Abs(yy-y));for(int i=0;i<=count;i++){float t=count==0?0:i/(float)count;Rect(Mathf.RoundToInt(Mathf.Lerp(x,xx,t)),Mathf.RoundToInt(Mathf.Lerp(y,yy,t)),thickness,thickness,c);}}
            public void Outline(){var copy=(Color32[])Data.Clone();for(int y=0;y<Height;y++)for(int x=0;x<Width;x++){if(copy[y*Width+x].a!=0)continue;for(int yy=Mathf.Max(0,y-1);yy<=Mathf.Min(Height-1,y+1);yy++)for(int xx=Mathf.Max(0,x-1);xx<=Mathf.Min(Width-1,x+1);xx++)if(copy[yy*Width+xx].a!=0)Data[y*Width+x]=Ink;}}
        }
    }
}
