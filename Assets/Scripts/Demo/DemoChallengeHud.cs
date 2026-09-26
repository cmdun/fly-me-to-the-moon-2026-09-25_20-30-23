using TMPro;
using UnityEngine;

namespace FlyMeToTheMoon.Demo
{
    public sealed class DemoChallengeHud : MonoBehaviour
    {
        DemoGame game;RectTransform root,panel,meter,goal;
        TextMeshProUGUI title,status,meterText;
        readonly TextMeshProUGUI[] labels=new TextMeshProUGUI[20];
        public void Build(DemoGame owner,RectTransform parent)
        {
            game=owner;root=parent;
            panel=Box(root,"Encounter status",new Vector2(.5f,1),new Vector2(0,-18),new Vector2(620,104),new Color(.025f,.045f,.085f,.9f));
            title=Text(panel,new Vector2(-40,29),new Vector2(490,30),21);
            status=Text(panel,new Vector2(0,1),new Vector2(590,40),16);
            var background=Box(panel,"Meter",Vector2.one*.5f,new Vector2(0,-36),new Vector2(350,7),new Color(.2f,.24f,.3f));
            meter=Box(background,"Value",new Vector2(0,.5f),Vector2.zero,new Vector2(350,7),DemoGame.Accent);
            meterText=Text(panel,new Vector2(0,-24),new Vector2(580,20),13);
            var leave=Box(panel,"Leave encounter",Vector2.one*.5f,new Vector2(249,31),new Vector2(100,28),new Color(.12f,.3f,.35f));
            var image=leave.GetComponent<UnityEngine.UI.Image>();image.raycastTarget=true;
            var button=leave.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=image;button.onClick.AddListener(game.LeaveChallenge);
            Text(leave,Vector2.zero,new Vector2(95,27),16).text="Leave";
            var pointer=Text(root,Vector2.zero,new Vector2(32,32),30);pointer.text=">";pointer.color=new Color(1,.85f,.3f);goal=pointer.rectTransform;
            for(int i=0;i<labels.Length;i++)labels[i]=Text(root,Vector2.zero,new Vector2(180,24),14);
            panel.gameObject.SetActive(false);
        }
        RectTransform Box(Transform parent,string name,Vector2 anchor,Vector2 position,Vector2 size,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image));go.transform.SetParent(parent,false);
            var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=r.pivot=anchor;r.anchoredPosition=position;r.sizeDelta=size;
            var g=go.GetComponent<UnityEngine.UI.Image>();g.color=color;g.raycastTarget=false;return r;
        }
        TextMeshProUGUI Text(Transform parent,Vector2 position,Vector2 size,float font)
        {
            var go=new GameObject("Encounter label",typeof(RectTransform));go.transform.SetParent(parent,false);
            var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.anchoredPosition=position;r.sizeDelta=size;
            var t=go.AddComponent<TextMeshProUGUI>();t.font=game.Font;t.fontSize=font;t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;return t;
        }
        void LateUpdate()
        {
            if(game==null)return;
            bool active=game.State==DemoState.Challenge && game.Challenge.Active && !game.MapVisible;
            panel.gameObject.SetActive(active);goal.gameObject.SetActive(active);
            foreach(var label in labels)label.gameObject.SetActive(false);
            if(!active)return;
            var encounter=game.Challenge.Encounter;title.text=DemoChallenge.Names[game.Challenge.Index];
            status.text=encounter.Status;
            meter.parent.gameObject.SetActive(encounter.Meter>=0);meter.sizeDelta=new Vector2(350*Mathf.Clamp01(encounter.Meter),7);
            meterText.text=encounter.Meter>=0?encounter.MeterLabel+(string.IsNullOrEmpty(game.Message)?"":"  ·  "+game.Message):game.Message;
            for(int i=0;i<Mathf.Min(labels.Length,encounter.Labels.Count);i++)
            {
                var item=encounter.Labels[i];if(item.Target==null || !item.Target.gameObject.activeInHierarchy || string.IsNullOrEmpty(item.Text))continue;
                Vector3 point=Camera.main.WorldToViewportPoint(item.Target.position);
                if(point.x<.04f || point.x>.96f || point.y<.08f || point.y>.8f)continue;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(root,Camera.main.WorldToScreenPoint(item.Target.position),null,out var local);
                local+=Vector2.up*24;
                if(local.x>root.rect.width*.5f-280 && local.y<-root.rect.height*.5f+290)continue;
                labels[i].gameObject.SetActive(true);labels[i].text=item.Text;
                labels[i].rectTransform.anchoredPosition=local;
            }
            Vector2 screen=Camera.main.WorldToScreenPoint(encounter.Goal);
            Vector2 center=Camera.main.WorldToScreenPoint(game.Player.Body.position);
            Vector2 direction=(screen-center).normalized;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root,screen,null,out var goalPoint);
            goalPoint+=Vector2.up*40;
            goalPoint.x=Mathf.Clamp(goalPoint.x,-root.rect.width*.5f+35,root.rect.width*.5f-35);
            goalPoint.y=Mathf.Clamp(goalPoint.y,-root.rect.height*.5f+45,root.rect.height*.5f-150);
            if(goalPoint.x>root.rect.width*.5f-270 && goalPoint.y<-root.rect.height*.5f+290)goalPoint.x=root.rect.width*.5f-280;
            goal.anchoredPosition=goalPoint;
            goal.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);
        }
    }
}
