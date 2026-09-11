using UnityEngine;

namespace MeowDream {
    // Areas of the dream. Terrain characters:
    //   outdoors  . grass   # path   ~ water   * tall grass   , flowers
    //             T tree    R rock   F fence   X building     D door   S sign
    //   indoors   . floor   W wall   D exit    u rug
    //             b bed     t table  h shelf
    public class Area {
        public string name, ground;
        public string[] rows;
        public Prop[] props = new Prop[0];
        public Npc[] npcs = new Npc[0];
        public Area(string n, string g, string[] r) { name = n; ground = g; rows = r; }
        public int Width => rows[0].Length;
        public int Height => rows.Length;
        public char At(int x, int y) =>
            x < 0 || y < 0 || x >= Width || y >= Height ? 'W' : rows[y][x];
    }

    // A character standing in an area. Lines are chosen by how many dream stars
    // the player has found: none yet, some, or all ten.
    public struct Npc {
        public string art, name; public int x, y; public string[] lines;
        public Npc(string a, string n, int px, int py, params string[] l) {
            art=a; name=n; x=px; y=py; lines=l;
        }
        public string Line(int cleared) =>
            lines[Mathf.Clamp(cleared == 0 ? 0 : cleared >= 10 ? 2 : 1, 0, lines.Length - 1)];
    }

    public struct Prop {
        public string art; public int x, y, w, h;
        public Prop(string a, int px, int py, int pw, int ph) { art=a; x=px; y=py; w=pw; h=ph; }
    }

    public static class DreamMap {
        public const int Tile = 64;
        // Walk sheets are one horizontal strip of this many frames (see Tools/import_characters.py).
        public const int WalkFrames = 8;
        public static readonly string[] Rows = {
        "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
        "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
        "TT........................................TT",
        "TT.FXXXX..XXXXF..................XXXXX....TT",
        "TT.FXXXX..XXXXF...T......T.......XXXXX....TT",
        "TT.FXDXX..XDXXF....T..........T..XXXXX....TT",
        "TT.F.#.....#..F.........T........XXDXX....TT",
        "TT.F.#######..F..............T............TT",
        "TT.F,,..##.,,,F..,..........,.....#######.TT",
        "TT.F,,S.##.,,,F......T............#######.TT",
        "TT.F....##....F............T...........##.TT",
        "TT.FFFFF##FFFFF..T.............T.*.*...##.TT",
        "TT......##..***...........,.....*****..##.TT",
        "TT......##******.......R........******.##.TT",
        "TT......##.*****................*****..##.TT",
        "TT......##.*.**.................******.##.TT",
        "TT......########################******.##RTT",
        "TT......########################...*...##.TT",
        "TT.......****......********...##.......##.TT",
        "TT.......****......*******....##.......##.TT",
        "TT.T......*...~~~~~~~~.**.....##.......##.TT",
        "TT...........~~~~~~~~~~...*...##.R.....##.TT",
        "TT......R....~~~~~~~~~~.*****.##.......##.TT",
        "TT...........~~~~~~~~~~******.##.......##.TT",
        "TT.......,..,~~~R~~~~~~.*****.##....,..##.TT",
        "TT....T......~~~~~~~~~~..**...##.......##.TT",
        "TT.....T.....~~~~~~~~~~.......###########.TT",
        "TT............~~~~~~~~........##########..TT",
        "TT.......T..,,,,,,,,,,,....R..............TT",
        "TT..................T....T......,.........TT",
        "TT..T.........................T....T..T...TT",
        "TT..........R.............................TT",
        "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
        "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
        };

        public static readonly Area Island = new Area("夢之島", "tex_grass", Rows) {
            props = new[] {
                new Prop("prop_house_red", 4, 3, 4, 3),
                new Prop("prop_house_blue", 10, 3, 4, 3),
                new Prop("prop_house_hall", 33, 3, 5, 4)
            }
        };

