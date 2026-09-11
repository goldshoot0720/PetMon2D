# 喵的夢

Unity 6 製作的 Windows 單人回合制冒險。角色、行走動畫、地形圖集、場景物件、美術背景、介面紙張、技能特效、戰鬥音效與背景音樂全部由 Meowa 生成。

## 開始遊玩

開啟 `Builds/Windows/MeowDream.exe`。分享時請複製整個 Windows 資料夾，不能只複製 exe。

- WASD／方向鍵：在夢之島上一格一格走動，鏡頭會跟著你。
- E／空白鍵：對話與挑戰。面向挑戰者、路牌、湖水、樹木或屋內家具時按下。
- 走到門口即可進出房屋；家裡的床可以睡覺回復生命，屋裡的人可以對話。
- 集滿十顆夢之星後走進夢之大廳，就會看到結局。
- 滑鼠：選角與點選技能。
- 1／2：使用角色技能；3：魚乾補血；4：防禦。
- Esc：暫停與聲音設定。

選擇喵白白或喵布布，從夢之島的村子出發，沿著步道依序挑戰十位敵人。十位挑戰者站在地圖上的固定位置，走到旁邊按 E 就能開打；只有「下一位」會接受挑戰。

走進深綠色的長草會隨機遇到已擊敗對手的「夢境回聲」，這是本作的野生遭遇：打贏累積經驗，每三次提升一級並增加最大生命；輸了則回到村子休息，不會失去進度。

每場挑戰前恢復生命、三份魚乾與三點夢能量。普通招式恢復能量；第二招消耗能量，喵白白可降低敵人攻擊、喵布布可恢復生命。防禦降低本回合傷害並恢復能量。勝利會升級並自動存檔。

存檔：`%USERPROFILE%/AppData/LocalLow/MeowDream/喵的夢/dream-save.json`。新冒險開始時會取代舊存檔。

## Unity 專案

以 Unity Hub 開啟此資料夾，使用 **6000.6.0f1**。開啟 `Assets/Scenes/Dream.unity` 並按 Play；遊戲在進入 Play 時自動建立場景內容。

批次建置：

```powershell
& 'C:/Program Files/Unity/Hub/Editor/6000.6.0f1/Editor/Unity.exe' -batchmode -nographics -quit -projectPath D:/CODE/PetMon2D -executeMethod DreamBuild.Build -logFile D:/CODE/PetMon2D/Logs/build.log
```

Windows 版使用系統微軟正黑體顯示繁體中文。原始參考圖保留在專案根目錄，實際遊戲載入 `Assets/Resources/Art` 內的 Meowa 生成素材。

夢之島是 44×34 的方格地圖，寫在 `Assets/Scripts/DreamMap.cs` 的文字網格裡（`.` 草地、`#` 步道、`~` 湖水、`*` 長草、`T` 樹、`X` 建築、`D` 門、`F` 柵欄、`R` 石頭、`S` 路牌、`,` 花叢）。地形用 Meowa 的 64px 雙格（dual-grid）圖集繪製，物件依 Y 座標排序疊圖。建置時 `DreamBuild.ValidateMap` 會走訪整張地圖，確認每格都連通、十位挑戰者都走得到。

## 素材工作流程

`Tools/generate_assets.py` 生成角色與場景插圖，`Tools/generate_world.py` 分階段生成地圖素材（`textures` 無縫材質 → `props` 場景物件 → `tilesets` 雙格地形圖集 → `interior`／`walls` 室內家具與牆面 → `anims` 角色走路動畫）。背景音樂以 `music-run` 生成，建置時會自動設為串流載入以免佔用記憶體。`Tools/import_assets.py` 匯入 Meowa 最終輸出並記錄來源與尺寸；`Tools/import_characters.py` 把多視角角色圖裁成共用基準框，並把 `anims` 階段產生的逐格走路動畫（動畫 WebP）切成每向八格的橫向連續圖。已完成的任務會自動略過，中斷後重跑不會重複扣點。

金鑰保存在不受版本控制的 `.env`，不能一併分享。注意 Meowa CLI **優先使用 `MEOWART_API_KEY2`**，只有在它不存在時才會用 `MEOWART_API_KEY`。

目前可自由走訪整座夢之島，包含村莊、步道、長草、湖畔與夢之大廳；十場挑戰、長草遭遇、圖鑑與存檔皆可使用。村子裡的兩間房屋與夢之大廳都可以進去，床鋪可以睡覺回復生命。家裡有喵媽媽、隔壁住著你沒選的那位主角，兩人的對話會隨著你找回的夢之星改變。集滿十顆後走進夢之大廳即進入結局。角色行走是 Meowa 生成的四方向各八格逐格動畫。尚未加入捕捉、隊伍交換、商店與多人功能。
