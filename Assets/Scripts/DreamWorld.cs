using System.Collections.Generic;
using UnityEngine;

namespace MeowDream {
// Tile-based overworld: dual-grid terrain, depth-sorted props, grid-locked walking,
// trainers standing on the route and random encounters in the tall grass.
public partial class DreamGame {
    const int Tile = DreamMap.Tile;
    const float StepSeconds = .17f;
    static readonly Vector2Int[] Steps = {
        new Vector2Int(0,1), new Vector2Int(-1,0), new Vector2Int(1,0), new Vector2Int(0,-1)
    };
    static readonly string[] FacingArt = {"down","left","right","up"};

    Vector2Int tile = DreamMap.Spawn, fromTile = DreamMap.Spawn;
    int facing;                 // 0 down, 1 left, 2 right, 3 up
    float stepT = 1f, stride;   // stepT 0..1 across one tile; stride drives the walk frame
    Vector2Int islandTile = DreamMap.Spawn;   // where to stand again after leaving a house
    string worldMessage = "";
    float worldMessageTime;
    readonly List<Sprite2D> drawList = new List<Sprite2D>();

    struct Sprite2D {
        public float sort; public Rect rect; public string art; public int frame, frames; public bool flip;
        public Sprite2D(float s, Rect r, string a, int f = 0, int n = 1, bool m = false) {
            sort = s; rect = r; art = a; frame = f; frames = n; flip = m;
        }
    }

    Vector2 PlayerPixel() {
        Vector2 from = new Vector2(fromTile.x, fromTile.y), to = new Vector2(tile.x, tile.y);
        return Vector2.Lerp(from, to, Mathf.Clamp01(stepT)) * Tile;
    }
    Vector2 CameraOffset() {
        Vector2 p = PlayerPixel() + new Vector2(Tile / 2f, Tile / 2f);
        return new Vector2(Follow(p.x, DreamMap.Width * Tile, 1600),
                           Follow(p.y, DreamMap.Height * Tile, 900));
    }
    // Follow the player across a large area; centre an area smaller than the screen
    // (a house interior) instead of pinning it to the top-left corner.
    static float Follow(float focus, float mapSize, float screen) =>
        mapSize <= screen ? (mapSize - screen) / 2f
                          : Mathf.Clamp(focus - screen / 2f, 0, mapSize - screen);
    int TrainerAt(int x, int y) {
        if (DreamMap.Indoors) return -1;
        for (int i = 0; i < DreamMap.Trainers.Length; i++)
            if (DreamMap.Trainers[i].x == x && DreamMap.Trainers[i].y == y) return i;
        return -1;
    }
    bool CanEnter(int x, int y) => !DreamMap.Solid(x, y) && TrainerAt(x, y) < 0 && NpcAt(x, y) < 0;

    int NpcAt(int x, int y) {
        var npcs = DreamMap.Current.npcs;
        for (int i = 0; i < npcs.Length; i++)
            if (npcs[i].x == x && npcs[i].y == y) return i;
        return -1;
    }
    // An NPC with no art of its own is the hero the player did not pick.
    string NpcArt(Npc npc) =>
        string.IsNullOrEmpty(npc.art) ? DreamData.Heroes[1 - save.hero].id : npc.art;
    string NpcName(Npc npc) =>
        string.IsNullOrEmpty(npc.name) ? DreamData.Heroes[1 - save.hero].name : npc.name;

    void Say(string text) { worldMessage = text; worldMessageTime = 4.5f; }

