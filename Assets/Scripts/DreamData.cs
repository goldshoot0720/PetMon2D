using System;
using UnityEngine;

namespace MeowDream {
    [Serializable] public class Fighter {
        public string id, name, trait;
        public string[] moves;
        public Fighter(string i, string n, string t, params string[] m) { id=i; name=n; trait=t; moves=m; }
    }
    public static class DreamData {
        public static readonly Fighter[] Heroes = {
            new Fighter("baibai","喵白白","柔軟外表，勇敢的心。\n擅長近身攻擊與降低敵人攻擊力。","喵掌","喵甩尾"),
            new Fighter("bubu","喵布布","小小肉球，大大能量。\n擅長能量攻擊與回復自身生命。","喵波","喵擊")
        };
        public static readonly Fighter[] Enemies = {
            new Fighter("fengxiong","鋒兄","夢境入口的自信挑戰者","鋒兄好帥無敵腳","鋒攻","效忠鋒兄光波"),
            new Fighter("yamei","牙妹","帶著冰旋風而來","牙壓","我愛鋒兄冰炫風"),
            new Fighter("xiaotu","小塗","笑容背後藏著俐落掌法","塗利手","塗鴨掌"),
            new Fighter("fengbang","鋒榜","不肯讓出榜首的位置","榜首射擊","榜首火焰"),
            new Fighter("xiaoying","小英","優雅卻強勁的旋風","英旋風","小英威力"),
            new Fighter("yumei","魚妹","湖畔的敏捷守護者","魚拳","魚刺"),
            new Fighter("fengshi","鋒市","通往夢之城的考驗","市長最大掌","市長無影腳"),
            new Fighter("fengzong","鋒總","無影腳的真正高手","總統最大掌","總統無影腳"),
            new Fighter("tudong","塗董","夢之城最後一道關卡","董事掌","總經禮炮"),
            new Fighter("gugu","咕咕嘎嘎","夢境盡頭的神祕朋友","咕咕","嘎嘎")
        };
    }
    [Serializable] public class DreamSave {
        public int version=2, hero, cleared, exp;
        public int x=8, y=9;   // tile coordinates on the island grid
    }
}