        // Each interior is entered from one door tile on the island and left through its own 'D'.
        public static readonly Area[] Interiors = {
            new Area("喵的家", "tex_floor", new[] {
                "WWWWWWWWWWWWW",
                "Wbb.......hhW",
                "Wbb.......hhW",
                "W...........W",
                "W.....u.....W",
                "W.....tt....W",
                "W...........W",
                "W...........W",
                "WWWWWWDWWWWWW"
            }) { props = new[] { new Prop("prop_bed", 1, 1, 2, 2), new Prop("prop_shelf", 10, 1, 2, 2),
                                 new Prop("prop_table", 5, 5, 2, 1) },
                 npcs = new[] { new Npc("npc_mama", "喵媽媽", 8, 3,
                     "喵媽媽：路上會遇到十位挑戰者喔。累了就回來睡一下，床永遠留著。",
                     "喵媽媽：又找回一顆夢之星了呀，媽媽有在數呢。要不要先睡一下？",
                     "喵媽媽：十顆都亮了……去夢之大廳看看吧，媽媽在家等你回來。") } },
            new Area("鄰居家", "tex_floor", new[] {
                "WWWWWWWWWWWWW",
                "Whh.......bbW",
                "Whh.......bbW",
                "W...........W",
                "W.....u.....W",
                "W....tt.....W",
                "W...........W",
                "W...........W",
                "WWWWWWDWWWWWW"
            }) { props = new[] { new Prop("prop_shelf", 1, 1, 2, 2), new Prop("prop_bed", 10, 1, 2, 2),
                                 new Prop("prop_table", 4, 5, 2, 1) },
                 npcs = new[] { new Npc("", "", 6, 3,
                     "還沒出發嗎？長草裡有以前對手的回聲，可以先去練練身手。",
                     "聽說防禦不只能減傷，還能補回夢能量，別忘了用。",
                     "十顆夢之星……那我也該做我自己的夢了。路上小心。") } },
            new Area("夢之大廳", "tex_floor", new[] {
                "WWWWWWWWWWWWWWWWW",
                "Whh...........hhW",
                "Whh...........hhW",
                "W...............W",
                "W.......u.......W",
                "W......tt.......W",
                "W...............W",
                "W...............W",
                "W...............W",
                "W...............W",
                "WWWWWWWWDWWWWWWWW"
            }) { props = new[] { new Prop("prop_shelf", 1, 1, 2, 2), new Prop("prop_shelf", 14, 1, 2, 2),
                                 new Prop("prop_table", 6, 5, 2, 1) } }
        };
        // Island door tile that opens each interior, in the same order as Interiors.
        public static readonly Vector2Int[] Doors = {
            new Vector2Int(5, 5), new Vector2Int(11, 5), new Vector2Int(35, 6)
        };

        public static Area Current = Island;
        public static int Width => Current.Width;
        public static int Height => Current.Height;
        public static readonly Vector2Int Spawn = new Vector2Int(8, 9);

        // One standing position per enemy, in challenge order. Island only.
        public static readonly Vector2Int[] Trainers = {
            new Vector2Int(10,18), new Vector2Int(14,12), new Vector2Int(18,14),
            new Vector2Int(22,18), new Vector2Int(27,14), new Vector2Int(24,26),
            new Vector2Int(32,22), new Vector2Int(36,27), new Vector2Int(37,20),
            new Vector2Int(36,10)
        };
        // Dual-grid atlas lookup: index by the 4-corner key, get the 4x4 atlas cell.
        public static readonly int[] AtlasCol = {0,3,0,3,0,1,2,3,1,0,3,2,1,2,1,2};
        public static readonly int[] AtlasRow = {3,3,0,2,2,2,3,1,3,1,0,0,0,2,1,1};

        public static char At(int x, int y) => Current.At(x, y);
        public static bool Solid(int x, int y) => "T~RFXSWbth".IndexOf(At(x, y)) >= 0;
        public static bool TallGrass(int x, int y) => At(x, y) == '*';
        public static bool Indoors => Current != Island;
        public static int DoorAt(int x, int y) {
            if (Current != Island) return -1;
            for (int i = 0; i < Doors.Length; i++)
                if (Doors[i].x == x && Doors[i].y == y) return i;
            return -1;
        }
        public static Vector2Int ExitTile(Area area) {
            for (int y = 0; y < area.Height; y++)
                for (int x = 0; x < area.Width; x++)
                    if (area.At(x, y) == 'D') return new Vector2Int(x, y);
            return new Vector2Int(1, 1);
        }
        // Corner key for the dual-grid display cell (dc, dr) of one terrain layer.
        public static int Key(char layer, int dc, int dr) {
            int key = 0;
            if (At(dc-1, dr-1) == layer) key += 1;
            if (At(dc-1, dr) == layer) key += 2;
            if (At(dc, dr-1) == layer) key += 4;
            if (At(dc, dr) == layer) key += 8;
            return key;
        }
    }
}
