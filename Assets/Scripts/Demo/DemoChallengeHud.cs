using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FlyMeToTheMoon.Demo
{
    public sealed class DemoChallengeHud : MonoBehaviour
    {
        DemoGame game;
        RectTransform panel, board, movingTarget, mazePlayer, mazeExit;
        TextMeshProUGUI heading, prompt, score;
        readonly RectTransform[] pads = new RectTransform[7], beats = new RectTransform[12], walls = new RectTransform[6];
        UnityEngine.UI.Button finish;
        public Vector2 TargetScreenPosition => RectTransformUtility.WorldToScreenPoint(null, movingTarget.position);
        public void Build(DemoGame owner, RectTransform parent)
        {
            game = owner;
            panel = Box(parent, "Fragment challenge", Vector2.zero, parent.sizeDelta, new Color(.025f,.045f,.085f,.98f));
            panel.anchorMin = Vector2.zero; panel.anchorMax = Vector2.one; panel.offsetMin = panel.offsetMax = Vector2.zero;
            heading = Label(panel, new Vector2(0,265), new Vector2(1100,55), 34);
            prompt = Label(panel, new Vector2(0,205), new Vector2(1100,60), 22);
            board = Box(panel, "Challenge board", new Vector2(0,-10), new Vector2(720,340), new Color(.06f,.1f,.16f));
            score = Label(panel, new Vector2(0,-225), new Vector2(1100,45), 22);
            for (int i=0;i<7;i++)
            {
                pads[i] = Box(board, "Note " + (i+1), new Vector2((i-3)*90,0), new Vector2(76,90), new Color(.13f,.25f,.32f));
                Label(pads[i], Vector2.zero, new Vector2(70,80), 23).text = (i+1)+"\n"+new[]{"C","D","E","F","G","A","B"}[i];
            }
            for(int i=0;i<12;i++) beats[i] = Box(board,"Beat",Vector2.zero,new Vector2(80,12),DemoGame.Accent);
            movingTarget=Box(board,"Moving star",Vector2.zero,new Vector2(60.8f,27.6f),new Color(1,.8f,.3f));
            movingTarget.GetComponent<UnityEngine.UI.Image>().sprite=game.Hud.CircleSprite;
            mazePlayer=Box(board,"Maze note",Vector2.zero,new Vector2(13,13),new Color(1,.8f,.3f));
            mazeExit=Box(board,"Exit",new Vector2(.88f*320,.75f*145),new Vector2(26,26),DemoGame.Accent);
            for(int i=0;i<3;i++)
            {
                float gap=DemoChallenge.MazeGap(i);
                float lowerTop=gap-.22f, upperBottom=gap+.22f;
                walls[i*2]=Box(board,"Maze wall",new Vector2((i-1)*.48f*320,(-1+lowerTop)*.5f*145),new Vector2(.07f*320,(lowerTop+1)*145),new Color(.5f,.4f,.6f));
                walls[i*2+1]=Box(board,"Maze wall",new Vector2((i-1)*.48f*320,(1+upperBottom)*.5f*145),new Vector2(.07f*320,(1-upperBottom)*145),new Color(.5f,.4f,.6f));
            }
            var line=Box(board,"Timing line",new Vector2(0,-100),new Vector2(690,3),Color.white);
            line.gameObject.AddComponent<ChallengeBeatOnly>();
            finish=Button(panel,"Return to moon  [Enter]",new Vector2(0,-285),game.FinishChallenge);
            Button(panel,"Leave",new Vector2(505,295),game.LeaveChallenge,new Vector2(130,38));
            panel.gameObject.SetActive(false);
        }
        RectTransform Box(Transform parent,string name,Vector2 position,Vector2 size,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image));go.transform.SetParent(parent,false);
            var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.anchoredPosition=position;r.sizeDelta=size;
            var graphic=go.GetComponent<UnityEngine.UI.Image>();graphic.color=color;graphic.raycastTarget=false;return r;
        }
        TextMeshProUGUI Label(Transform parent,Vector2 position,Vector2 size,float fontSize)
        {
            var go=new GameObject("Label",typeof(RectTransform));go.transform.SetParent(parent,false);
            var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.anchoredPosition=position;r.sizeDelta=size;
            var t=go.AddComponent<TextMeshProUGUI>();t.font=game.Font;t.fontSize=fontSize;t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;return t;
        }
        UnityEngine.UI.Button Button(Transform parent,string caption,Vector2 position,UnityEngine.Events.UnityAction action,Vector2? size=null)
        {
            var r=Box(parent,caption,position,size??new Vector2(380,45),new Color(.12f,.3f,.35f));
            var image=r.GetComponent<UnityEngine.UI.Image>();image.raycastTarget=true;
            var b=r.gameObject.AddComponent<UnityEngine.UI.Button>();b.targetGraphic=image;b.onClick.AddListener(action);
            Label(r,Vector2.zero,r.sizeDelta,20).text=caption;return b;
        }
        void Update()
        {
            if(game==null)return;
            bool active=game.State==DemoState.Challenge;panel.gameObject.SetActive(active);if(!active)return;
            var c=game.Challenge;heading.text=DemoChallenge.Names[c.Index];
            bool echo=c.Kind==FragmentChallenge.Echo, beat=c.Kind==FragmentChallenge.Beat, target=c.Kind==FragmentChallenge.Targets, maze=c.Kind==FragmentChallenge.Maze;
            for(int i=0;i<7;i++)
            {
                pads[i].gameObject.SetActive((echo || c.Kind==FragmentChallenge.Code || (beat && i<4)) && !c.Finished);
                pads[i].anchoredPosition=beat?new Vector2((i-1.5f)*150,-135):new Vector2((i-3)*90,0);
                pads[i].sizeDelta=beat?new Vector2(90,35):new Vector2(76,90);
                pads[i].GetComponent<UnityEngine.UI.Image>().color=echo && c.PreviewNote==i?DemoGame.Accent:new Color(.13f,.25f,.32f);
                pads[i].GetComponentInChildren<TextMeshProUGUI>().text=beat?(i<4?new[]{"A","S","D","F"}[i]:""):(i+1)+"\n"+new[]{"C","D","E","F","G","A","B"}[i];
            }
            for(int i=0;i<12;i++)
            {
                float y=-100+(DemoChallenge.BeatTime(i)-c.Elapsed)*150;
                beats[i].gameObject.SetActive(beat && !c.Finished && !c.Judged[i] && y<155 && y>-125);
                beats[i].anchoredPosition=new Vector2((c.Sequence[i]-1.5f)*150,y);
            }
            board.GetComponentInChildren<ChallengeBeatOnly>(true).gameObject.SetActive(beat && !c.Finished);
            movingTarget.gameObject.SetActive(target && !c.Finished);
            movingTarget.anchoredPosition=Vector2.Scale(c.TargetPosition,new Vector2(320,145));
            mazePlayer.gameObject.SetActive(maze && !c.Finished);mazeExit.gameObject.SetActive(maze && !c.Finished);
            mazePlayer.anchoredPosition=Vector2.Scale(c.MazePosition,new Vector2(320,145));
            foreach(var wall in walls)wall.gameObject.SetActive(maze && !c.Finished);
            if(target && !c.Finished && Mouse.current!=null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(board,Mouse.current.position.ReadValue(),null,out var local);
                if(board.rect.Contains(local) && c.Shoot(new Vector2(local.x/320,local.y/145))) game.Audio.Note(0,c.Step%7);
            }
            prompt.text=c.Finished?(c.Success?"Score fragment recovered":"Try again at the beacon"):
                echo?(c.Previewing?"Listen and remember":"Repeat with 1–7"):
                beat?"A / S / D / F — hit the white line":target?"Left-click eight moving stars":maze?"WASD — reach the green exit":c.CodeClue+"   |   1–7, wrap at the ends";
            score.text=c.Finished?(c.Success?"Return to collect your page":"Your other pages are safe"):
                Mathf.CeilToInt(c.Remaining)+"s   |   "+(maze?"":c.Step+" / "+(beat?12:target?8:6)+"   |   ")+"Mistakes "+c.Strikes+" / "+(echo || c.Kind==FragmentChallenge.Code?2:3);
            finish.gameObject.SetActive(c.Finished);
        }
    }
    public sealed class ChallengeBeatOnly : MonoBehaviour { }
}
