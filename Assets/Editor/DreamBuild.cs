using System;
using System.IO;
using MeowDream;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class DreamBuild {
    public static void Build() {
        Directory.CreateDirectory("Assets/Scenes");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/Dream.unity");
        PlayerSettings.companyName="MeowDream";PlayerSettings.productName="喵的夢";
        PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;
        PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.runInBackground=true;
        PlayerSettings.resizableWindow=true;
        foreach(string guid in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/Resources/Art"})) {
            var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
            importer.textureType=TextureImporterType.Default;importer.alphaIsTransparency=true;
            importer.mipmapEnabled=false;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        }
        foreach(string guid in AssetDatabase.FindAssets("t:AudioClip",new[]{"Assets/Resources/Art"})) {
            string path=AssetDatabase.GUIDToAssetPath(guid);
            var importer=(AudioImporter)AssetImporter.GetAtPath(path);
            var settings=importer.defaultSampleSettings;
            // Stream the long music track; keep short effects resident for instant playback.
            bool music=path.Contains("ambience");
            settings.loadType=music?AudioClipLoadType.Streaming:AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat=music?AudioCompressionFormat.Vorbis:AudioCompressionFormat.PCM;
            settings.preloadAudioData=!music;
            importer.defaultSampleSettings=settings;
            importer.SaveAndReimport();
        }
        AssetDatabase.Refresh();
        Validate();
        var report=BuildPipeline.BuildPlayer(new[]{"Assets/Scenes/Dream.unity"},"Builds/Windows/MeowDream.exe",BuildTarget.StandaloneWindows64,BuildOptions.None);
        if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new Exception("Build failed: "+report.summary.result);
        Debug.Log("MEOW_BUILD_PASS");
    }
    public static void Validate() {
        foreach(var f in DreamData.Heroes) Check(f);
        foreach(var f in DreamData.Enemies) Check(f);
        foreach(var id in new[]{"world","battle","title","ui","spark"}) if(Resources.Load<Texture2D>("Art/"+id)==null) throw new Exception("Missing art: "+id);
        if(DreamData.Enemies.Length!=10||DreamData.Heroes.Length!=2||DreamData.Enemies[0].moves.Length!=3) throw new Exception("Roster mismatch");
        if(Resources.Load<AudioClip>("Art/hit")==null) throw new Exception("Missing sound: hit");
        if(Resources.Load<AudioClip>("Art/ambience")==null) throw new Exception("Missing music: ambience");
        ValidateMap();
        Debug.Log("MEOW_CONTENT_PASS: 12 characters, 25 moves and 5 environment/UI textures");
    }
    // The island is authored as text, so prove it is still walkable before shipping it.
    static void ValidateMap() {
        DreamMap.Current=DreamMap.Island;
        int w=DreamMap.Width, h=DreamMap.Height;
        foreach(var row in DreamMap.Rows) if(row.Length!=w) throw new Exception("Ragged map row");
        if(DreamMap.Trainers.Length!=DreamData.Enemies.Length) throw new Exception("Trainer count mismatch");
        var blocked=new bool[w,h];
        for(int y=0;y<h;y++) for(int x=0;x<w;x++) blocked[x,y]=DreamMap.Solid(x,y);
        foreach(var t in DreamMap.Trainers) blocked[t.x,t.y]=true;
        var seen=new bool[w,h];
        var queue=new System.Collections.Generic.Queue<Vector2Int>();
        if(blocked[DreamMap.Spawn.x,DreamMap.Spawn.y]) throw new Exception("Spawn is blocked");
        seen[DreamMap.Spawn.x,DreamMap.Spawn.y]=true; queue.Enqueue(DreamMap.Spawn);
        int reached=0;
        while(queue.Count>0) {
            var c=queue.Dequeue(); reached++;
            foreach(var d in new[]{Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right}) {
                int nx=c.x+d.x, ny=c.y+d.y;
                if(nx<0||ny<0||nx>=w||ny>=h||seen[nx,ny]||blocked[nx,ny]) continue;
                seen[nx,ny]=true; queue.Enqueue(new Vector2Int(nx,ny));
            }
        }
        for(int i=0;i<DreamMap.Trainers.Length;i++) {
            var t=DreamMap.Trainers[i];
            bool adjacent=false;
            foreach(var d in new[]{Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right}) {
                int nx=t.x+d.x, ny=t.y+d.y;
                if(nx>=0&&ny>=0&&nx<w&&ny<h&&seen[nx,ny]) adjacent=true;
            }
            if(!adjacent) throw new Exception("Trainer unreachable: "+DreamData.Enemies[i].name);
        }
        if(DreamMap.Interiors.Length!=DreamMap.Doors.Length) throw new Exception("Door/interior count mismatch");
        for(int i=0;i<DreamMap.Doors.Length;i++) {
            var door=DreamMap.Doors[i];
            if(DreamMap.Island.At(door.x,door.y)!='D') throw new Exception("Island door "+i+" is not a door tile");
            if(!seen[door.x,door.y]) throw new Exception("Island door "+i+" is unreachable");
            var room=DreamMap.Interiors[i];
            foreach(var row in room.rows) if(row.Length!=room.Width) throw new Exception("Ragged interior row in "+room.name);
            var exit=DreamMap.ExitTile(room);
            if(room.At(exit.x,exit.y)!='D') throw new Exception("No exit in "+room.name);
            // The player lands one tile above the exit, so that tile has to be standable.
            DreamMap.Current=room;
            if(DreamMap.Solid(exit.x,exit.y-1)) throw new Exception("Blocked landing spot in "+room.name);
            foreach(var npc in room.npcs) {
                if(DreamMap.Solid(npc.x,npc.y)) throw new Exception("NPC stands in a wall in "+room.name);
                if(npc.x==exit.x&&npc.y==exit.y-1) throw new Exception("NPC blocks the landing spot in "+room.name);
                if(npc.lines==null||npc.lines.Length==0) throw new Exception("NPC has no dialogue in "+room.name);
                if(!string.IsNullOrEmpty(npc.art)&&Resources.Load<Texture2D>("Art/"+npc.art)==null)
                    throw new Exception("Missing NPC art: "+npc.art);
            }
            DreamMap.Current=DreamMap.Island;
        }
        Debug.Log("MEOW_MAP_PASS: "+w+"x"+h+" island, "+reached+" reachable tiles, 10 trainers approachable, "
                  +DreamMap.Interiors.Length+" interiors linked");
    }
    static void Check(Fighter f) {if(Resources.Load<Texture2D>("Art/"+f.id)==null)throw new Exception("Missing art: "+f.id);if(f.moves.Length<2)throw new Exception("Missing moves: "+f.name);}
}