    void UpdateWorld() {
        if (worldMessageTime > 0) worldMessageTime -= Time.deltaTime;
        if (stepT < 1f) {
            stepT += Time.deltaTime / StepSeconds;
            stride += Time.deltaTime / StepSeconds;
            if (stepT >= 1f) { stepT = 1f; fromTile = tile; ArriveOnTile(); }
            return;
        }
        if (page != Page.World) return;
        int want = -1;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) want = 0;
        else if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) want = 1;
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) want = 2;
        else if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) want = 3;
        if (want >= 0) {
            facing = want;
            Vector2Int target = tile + Steps[want];
            if (CanEnter(target.x, target.y)) { fromTile = tile; tile = target; stepT = 0f; }
            else stride = 0f;
        } else stride = 0f;
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            Interact();
    }

    void ArriveOnTile() {
        if (DreamMap.At(tile.x, tile.y) == 'D') {
            if (DreamMap.Indoors) LeaveInterior();
            else { int door = DreamMap.DoorAt(tile.x, tile.y); if (door >= 0) EnterInterior(door); }
            return;
        }
        if (DreamMap.TallGrass(tile.x, tile.y) && save.cleared > 0 && Random.value < .12f) {
            BeginBattle(Random.Range(0, save.cleared), true);
        }
    }

    void EnterInterior(int index) {
        islandTile = fromTile;                       // the doorstep we walked in from
        DreamMap.Current = DreamMap.Interiors[index];
        var exit = DreamMap.ExitTile(DreamMap.Current);
        tile = fromTile = new Vector2Int(exit.x, exit.y - 1);
        stepT = 1f; facing = 3;
        if (DreamMap.Current == DreamMap.Interiors[DreamMap.Interiors.Length - 1] && save.cleared >= 10) {
            page = Page.Ending;
            return;
        }
        Say(DreamMap.Current == DreamMap.Interiors[DreamMap.Interiors.Length - 1]
            ? "夢之大廳的中央有一圈光，十顆夢之星到齊時才會亮起。"
            : "走進" + DreamMap.Current.name + "。");
    }

    void LeaveInterior() {
        DreamMap.Current = DreamMap.Island;
        tile = fromTile = islandTile;
        stepT = 1f; facing = 0;
        worldMessageTime = 0f;
    }

    void Interact() {
        Vector2Int front = tile + Steps[facing];
        int person = NpcAt(front.x, front.y);
        if (person >= 0) {
            var npc = DreamMap.Current.npcs[person];
            string line = npc.Line(save.cleared);
            Say(string.IsNullOrEmpty(npc.name) ? NpcName(npc) + "：" + line : line);
            return;
        }
        int who = TrainerAt(front.x, front.y);
        if (who >= 0) { Challenge(who); return; }
        char c = DreamMap.At(front.x, front.y);
        if (c == 'b') {
            if (hp >= MaxHp) Say("床鋪很暖，但現在精神正好。");
            else { hp = MaxHp; Say("躺下來睡了一下，生命完全恢復了。"); }
            return;
        }
        if (c == 't') { Say("桌上的茶還溫著。"); return; }
        if (c == 'h') { Say("架上放著夢之島的舊地圖與幾本故事書。"); return; }
        if (c == 'S') { Say("路牌寫著：往南是夢之島步道，沿路會遇到十位挑戰者。"); return; }
        if (c == 'D') { Say(DreamMap.Indoors ? "往下走就能出去。" : "推開門就能進去。"); return; }
        if (c == '~') { Say("湖水映著天空，看得見自己的臉。"); return; }
        if (c == 'T') { Say("樹葉沙沙作響，好像有誰在打呼。"); return; }
        if (DreamMap.Indoors) { Say("屋子裡很安靜，只聽得見自己的腳步聲。"); return; }
        Say(save.cleared < 10
            ? "下一位挑戰者是" + DreamData.Enemies[save.cleared].name + "。"
            : "十顆夢之星都亮了，可以回去看看結局。");
    }

    void Challenge(int who) {
        if (who < save.cleared) { Say(DreamData.Enemies[who].name + "已經是你的朋友了。"); return; }
        if (who > save.cleared) {
            Say("先去挑戰" + DreamData.Enemies[save.cleared].name + "，" +
                DreamData.Enemies[who].name + "還在等更強的對手。");
            return;
        }
        BeginBattle(who, false);
    }

    // ---------- rendering ----------

    void World() {
        Vector2 cam = CameraOffset();
        DrawTerrain(cam);
        DrawObjects(cam);
        WorldHud();
    }

    Rect TileRect(int x, int y, Vector2 cam) =>
        new Rect(x * Tile - cam.x, y * Tile - cam.y, Tile, Tile);

    void DrawTerrain(Vector2 cam) {
        int x0 = Mathf.Max(0, Mathf.FloorToInt(cam.x / Tile));
        int y0 = Mathf.Max(0, Mathf.FloorToInt(cam.y / Tile));
        int x1 = Mathf.Min(DreamMap.Width - 1, Mathf.CeilToInt((cam.x + 1600) / Tile));
        int y1 = Mathf.Min(DreamMap.Height - 1, Mathf.CeilToInt((cam.y + 900) / Tile));
        bool haveGround = art.TryGetValue(DreamMap.Current.ground, out var ground) && ground != null;
        Fill(new Rect(0, 0, 1600, 900), DreamMap.Indoors ? new Color(.12f,.11f,.10f) : green);
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
                if (haveGround) GUI.DrawTexture(TileRect(x, y, cam), ground, ScaleMode.StretchToFill, false);
        Layer('W', null, "tex_wall", cam, x0, y0, x1, y1);
        Layer('#', "tile_path_on_grass", "tex_path", cam, x0, y0, x1, y1);
        Layer('~', "tile_water_in_grass", "tex_water", cam, x0, y0, x1, y1);
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
                if (DreamMap.At(x, y) == ',') Tex(TileRect(x, y, cam), "prop_flowers");
        Layer('*', "tile_tallgrass", "tex_tallgrass", cam, x0, y0, x1, y1);
    }

    // One dual-grid terrain layer. The display grid sits half a tile up-left of the
    // world grid, so each display cell blends the four world tiles around its corner.
    // Without the atlas, fall back to flat tiling of the raw material so the terrain
    // still reads correctly, just without blended edges.
    void Layer(char layer, string atlasId, string flatId, Vector2 cam, int x0, int y0, int x1, int y1) {
        Texture2D atlas = null;
        if (atlasId != null) art.TryGetValue(atlasId, out atlas);
        if (atlas == null) {
            art.TryGetValue(flatId, out var flat);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++) {
                    if (DreamMap.At(x, y) != layer) continue;
                    if (flat != null) GUI.DrawTexture(TileRect(x, y, cam), flat, ScaleMode.StretchToFill, false);
                    else Fill(TileRect(x, y, cam), FlatColor(layer));
                }
            return;
        }
        for (int dr = y0; dr <= y1 + 1; dr++) {
            for (int dc = x0; dc <= x1 + 1; dc++) {
                int key = DreamMap.Key(layer, dc, dr);
                if (key == 0) continue;
                var target = new Rect(dc * Tile - Tile / 2f - cam.x, dr * Tile - Tile / 2f - cam.y, Tile, Tile);
                var uv = new Rect(DreamMap.AtlasCol[key] * .25f, 1f - (DreamMap.AtlasRow[key] + 1) * .25f, .25f, .25f);
                GUI.DrawTextureWithTexCoords(target, atlas, uv, true);
            }
        }
    }

    void DrawObjects(Vector2 cam) {
        drawList.Clear();
        int x0 = Mathf.Max(0, Mathf.FloorToInt(cam.x / Tile) - 2);
        int y0 = Mathf.Max(0, Mathf.FloorToInt(cam.y / Tile) - 3);
        int x1 = Mathf.Min(DreamMap.Width - 1, Mathf.CeilToInt((cam.x + 1600) / Tile) + 2);
        int y1 = Mathf.Min(DreamMap.Height - 1, Mathf.CeilToInt((cam.y + 900) / Tile) + 2);
        for (int y = y0; y <= y1; y++) {
            for (int x = x0; x <= x1; x++) {
                float bottom = (y + 1) * Tile;
                switch (DreamMap.At(x, y)) {
                    case 'T': Add(bottom, x * Tile - 16, bottom - 104, 96, 104, "prop_tree", cam); break;
                    case 'R': Add(bottom, x * Tile + 4, bottom - 56, 56, 56, "prop_rock", cam); break;
                    // Fence art carries transparent side margins, so overdraw each segment
                    // past its tile to make a neighbouring run read as one continuous fence.
                    case 'F': Add(bottom, x * Tile - 14, bottom - 58, Tile + 28, 58, "prop_fence", cam); break;
                    case 'S': Add(bottom, x * Tile + 8, bottom - 72, 48, 72, "prop_sign", cam); break;
                    case 'u': Add(bottom - Tile, x * Tile, bottom - Tile, Tile, Tile, "prop_rug", cam); break;
                }
            }
        }
        foreach (var b in DreamMap.Current.props) {
            float size = b.w * Tile, bottom = (b.y + b.h) * Tile;
            Add(bottom, b.x * Tile, bottom - size, size, size, b.art, cam);
        }
        foreach (var npc in DreamMap.Current.npcs) {
            float bottom = (npc.y + 1) * Tile + 6;
            Add(bottom, npc.x * Tile - 12, bottom - 92, 88, 92, NpcArt(npc), cam);
        }
        if (DreamMap.Indoors) { AddPlayer(cam); drawList.Sort((a, b) => a.sort.CompareTo(b.sort));
            foreach (var sp in drawList) Blit(sp); return; }
        for (int i = 0; i < DreamMap.Trainers.Length; i++) {
            var t = DreamMap.Trainers[i];
            if (t.x < x0 || t.x > x1 || t.y < y0 || t.y > y1) continue;
            float bottom = (t.y + 1) * Tile + 6;
            GUI.color = i < save.cleared ? new Color(1, 1, 1, .55f) : Color.white;
            Add(bottom, t.x * Tile - 12, bottom - 92, 88, 92, DreamData.Enemies[i].id, cam);
            GUI.color = Color.white;
        }
        AddPlayer(cam);
        drawList.Sort((a, b) => a.sort.CompareTo(b.sort));
        foreach (var s in drawList) Blit(s);
    }

    void Add(float sort, float x, float y, float w, float h, string artId, Vector2 cam) {
        drawList.Add(new Sprite2D(sort, new Rect(x - cam.x, y - cam.y, w, h), artId));
    }

    void AddPlayer(Vector2 cam) {
        Vector2 p = PlayerPixel();
        float bottom = p.y + Tile + 6;
        const float height = 96f;
        string sheet = "walk_" + Hero.id + "_" + FacingArt[facing];
        art.TryGetValue(sheet, out var tex);
        // A walk sheet is one wide strip; anything near-square is a single standing pose.
        int frames = tex != null && tex.height > 0 && tex.width > tex.height * 3
            ? DreamMap.WalkFrames : 1;
        float aspect = tex != null && tex.height > 0
            ? (tex.width / (float)frames) / tex.height : .8f;
        var rect = new Rect(p.x + (Tile - height * aspect) / 2f - cam.x,
                            bottom - height - cam.y, height * aspect, height);
        if (stepT < 1f && frames == 1)
            rect.y -= Mathf.Abs(Mathf.Sin(stride * Mathf.PI)) * 6f;   // bob a static pose
        if (tex == null) { drawList.Add(new Sprite2D(bottom, rect, Hero.id)); return; }
        int frame = stepT < 1f ? Mathf.Abs(Mathf.FloorToInt(stride * frames)) % frames : 0;
        drawList.Add(new Sprite2D(bottom, rect, sheet, frame, frames));
    }

    static Color FlatColor(char layer) => layer switch {
        '#' => new Color(.87f, .80f, .62f),
        '~' => new Color(.42f, .62f, .81f),
        '*' => new Color(.20f, .45f, .27f, .85f),
        'W' => new Color(.86f, .80f, .70f),
        _ => new Color(.45f, .70f, .50f)
    };

    // Stand-in shapes so the island stays readable for any prop art not generated yet.
    static readonly Dictionary<string, Color> PropFallback = new Dictionary<string, Color> {
        {"prop_tree", new Color(.20f,.44f,.27f)}, {"prop_rock", new Color(.56f,.57f,.54f)},
        {"prop_fence", new Color(.72f,.60f,.43f)}, {"prop_sign", new Color(.60f,.44f,.28f)},
        {"prop_flowers", new Color(.93f,.71f,.78f)}, {"prop_house_red", new Color(.79f,.38f,.33f)},
        {"prop_house_blue", new Color(.45f,.56f,.75f)}, {"prop_house_hall", new Color(.36f,.62f,.60f)}
    };

    void Blit(Sprite2D s) {
        if (!art.TryGetValue(s.art, out var tex) || tex == null) {
            if (PropFallback.TryGetValue(s.art, out var c)) {
                Fill(s.rect, c);
                Fill(new Rect(s.rect.x + 4, s.rect.y + 4, s.rect.width - 8, s.rect.height * .35f),
                     new Color(1, 1, 1, .18f));
            }
            return;
        }
        if (s.frames <= 1 && !s.flip) { GUI.DrawTexture(s.rect, tex, ScaleMode.ScaleToFit, true); return; }
        float w = 1f / s.frames, u = s.frame * w;
        var uv = s.flip ? new Rect(u + w, 0, -w, 1) : new Rect(u, 0, w, 1);
        GUI.DrawTextureWithTexCoords(s.rect, tex, uv, true);
    }

    // Battles and saves always belong to the island, so keep that position authoritative.
    Vector2Int IslandPosition => DreamMap.Indoors ? islandTile : tile;

    void WorldHud() {
        Panel(new Rect(28, 22, 430, 112));
        Label(new Rect(58, 38, 380, 40), Hero.name + "  Lv." + Level, 30);
        Label(new Rect(58, 84, 380, 30), DreamMap.Current.name + "   夢之星 " + save.cleared + " / 10" +
            (save.exp > 0 ? "   回聲 " + save.exp : ""), 21);
        if (Button(new Rect(1170, 30, 180, 54), "夢境圖鑑")) page = Page.Journal;
        if (Button(new Rect(1372, 30, 180, 54), "儲存進度")) Save();
        if (worldMessageTime > 0) {
            Panel(new Rect(180, 690, 1240, 150));
            Label(new Rect(232, 726, 1140, 90), worldMessage, 27);
        } else {
            Panel(new Rect(180, 742, 1240, 96));
            string objective = DreamMap.Indoors
                ? "在" + DreamMap.Current.name + "裡休息一下"
                : save.cleared < 10
                    ? "下一位挑戰者：" + DreamData.Enemies[save.cleared].name
                    : "十顆夢之星，全都回來了！";
            Label(new Rect(232, 766, 700, 44), objective, 27);
            Label(new Rect(232, 806, 780, 30), DreamMap.Indoors
                ? "WASD／方向鍵走路 · E 查看 · 走到門口離開"
                : "WASD／方向鍵走路 · E 對話與挑戰 · 長草裡會遇到夢境回聲", 19);
            if (!DreamMap.Indoors && save.cleared >= 10 &&
                Button(new Rect(1160, 758, 220, 64), "看夢境結局", true)) page = Page.Ending;
        }
        Label(new Rect(1000, 806, 400, 30), saveMessage, 19, cream);
    }
}
}
