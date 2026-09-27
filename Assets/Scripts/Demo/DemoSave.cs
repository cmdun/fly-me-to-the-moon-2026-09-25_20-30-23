using System;
using UnityEngine;
namespace FlyMeToTheMoon.Demo
{
    /// <summary>One local expedition slot. Completed pages survive restarts; unfinished encounters restart safely.</summary>
    public static class DemoSave
    {
        const string Key="FlyMeToTheMoon.Expedition.v1";
        static bool Allowed => !Application.isBatchMode && Array.IndexOf(Environment.GetCommandLineArgs(),"-demoSmoke")<0 && Array.IndexOf(Environment.GetCommandLineArgs(),"-runTests")<0;
        [Serializable] public sealed class Data
        {
            public int version=1, seed, moon, equipped;
            public float upX, upY=1;
            public bool[] fragments=new bool[5];
            public bool repaired, piano;
            public bool Valid => version==1 && fragments!=null && fragments.Length==5 && moon>=0 && moon<=24
                && !float.IsNaN(upX) && !float.IsNaN(upY) && Mathf.Abs(upX)<=1.01f && Mathf.Abs(upY)<=1.01f;
        }
        public static Data Read()
        {
            if(!Allowed)return null;
            try {var data=JsonUtility.FromJson<Data>(PlayerPrefs.GetString(Key,""));return data!=null && data.Valid?data:null;}
            catch(ArgumentException){return null;}
        }
        public static void Write(DemoGame game)
        {
            if(!Allowed || game.Melody==null || game.State==DemoState.Title)return;
            var previous=Read();
            int moon=previous!=null && previous.seed==game.Seed?previous.moon:0;
            Vector2 up=previous!=null && previous.seed==game.Seed?new Vector2(previous.upX,previous.upY):Vector2.up;
            if(game.Player.IsGrounded && game.Player.gravityManager.CurrentBody!=null)
            {moon=Array.IndexOf(game.Player.gravityManager.bodies,game.Player.gravityManager.CurrentBody);up=game.Player.Up;}
            var data=new Data{seed=game.Seed,moon=Mathf.Max(0,moon),upX=up.x,upY=up.y,
                fragments=(bool[])game.Melody.Fragments.Clone(),repaired=game.MelodyRepaired,piano=game.Melody.Unlocked,equipped=game.Equipped};
            PlayerPrefs.SetString(Key,JsonUtility.ToJson(data));PlayerPrefs.Save();
        }
        public static void Clear(){if(Allowed){PlayerPrefs.DeleteKey(Key);PlayerPrefs.Save();}}
    }
}
