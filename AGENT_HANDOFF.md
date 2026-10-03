# 🤖 AGENT HANDOFF: War of Samsara (WOS) Project Context

> **สำหรับ AI Agent ที่มารับช่วงต่อ (To the Next Antigravity AI Agent):**
> เอกสารฉบับนี้สรุปบริบททั้งหมดของโปรเจกต์ ความต้องการของ User สถาปัตยกรรม และสิ่งที่ต้องทำต่อทันที เพื่อให้เริ่มงานต่อได้แบบไร้รอยต่อ 100%

---

## 📌 1. ข้อมูลสำคัญของโปรเจกต์ (Project Overview)
* **ชื่อเกม:** **War of Samsara (WOS)** — สงครามสังสารวัฏ 6 ภพภูมิ
* **แนวเกม:** 2D Side-scrolling Action MMORPG
* **DNA ของเกม (Core Inspirations):**
  1. **MapleStory:** การฟาร์มมอนสเตอร์, แมพแบ่งเป็น Channel & Portal, ฟิสิกส์ 2D Platformer (กระโดดลงพื้น, ปีนเชือก), ระบบ Paperdoll แฟชั่น, ระบบเศรษฐกิจ 5 ช่องกระเป๋า
  2. **SoulSaver (Ghost Online / โกสต์ออนไลน์):** ระบบคอมโบต่อสู้เดือดๆ, วิชาตัวเบา (Dash & Air Dash), การงัดศัตรูลอยฟ้า (Aerial Juggling), ระบบล้มแล้วลุกอมตะ (Down & Wake-up i-frame), การดูดวิญญาณ (Soul Absorbing), ลานประลอง PvP และสงครามฝ่ายธรรมะ vs อธรรม
* **เนื้อเรื่อง & โลก (Cosmology - 6 ภพภูมิ):**
  * มนุษยโลก (Human) - เมืองหลวง, ตลาดค้าขาย (Lv. 1-30)
  * เดรัจฉานภูมิ (Beast) - ป่าดึกดำบรรพ์, สัตว์ขี่ (Lv. 30-50)
  * เปรต/วิญญาณ (Ghost) - สุสาน, ดินแดนดูดวิญญาณแบบ SoulSaver (Lv. 50-70)
  * อสูรโลก (Asura) - ลานประลองสงคราม PvP & Guild War (Lv. 60+)
  * นรกภูมิ (Naraka) - ขุมนรกเพลิง, World Boss Raid (Lv. 70-90)
  * เทวโลก (Deva) - เกาะสวรรค์ลอยฟ้า, Endgame Gear (Lv. 90-100)

---

## 💻 2. ที่ตั้งไฟล์ & Repository (Locations)
* **Git Repository Root:** `D:\Unity Game Project\Eruption\WarOfSamsara`
  * Remote: `https://github.com/QNIIKE1234/WarOfSamsara.git` (Branch: `main`)
* **Unity Project Root:** `D:\Unity Game Project\Eruption\WarOfSamsara\WarOfSamsara`
  * Unity Version: **Unity 6 (6000.3+) URP 2D**

---

## 🛠️ 3. Tech Stack ที่ตกลงไว้กับ User
* **Client / Game Server:** Unity 6 (URP 2D)
* **Real-time Networking:** **Photon Fusion 2** (Dedicated Server Mode + Client-Side Prediction)
  * *หมายเหตุ:* โปรเจกต์เก่าเคยมี Quantum แต่ลบออกไปหมดแล้ว ย้ายมาขึ้นโปรเจกต์ใหม่ `WarOfSamsara` ที่คลีน 100%
* **Master Backend:** C# (.NET 8/9 Web API + gRPC)
* **Databases:**
  * **PostgreSQL:** บันทึกข้อมูลถาวร, คลังไอเทม, ระบบ P2P Trade แบบ **ACID Transaction (`SELECT ... FOR UPDATE`)** เพื่อกันบั๊กปั๊มของ 100%
  * **Redis:** Caching, Session, State ข้าม Channel
* **Shared Code:** `WarOfSamsara.Shared` (แชร์สูตรคำนวณและ DTOs ระหว่าง Unity และ .NET Backend)

---

