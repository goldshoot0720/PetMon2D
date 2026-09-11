using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MeowDream {
public partial class DreamGame : MonoBehaviour {
    enum Page { Title, Select, World, Battle, Result, Journal, Ending }
    Page page=Page.Title;
    readonly Dictionary<string,Texture2D> art=new Dictionary<string,Texture2D>();
    Font font;
    DreamSave save=new DreamSave();
    int selected, foe, hp, enemyHp, energy=3, potions=3, weaken;
    bool busy, victory, paused, muted, testing, fastTest;
    float flash, heroBump, enemyBump;
    string message="", saveMessage="";
    bool wild;
    AudioSource audioSource;
    AudioClip hit;
    readonly Color ink=new Color(.17f,.23f,.22f), cream=new Color(.99f,.97f,.9f), green=new Color(.21f,.42f,.34f);
    string SavePath => Path.Combine(Application.persistentDataPath,"dream-save.json");
    Fighter Hero => DreamData.Heroes[save.hero];
    Fighter Enemy => DreamData.Enemies[foe];
    int Bonus => save.exp/3;                       // three dream echoes make one level
    int Level => save.cleared+1+Bonus;
    int MaxHp => 105+save.cleared*12+Bonus*6;
    int EnemyMax => 62+foe*19;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot() { if(FindFirstObjectByType<DreamGame>()==null) new GameObject("喵的夢").AddComponent<DreamGame>(); }
    void Awake() {
        Application.targetFrameRate=60;
        font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft JhengHei","Microsoft YaHei","Arial"},24);
        foreach(var f in DreamData.Heroes) LoadArt(f.id);
        foreach(var f in DreamData.Enemies) LoadArt(f.id);
        foreach(var id in new[]{"world","battle","title","ui","spark"}) LoadArt(id);
        foreach(var id in new[]{"tex_grass","tile_path_on_grass","tile_water_in_grass","tile_tallgrass",
            "prop_tree","prop_rock","prop_fence","prop_sign","prop_flowers",
            "prop_house_red","prop_house_blue","prop_house_hall",
            "tex_floor","tex_wall","prop_bed","prop_table","prop_rug","prop_shelf","npc_mama"}) LoadArt(id);
        foreach(var f in DreamData.Heroes) foreach(var view in new[]{"down","up","side"}) LoadArt("walk_"+f.id+"_"+view);
        audioSource=gameObject.AddComponent<AudioSource>();
        hit=Resources.Load<AudioClip>("Art/hit");
        var music=Resources.Load<AudioClip>("Art/ambience");
        if(music!=null) {audioSource.clip=music;audioSource.loop=true;audioSource.volume=.25f;audioSource.Play();}
        if(Camera.main==null) {var cam=new GameObject("Camera").AddComponent<Camera>();cam.tag="MainCamera";cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=green;}
        hp=MaxHp;
        if(Array.Exists(Environment.GetCommandLineArgs(),x=>x=="-dreamSmoke")) {testing=true;StartCoroutine(Smoke());}
    }
    void LoadArt(string id) {art[id]=Resources.Load<Texture2D>("Art/"+id);}
    void Update() {
        flash=Mathf.MoveTowards(flash,0,Time.deltaTime*2);
        heroBump=Mathf.MoveTowards(heroBump,0,Time.deltaTime*160);
        enemyBump=Mathf.MoveTowards(enemyBump,0,Time.deltaTime*160);
        if(Input.GetKeyDown(KeyCode.Escape)) {if(page==Page.Journal) page=Page.World;else paused=!paused;}
        if(paused) return;
        if(page==Page.World) UpdateWorld();
        if(page==Page.Battle && !busy) {
            if(Input.GetKeyDown(KeyCode.Alpha1)) Act(0);
            if(Input.GetKeyDown(KeyCode.Alpha2)) Act(1);
            if(Input.GetKeyDown(KeyCode.Alpha3)) Act(2);
            if(Input.GetKeyDown(KeyCode.Alpha4)) Act(3);
        }
    }
    void OnGUI() {
        float scale=Mathf.Min(Screen.width/1600f,Screen.height/900f);
        GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1600*scale)/2,(Screen.height-900*scale)/2),Quaternion.identity,new Vector3(scale,scale,1));
        GUI.skin.font=font;
        GUI.enabled=!paused;
        switch(page) {
            case Page.Title: Title(); break;
            case Page.Select: Select();break;
            case Page.World: World();break;
            case Page.Battle: Battle();break;
            case Page.Result: Result();break;
            case Page.Journal: Journal();break;
            case Page.Ending: Ending();break;
        }
        GUI.enabled=true;
        if(paused) {
            Fill(new Rect(0,0,1600,900),new Color(0,0,0,.65f));Panel(new Rect(530,215,540,450));
            Label(new Rect(570,255,460,65),"夢境暫停中",40,ink,TextAnchor.MiddleCenter);
            if(Button(new Rect(620,350,360,65),"繼續冒險")) paused=false;
            if(Button(new Rect(620,430,360,65),muted?"開啟聲音":"關閉聲音")) {muted=!muted;AudioListener.volume=muted?0:1;}
            if(Button(new Rect(620,510,360,65),"返回標題")) {StopAllCoroutines();busy=false;paused=false;page=Page.Title;}
        }
    }
    void Background(string id) {Fill(new Rect(0,0,1600,900),green);Tex(new Rect(0,0,1600,900),id,ScaleMode.ScaleAndCrop);}
    void Title() {
        Background("title");Fill(new Rect(0,0,1600,900),new Color(.05f,.1f,.16f,.20f));
        Label(new Rect(100,90,1400,130),"喵的夢",92,cream,TextAnchor.MiddleCenter);
        Label(new Rect(100,228,1400,55),"一場從肉球開始的夢境冒險",28,cream,TextAnchor.MiddleCenter);
        Tex(new Rect(250,340,370,390),"baibai");Tex(new Rect(980,340,370,390),"bubu");
        if(Button(new Rect(620,445,360,76),"開始新的冒險",true)) {selected=0;page=Page.Select;}
        GUI.enabled=File.Exists(SavePath)&&!paused;
        if(Button(new Rect(620,540,360,66),"繼續夢境")) LoadSave();
        GUI.enabled=!paused;
        Label(new Rect(400,670,800,50),saveMessage,22,cream,TextAnchor.MiddleCenter);
        Label(new Rect(200,825,1200,38),"WASD / 方向鍵移動     E 挑戰     滑鼠操作     Esc 暫停",22,cream,TextAnchor.MiddleCenter);
    }
    void Select() {
        Background("title");Fill(new Rect(0,0,1600,900),new Color(.04f,.13f,.14f,.55f));
        Label(new Rect(100,48,1400,75),"今天，誰走進夢裡？",49,cream,TextAnchor.MiddleCenter);
        for(int i=0;i<2;i++) {
            var r=new Rect(285+i*535,150,495,585);Panel(r);
            Tex(new Rect(r.x+65,r.y+25,365,320),DreamData.Heroes[i].id);
            Label(new Rect(r.x+40,r.y+350,415,55),DreamData.Heroes[i].name,38,ink,TextAnchor.MiddleCenter);
            Label(new Rect(r.x+42,r.y+417,411,80),DreamData.Heroes[i].trait,23,ink,TextAnchor.MiddleCenter);
            if(Button(new Rect(r.x+82,r.y+510,331,55),selected==i?"已選擇":"選擇這位夥伴",selected==i)) selected=i;
        }
        if(Button(new Rect(575,778,450,70),"和"+DreamData.Heroes[selected].name+"一起出發",true)) {save=new DreamSave{hero=selected};DreamMap.Current=DreamMap.Island;tile=fromTile=DreamMap.Spawn;islandTile=DreamMap.Spawn;stepT=1f;facing=0;hp=MaxHp;Save();page=Page.World;}
        if(Button(new Rect(50,65,150,50),"返回")) page=Page.Title;
    }
    void BeginBattle() { BeginBattle(save.cleared,false); }
    void BeginBattle(int index,bool echo) {
        foe=Mathf.Clamp(index,0,DreamData.Enemies.Length-1); wild=echo;
        hp=MaxHp;enemyHp=echo?Mathf.RoundToInt(EnemyMax*.7f):EnemyMax;
        energy=3;potions=3;weaken=0;busy=false;stepT=1f;fromTile=tile;
        message=echo
            ?"長草裡跳出"+Enemy.name+"的夢境回聲！\n打倒它可以變得更強。"
            :Enemy.name+"出現了！\n"+Enemy.trait+"。";
        page=Page.Battle;
    }
    void Battle() {
        Background("battle");
        Panel(new Rect(35,20,390,70));Label(new Rect(65,35,340,45),"夢境挑戰  "+(foe+1)+" / 10",28,ink);
        Panel(new Rect(955,88,570,155));Label(new Rect(998,110,490,45),Enemy.name+"  Lv."+(foe+2),33);
        Health(new Rect(998,173,475,22),enemyHp,EnemyMax);
        Tex(new Rect(940-enemyBump,242,380,375),Enemy.id);
        Tex(new Rect(275+heroBump,332,400,380),Hero.id);
        Panel(new Rect(62,185,505,149));Label(new Rect(100,202,420,44),Hero.name+"  Lv."+Level,32);
        Health(new Rect(100,258,420,20),hp,MaxHp);
        Label(new Rect(100,288,420,30),"夢能量  "+energy+" / 3",19);
        if(flash>0) {GUI.color=new Color(1,1,1,flash);Tex(new Rect(enemyBump>0?960:315,310,310,310),"spark");GUI.color=Color.white;}
        Panel(new Rect(32,697,1536,175));
        Label(new Rect(68,726,630,114),message,25);
        bool available=!busy&&!paused;GUI.enabled=available;
        if(Button(new Rect(755,718,363,65),"1  "+Hero.moves[0]+"  · 蓄能",true)) Act(0);
        GUI.enabled=available&&energy>0;
        if(Button(new Rect(1140,718,370,65),"2  "+Hero.moves[1]+"  · 能量 1",true)) Act(1);
        GUI.enabled=available&&potions>0&&hp<MaxHp;
        if(Button(new Rect(755,797,363,54),"3  夢之魚乾  ×"+potions)) Act(2);
        GUI.enabled=available;
        if(Button(new Rect(1140,797,370,54),"4  防禦 · 恢復能量")) Act(3);
        GUI.enabled=!paused;
    }
    void Act(int action) {if(busy || page!=Page.Battle || (action==1&&energy==0) || (action==2&&(potions==0||hp>=MaxHp))) return;StartCoroutine(Turn(action));}
    IEnumerator Delay(float duration) {if(fastTest) duration=.01f;while(duration>0) {if(!paused) duration-=Time.deltaTime;yield return null;}}
    IEnumerator Turn(int action) {
        busy=true;bool guard=action==3;
        if(action<2) {
            int damage=(action==0?22:30)+save.cleared*4+UnityEngine.Random.Range(0,5);
            if(action==0) energy=Mathf.Min(3,energy+1);else {energy--;if(save.hero==0) weaken=2;else hp=Mathf.Min(MaxHp,hp+15+Level*2);}
            enemyHp=Mathf.Max(0,enemyHp-damage);enemyBump=35;flash=1;PlayHit();
            message=Hero.name+"使出「"+Hero.moves[action]+"」！\n造成 "+damage+" 傷害"+(action==1?(save.hero==0?"，敵方攻擊降低。":"，恢復生命。"):"，夢能量 +1。");
        } else if(action==2) {potions--;hp=Mathf.Min(MaxHp,hp+MaxHp/2);message="吃下夢之魚乾，恢復一半最大生命！";}
        else {energy=Mathf.Min(3,energy+1);message="縮起肉球防禦！本回合傷害減半，夢能量 +1。";}
        yield return Delay(1.2f);
        if(enemyHp<=0) {Finish(true);yield break;}
        int move=UnityEngine.Random.Range(0,Enemy.moves.Length);
        int damageTaken=12+foe*3+move*3+UnityEngine.Random.Range(0,4);
        if(weaken>0) {damageTaken=Mathf.RoundToInt(damageTaken*.7f);weaken--;}
        if(guard) damageTaken=(damageTaken+1)/2;
        hp=Mathf.Max(0,hp-damageTaken);heroBump=30;flash=1;PlayHit();
        message=Enemy.name+"使出「"+Enemy.moves[move]+"」！\n"+Hero.name+"受到 "+damageTaken+" 傷害。";
        yield return Delay(1.1f);
        if(hp<=0) {Finish(false);yield break;}
        busy=false;message+="\n輪到你了。";
    }
    void PlayHit() {if(hit!=null) audioSource.PlayOneShot(hit,.45f);}
    void Finish(bool won) {
        victory=won;busy=false;
        if(won) {if(wild) save.exp++;else save.cleared=Mathf.Max(save.cleared,foe+1);Save();}
        else if(wild) {DreamMap.Current=DreamMap.Island;tile=fromTile=DreamMap.Spawn;save.x=tile.x;save.y=tile.y;}
        page=Page.Result;
    }
    void Result() {
        Background("battle");Fill(new Rect(0,0,1600,900),new Color(.1f,.2f,.16f,.4f));
        Panel(new Rect(420,92,760,730));Tex(new Rect(650,132,300,285),Hero.id);
        Label(new Rect(460,422,680,65),victory?(wild?"夢境回聲散開了":"找回一顆夢之星！"):"休息一下，再做一場夢",40,ink,TextAnchor.MiddleCenter);
        Label(new Rect(480,515,640,100),victory
            ?(wild?"擊散了"+Enemy.name+"的回聲。\n"+Hero.name+"的經驗增加了（"+save.exp+"）。"
                  :"戰勝了"+Enemy.name+"！\n"+Hero.name+"升至 Lv."+Level+"，最大生命增加。")
            :(wild?"回到夢之島的村子休息。\n生命與夢之魚乾都補滿了。"
                  :"夢之魚乾與生命會在下次挑戰補滿。\n善用防禦與技能，夢境仍在等你。"),25,ink,TextAnchor.MiddleCenter);
        if(Button(new Rect(590,659,420,76),victory||wild?"回到夢之島":"再次挑戰",true)) {
            if(!victory&&!wild) BeginBattle(foe,false);
            else page=victory&&!wild&&save.cleared==10?Page.Ending:Page.World;
        }
        if(!victory && !wild && Button(new Rect(590,749,420,45),"返回地圖")) page=Page.World;
    }
    void Journal() {
        Background("title");Fill(new Rect(0,0,1600,900),new Color(.02f,.1f,.1f,.6f));
        Label(new Rect(70,30,1100,80),"夢境圖鑑",44,cream);
        if(Button(new Rect(1330,42,200,60),"返回地圖")) page=Page.World;
        for(int i=0;i<10;i++) {
            var r=new Rect(50+(i%5)*303,140+(i/5)*355,288,332);Panel(r);
            Tex(new Rect(r.x+69,r.y+12,150,151),DreamData.Enemies[i].id);
            Label(new Rect(r.x+18,r.y+166,252,41),DreamData.Enemies[i].name,28,ink,TextAnchor.MiddleCenter);
            Label(new Rect(r.x+23,r.y+213,242,76),string.Join("\n",DreamData.Enemies[i].moves),18,ink,TextAnchor.MiddleCenter);
            Label(new Rect(r.x+20,r.y+291,248,29),i<save.cleared?"已找回夢之星":"尚待挑戰",18,green,TextAnchor.MiddleCenter);
        }
    }
    void Ending() {
        Background("title");Fill(new Rect(0,0,1600,900),new Color(.04f,.12f,.17f,.4f));
        Label(new Rect(100,105,1400,100),"夢醒之後，也要勇敢",62,cream,TextAnchor.MiddleCenter);
        Label(new Rect(230,238,1140,100),"十顆夢之星點亮了夢之島。\n"+Hero.name+"交到了十位新朋友，帶著勇氣踏上回家的路。",28,cream,TextAnchor.MiddleCenter);
        for(int i=0;i<10;i++) Tex(new Rect(110+i*140,410+(i%2)*30,135,180),DreamData.Enemies[i].id);
        Tex(new Rect(650,570,300,210),Hero.id);
        if(Button(new Rect(375,795,400,63),"回到夢之島",true)) page=Page.World;
        if(Button(new Rect(825,795,400,63),"返回標題")) page=Page.Title;
    }
    void Save() {if(testing)return;try {save.x=IslandPosition.x;save.y=IslandPosition.y;File.WriteAllText(SavePath,JsonUtility.ToJson(save));saveMessage="夢境已儲存。";}catch(Exception) {saveMessage="儲存失敗，請確認磁碟可寫入。";}}
    void LoadSave() {
        try {
            var data=JsonUtility.FromJson<DreamSave>(File.ReadAllText(SavePath));
            if(data==null||data.hero<0||data.hero>1||data.cleared<0||data.cleared>10||data.exp<0) throw new Exception();
            save=data;
            // Version 1 stored pixel coordinates on the old painted map; restart such a
            // save from the village instead of translating a position that no longer exists.
            DreamMap.Current=DreamMap.Island;
            if(save.version!=2||!CanEnter(save.x,save.y)) {save.version=2;save.x=DreamMap.Spawn.x;save.y=DreamMap.Spawn.y;}
            DreamMap.Current=DreamMap.Island;
            tile=fromTile=new Vector2Int(save.x,save.y);stepT=1f;facing=0;
            hp=MaxHp;page=Page.World;saveMessage="夢境已讀取。";
        } catch(Exception) {saveMessage="存檔無法讀取，請開始新的冒險。";}
    }
    void Fill(Rect r,Color c) {GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=Color.white;}
    void Tex(Rect r,string id,ScaleMode mode=ScaleMode.ScaleToFit) {if(art.TryGetValue(id,out var t)&&t!=null) GUI.DrawTexture(r,t,mode,true);}
    void Panel(Rect r) {if(art.TryGetValue("ui",out var t)&&t!=null) GUI.DrawTexture(r,t,ScaleMode.StretchToFill,true);else Fill(r,cream);}
    void Label(Rect r,string text,int size,Color? color=null,TextAnchor alignment=TextAnchor.UpperLeft) {var s=new GUIStyle(GUI.skin.label){font=font,fontSize=size,alignment=alignment,wordWrap=true};s.normal.textColor=color??ink;GUI.Label(r,text,s);}
    bool Button(Rect r,string text,bool primary=false) {
        bool hover=r.Contains(Event.current.mousePosition);Color c=primary?green:cream;
        if(hover&&GUI.enabled) c=primary?new Color(.29f,.53f,.42f):new Color(.92f,.89f,.76f);
        if(!GUI.enabled)c=new Color(.66f,.69f,.64f);
        Fill(r,c);var s=new GUIStyle(GUI.skin.button){font=font,fontSize=24,alignment=TextAnchor.MiddleCenter};
        s.normal.background=null;s.hover.background=null;s.active.background=null;s.focused.background=null;
        s.normal.textColor=primary?cream:ink;s.hover.textColor=s.normal.textColor;s.active.textColor=s.normal.textColor;
        return GUI.Button(r,text,s);
    }
    void Health(Rect r,int current,int max) {Fill(r,new Color(.76f,.79f,.71f));Fill(new Rect(r.x,r.y,r.width*Mathf.Clamp01((float)current/max),r.height),current>max/3?green:new Color(.74f,.29f,.22f));Label(new Rect(r.x,r.y-1,r.width,r.height+4),current+" / "+max,16,Color.white,TextAnchor.MiddleCenter);}
    IEnumerator Smoke() {
        // Explicit test mode only. Never writes the player's save.
        string folder=Path.Combine(Application.dataPath,"..","Smoke");Directory.CreateDirectory(folder);
        yield return new WaitForSeconds(2);ScreenCapture.CaptureScreenshot(Path.Combine(folder,"title.png"));
        yield return new WaitForSeconds(1);page=Page.Select;
        yield return new WaitForSeconds(1);ScreenCapture.CaptureScreenshot(Path.Combine(folder,"select.png"));
        yield return new WaitForSeconds(1);page=Page.World;DreamMap.Current=DreamMap.Island;tile=fromTile=DreamMap.Spawn;stepT=1f;facing=0;
        yield return new WaitForSeconds(1);ScreenCapture.CaptureScreenshot(Path.Combine(folder,"world.png"));
        yield return new WaitForSeconds(1);
        // Walk south out of the village, then verify collision and the tall-grass route.
        int walked=0;
        for(int i=0;i<7;i++) {
            var next=tile+Steps[0];
            if(!CanEnter(next.x,next.y)) break;
            fromTile=tile;tile=next;stepT=0f;walked++;
            while(stepT<1f) yield return null;
        }
        // Prove the shipped player really loaded its audio, not just the editor.
        bool audioOk=hit!=null && audioSource.clip!=null && audioSource.loop;
        bool worldOk=audioOk && walked>=5 && tile.y>DreamMap.Spawn.y && !CanEnter(3,9)
            && !CanEnter(DreamMap.Trainers[0].x,DreamMap.Trainers[0].y) && DreamMap.TallGrass(11,13);
        ScreenCapture.CaptureScreenshot(Path.Combine(folder,"walk.png"));
        yield return new WaitForSeconds(1);
        // Step onto the home doorstep and prove the interior loads, heals and lets us out.
        tile=fromTile=DreamMap.Doors[0];ArriveOnTile();
        yield return new WaitForSeconds(1);
        bool inside=DreamMap.Indoors;
        hp=1;facing=3;tile=fromTile=new Vector2Int(1,3);Interact();   // face the bed and sleep
        bool healed=hp==MaxHp;
        facing=2;tile=fromTile=new Vector2Int(7,3);Interact();        // face 喵媽媽 and talk
        bool talked=worldMessage.Contains("喵媽媽");
        ScreenCapture.CaptureScreenshot(Path.Combine(folder,"house.png"));
        yield return new WaitForSeconds(1);
        var back=DreamMap.ExitTile(DreamMap.Current);
        tile=fromTile=back;ArriveOnTile();
        bool outside=!DreamMap.Indoors && tile==DreamMap.Doors[0];
        // The dream hall only opens its ending once every star is found.
        int keep=save.cleared; save.cleared=10;
        tile=fromTile=DreamMap.Doors[DreamMap.Doors.Length-1];ArriveOnTile();
        bool hallEnding=page==Page.Ending;
        save.cleared=keep;page=Page.World;
        DreamMap.Current=DreamMap.Island;tile=fromTile=DreamMap.Spawn;stepT=1f;
        worldOk=worldOk && inside && healed && talked && outside && hallEnding;
        yield return new WaitForSeconds(1);
        yield return new WaitForSeconds(1);BeginBattle();
        yield return new WaitForSeconds(1);ScreenCapture.CaptureScreenshot(Path.Combine(folder,"battle.png"));
        yield return new WaitForSeconds(1);Act(1);
        yield return new WaitForSeconds(3);bool ok=worldOk&&enemyHp<EnemyMax&&hp<MaxHp&&!busy&&energy==2;
        page=Page.Journal;yield return new WaitForSeconds(1);ScreenCapture.CaptureScreenshot(Path.Combine(folder,"journal.png"));
        yield return new WaitForSeconds(1);
        fastTest=true;UnityEngine.Random.InitState(42);int wins=0;
        for(int h=0;h<2;h++) {
            save=new DreamSave{hero=h};
            for(int battle=0;battle<10;battle++) {
                BeginBattle();int turns=0;
                while(page==Page.Battle&&turns++<60) {
                    int action=(hp<MaxHp*.48f&&potions>0)?2:(energy>0?1:0);
                    yield return Turn(action);
                }
                if(!victory||page!=Page.Result) {ok=false;break;}wins++;
            }
        }
        ok=ok&&wins==20;
        page=Page.Ending;yield return new WaitForSeconds(1);ScreenCapture.CaptureScreenshot(Path.Combine(folder,"ending.png"));
        yield return new WaitForSeconds(1);File.WriteAllText(Path.Combine(folder,"result.txt"),ok?"PASS: eight rendered screens; island walking, collision, tall grass, house interior, bed healing, NPC dialogue, the dream hall ending and loaded music; skill, energy, enemy response and turn unlock; both heroes complete all ten battles (20 wins). No player save modified.":"FAIL: worldOk="+worldOk+" audio="+audioOk+" campaign wins="+wins);
        Application.Quit(ok?0:2);
    }
}
}
