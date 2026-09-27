using UnityEngine;
namespace FlyMeToTheMoon.Demo
{
    /// <summary>Small repeatable musical toys. Active encounters hide these to keep their rules intact.</summary>
    public sealed class DemoLandmarkArt : MonoBehaviour
    {
        public string Kind => kind;
        public int Activations { get; private set; }
        public bool CanInteract => game.State==DemoState.Explore && game.Player.IsGrounded && game.Player.gravityManager.CurrentBody==body;
        DemoGame game; GravityBody body; SpriteRenderer sprite, friend, response;
        string kind; float until, launchAt=-1, nextUse, melodyAt, descent; int pressedKey=-1; bool wasNear, wasGrounded;
        Vector3 scale; Quaternion rotation;
        public void Build(DemoGame owner, GravityBody moon, string theme, SpriteRenderer renderer)
        {
            game=owner;body=moon;kind=theme;sprite=renderer;
            if(kind=="drum") { sprite.sprite=PixelArtLibrary.Get("04/object/0");PixelArtLibrary.Height(sprite,.65f);transform.localPosition=Vector3.down*.6f; }
            if(kind=="garden") { sprite.sprite=PixelArtLibrary.Get("07/object/0");PixelArtLibrary.Height(sprite,.5f); }
            if(kind=="piano") PixelArtLibrary.Height(sprite,.35f);
            scale=transform.localScale;rotation=transform.localRotation;
            if(kind=="woodwinds")
            {
                friend=PixelArtLibrary.Create(transform.parent,"Little singer","06/object/0",1);
                friend.transform.localPosition=new Vector3(-1,0,-.05f);PixelArtLibrary.Height(friend,.45f);
            }
            if(kind=="harp" || kind=="crystals")
            {
                response=PixelArtLibrary.Create(transform.parent,kind=="harp"?"Hinged chime gate":"Crystal lantern",kind=="harp"?"prop/gate":"prop/crystals",1);
                response.transform.localPosition=new Vector3(1.25f,-.12f,0);PixelArtLibrary.Height(response,kind=="harp"?.75f:.6f);
            }
        }
        public void Interact()
        {
            if(!CanInteract || Vector2.Distance(game.Player.Body.position,transform.position)>1.65f || Time.time<nextUse)return;
            Activate();
        }
        void Activate()
        {
            if(Time.time<nextUse)return;
            nextUse=Time.time+(kind=="drum"?.8f:.5f);until=Time.time+1.1f;Activations++;
            game.Audio.Note(kind=="drum"?3:kind=="bell"?2:kind=="harp"?4:0,Activations%7,.26f);
            if(kind=="drum"){launchAt=Time.time+.11f;if(CanInteract && Vector2.Distance(game.Player.Body.position,transform.parent.position)<1)game.Art.PlayerArt.Crouch();}
            else game.Art.Effect(kind=="harp"?"05":"01",(Vector2)transform.position+(Vector2)transform.up*.65f,.45f,transform.rotation);
        }
        public void NotePassed(Vector2 from, Vector2 to)
        {
            if(game.State!=DemoState.Explore || Time.time<nextUse)return;
            Vector2 point=(Vector2)transform.position+(Vector2)transform.up*(kind=="garden"?.3f:.65f),line=to-from;
            float t=line.sqrMagnitude<.0001f?0:Mathf.Clamp01(Vector2.Dot(point-from,line)/line.sqrMagnitude);
            if(Vector2.Distance(point,from+line*t)<.65f)Activate();
        }
        void LateUpdate()
        {
            if(game==null || Time.timeScale==0)return;
            var player=game.Player;bool grounded=player.IsGrounded;
            bool near=player.gravityManager.CurrentBody==body && Vector2.Distance(player.Body.position,kind=="drum"?transform.parent.position:transform.position)<.95f;
            bool active=Time.time<until;
            if(kind=="drum")
            {
                if(near && grounded && !wasGrounded && descent < -1f && CanInteract)Activate();
                if(launchAt>=0 && Time.time>=launchAt)
                {
                    launchAt=-1;
                    if(near && CanInteract && player.TryDrumLaunch(player.jumpSpeed*1.35f))
                        game.Art.Effect("04",player.Body.position,.6f,transform.rotation);
                }
                sprite.sprite=PixelArtLibrary.Get("04/object/"+(launchAt>=0?1:active?3:0));
                transform.localScale=new Vector3(scale.x,scale.y*(launchAt>=0?.72f:1),scale.z);
            }
            else if(kind=="piano")
            {
                int key=near && grounded ? Mathf.Clamp(Mathf.FloorToInt(Vector2.Dot(player.Body.position-(Vector2)transform.position,transform.right)*3+3),0,6):-1;
                if(key>=0 && key!=pressedKey){game.Audio.Note(1,key,.25f);until=Time.time+.25f;Activations++;}
                pressedKey=key;sprite.sprite=PixelArtLibrary.Get("03/object/"+(key>=0?2+key%3:0));PixelArtLibrary.Height(sprite,.35f);
            }
            else if(kind=="bell")
            {
                sprite.sprite=PixelArtLibrary.Get("01/object/"+(active?Mathf.Clamp(2+(int)((1.1f-until+Time.time)*4),2,5):Activations>0?5:0));PixelArtLibrary.Height(sprite,1.7f);
            }
            else if(kind=="garden")
            {
                sprite.sprite=PixelArtLibrary.Get("07/object/"+(Activations>0?Mathf.Min(5,Activations+2):0));
                transform.localRotation=rotation*Quaternion.Euler(0,0,Mathf.Sin(Time.time*1.8f+transform.position.x)*(active?5:1.5f));
            }
            else if(kind=="woodwinds")
            {
                float x=active?.55f:-1;
                var p=friend.transform.localPosition;p.x=Mathf.MoveTowards(p.x,x,Time.deltaTime*.8f);p.y=Mathf.Abs(Mathf.Sin(Time.time*6))*.035f;friend.transform.localPosition=p;
                friend.flipX=x<p.x;friend.sprite=PixelArtLibrary.Get("06/object/"+(active?2+(int)(Time.time*6)%3:0));
                if(near && !wasNear && Time.time>melodyAt){melodyAt=Time.time+4;game.Audio.Note(0,4,.1f);}
            }
            else if(kind=="harp")response.transform.localRotation=Quaternion.Slerp(response.transform.localRotation,Quaternion.Euler(0,0,active?-78:0),Time.deltaTime*8);
            else if(kind=="crystals")
            {
                response.color=active?new Color(.6f,1,1):new Color(.55f,.65f,.85f);
                transform.localScale=scale*(1+(active?Mathf.Sin(Time.time*14)*.018f:0));
            }
            wasNear=near;wasGrounded=grounded;descent=Vector2.Dot(player.Body.linearVelocity,player.Up);
        }
    }
    public sealed class DemoHomeGarden : MonoBehaviour
    {
        DemoGame game;Vector3 size;int pages=-1;
        public void Build(DemoGame owner){game=owner;size=transform.localScale;}
        void LateUpdate()
        {
            if(game==null || game.Melody.Count==pages)return;
            int previous=pages;pages=game.Melody.Count;transform.localScale=size*(.7f+.12f*pages);
            for(int i=Mathf.Max(1,previous+1);i<=pages;i++)
            {
                var singer=PixelArtLibrary.Create(transform.parent,"Returned singer","06/object/5",2);
                singer.transform.localPosition=new Vector3(.55f+i*.34f,-.07f,-.05f);PixelArtLibrary.Height(singer,.36f);
                singer.color=Color.Lerp(Color.white,DemoWorldSurface.Palette[(i-1)%6],.25f);
            }
        }
    }
}