## ✅ 4. สิ่งที่ทำเสร็จแล้วในเซสชันนี้ (Completed Work)
1. เคลียร์ปัญหาโปรเจกต์เก่า ลบ Quantum และแก้บั๊ก TextMesh Pro เรียบร้อย
2. สร้างโปรเจกต์ Unity 6 2D URP ใหม่ที่ `D:\Unity Game Project\Eruption\WarOfSamsara\WarOfSamsara`
3. วางรากฐานสคริปต์ฟิสิกส์ 2D ใน `Assets/Scripts/`:
   * `Player/PlayerMovement2D.cs`: เดิน, วิ่งมีแรงเฉื่อย, กระโดดปรับระดับ, ก้ม (Crouch), กระโดดทะลุลงพื้น (Down + Alt), ปีนบันได/เชือก (Snap เข้าแกนกลาง และโดดออก)
   * `Player/PlayerInputHandler.cs`: อินพุตปุ่มลัดสไตล์เมเปิ้ล (Alt กระโดด, Ctrl โจมตี)
   * `Environment/OneWayPlatform.cs`: แพลตฟอร์มโดดขึ้น-ลง
   * `Environment/ClimbableRope.cs`: เชือก/บันได
   * `Environment/MapPortal.cs`: วาร์ปย้ายแมพ
   * `Core/Shared/CharacterStats.cs`: สเตตัส 4 สายหลัก (STR, DEX, INT, LUK, HP, MP, Mesos) และสูตร Damage Range
   * `Core/Shared/ItemDefinition.cs`: โครงสร้างกระเป๋า 5 ช่อง (`Equip`, `Use`, `Setup`, `Etc`, `Cash`)
4. ปรับแต่ง `.gitignore` ไม่ให้ดันโฟลเดอร์ขยะ `Library/` และ `Temp/`
5. สร้าง `README.md` แม่บทเกมดีไซน์
6. **Import Photon Fusion 2 SDK (v2.1) และ Settings เข้ามาเรียบร้อยแล้ว**
7. **Commit & Push ขึ้น GitHub เรียบร้อยแล้ว (Commit `5e3b97b`)**
8. **เซ็ตอัปฉากจริงแรก `STG001_The Streets` พร้อมแก้บั๊กฟิสิกส์ 2D 100%:**
   - นำเข้า Asset แยก 7 เลเยอร์ Parallax (Sky, BG, MG, Ground, FG, Super FG)
   - ผิวถนนจริงอยู่ที่ `Y = +1.25m` (ความกว้าง 53.33m) พร้อมกำแพงกั้นซ้าย-ขวา
   - ปรับสเกลตัวละคร `FGT001_AllRounder` เป็น `0.7` สูง 2.40m ส้นเท้าตรงกับขอบล่าง Collider เป๊ะ วางจุดเกิดที่ `Y = 3.05m` ไม่ลอยไม่จม
   - แก้ไข `PlayerMovement2D.cs` ใช้ `_collider.bounds` และ `Physics2D.BoxCast(ContactFilter2D)` สไตล์ Unity 6 ป้องกันบั๊กตัด Layer 0
   - ติดตั้งระบบ **Dash / วิชาตัวเบา (Shift)** พุ่งเลียดพื้น 15m/s คูลดาวน์ 0.45s
   - วางมอนสเตอร์กระสอบทราย `Enemy_Thug (ENE002)` และ `Enemy_BadDog (ENE001)` ไว้บนถนน
   - จัดวางสถาปัตยกรรม **Single Scene + Map Prefabs** สร้างโฟลเดอร์ `Assets/Prefabs/Maps/` และ `Assets/Resources/Maps/` พร้อมระบบ Auto-Export Prefab ใน `StreetStageBuilder.cs`

---

## 🎯 5. สิ่งที่ต้องทำต่อในเซสชันนี้ (Immediate Next Steps)
1. **ระบบต่อสู้ & คอมโบหมัดมวย/ดาบสไตล์ SoulSaver (Combat System):**
   - สร้างปุ่มโจมตี (Left Ctrl / Key C) ทำ Combo Attack 3 จังหวะ
   - สร้างระบบ **Hitbox & Hurtbox 2D** (Trigger Box ตรวจจับศัตรู)
   - สร้างระบบ **Hit Reactions & CC**:
     - `Flinch` (ชะงัก)
     - `Knockback` (กระเด็น)
     - `Air Launch` (งัดลอยฟ้า สำหรับต่อ Air Combo)
     - `Down & Wake-up Invincibility` (ล้มลงพื้น + ลุกขึ้นมากระพริบอมตะ 1.5 วิ ป้องกัน Infinite Combo)
2. **ระบบดูดวิญญาณ (Soul Absorbing):**
   - มอนสเตอร์หรือผู้เล่นเวลาโดนตี/ตาย จะดรอป Soul Orb ลอยออกมา
   - กดปุ่มดูดวิญญาณ (เช่น Spacebar) ลากวิญญาณเข้าตัว เพื่อเติมเกจ Fury / Mana
3. **ตัวเลขดาเมจลอย (Damage Popup):**
   - ตัวเลขดาเมจลอยตามจังหวะคอมโบ สไตล์ MapleStory (Damage Skins + Critical Hit)
4. **ระบบ MapManager (Portal Warp):**
   - เดินเข้าวาร์ป -> Fade Black -> Unload Map เก่า -> โหลด Map Prefab ใหม่จาก `Assets/Prefabs/Maps/` -> ย้ายตำแหน่งตัวละคร -> Fade In
