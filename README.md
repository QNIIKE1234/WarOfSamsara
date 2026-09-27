# ⚔️ War of Samsara (WOS)
> **สงครามสังสารวัฏ 6 ภพภูมิ** — 2D Side-scrolling Action MMORPG
> *MapleStory Progression & Nostalgia × SoulSaver Combat & Aerial PvP*

---

## 🌌 1. โลกแห่งสังสารวัฏ (The Six Realms)

โลกของ **War of Samsara** ถูกแบ่งออกเป็น 6 ภพภูมิตามวัฏสงสาร แต่ละภพภูมิมีเอกลักษณ์ วัฒนธรรม มอนสเตอร์ และระบบการเล่นเฉพาะตัว:

| ภพภูมิ | ดินแดน | ธีมและจุดเด่น | สไตล์การเล่น / มอนสเตอร์ |
| :--- | :--- | :--- | :--- |
| **มนุษยโลก** *(Human)* | เมืองหลวง & ท่าเรือ | ศูนย์กลางเศรษฐกิจ ค้าขาย ตลาดเสรี (Free Market) | มอนสเตอร์ป่า สัตว์ทั่วไป แหล่งรวมเควสต์เริ่มต้น (Lv. 1–30) |
| **เดรัจฉานภูมิ** *(Beast)* | ป่าดึกดำบรรพ์ & เกาะอสูร | พืชโบราณ ป่าหมอก และสัตว์ยักษ์ | ดันเจี้ยนสัตว์อสูร แหล่งจับสัตว์ขี่และวัตถุดิบตีบวก (Lv. 30–50) |
| **เปรต/วิญญาณ** *(Ghost)* | แดนสุสานร้าง & เมืองผีสิง | ดินแดนภูตผีปีศาจ สไตล์ **SoulSaver / Ghost Online** | การดูดวิญญาณฟื้นเกจพลัง (Soul Absorbing) มอนสเตอร์ผี (Lv. 50–70) |
| **อสูรโลก** *(Asura)* | ลานประลองสงคราม | ยักษ์และเหล่านักรบกระหายเลือด **(Main PvP Zone)** | ลานประลอง 1-1, Team Battle (ธรรมะ vs อธรรม), กิลด์วอร์ (Lv. 60+) |
| **นรกภูมิ** *(Naraka)* | ขุมนรกเพลิงโลกันตร์ | ลาวาและโซ่ตรวน มัจจุราช และบอสปีศาจขนาดยักษ์ | World Boss Raid, ดันเจี้ยนความยากระดับสูงสุด (Lv. 70–90) |
| **เทวโลก** *(Deva)* | เกาะลอยฟ้า & แดนสวรรค์ | สถาปัตยกรรมสีขาวทอง เทวทูต พลังศักดิ์สิทธิ์ | เควสต์เปลี่ยนอาชีพขั้นสูง, Endgame Gear (Lv. 90–100) |

---

## ⚡ 2. สถาปัตยกรรมเทคโนโลยี (Tech Stack)

* **Game Engine (Client/Server):** Unity 6 (6000.3+)
* **Realtime Networking:** Photon Fusion 2 (Dedicated Server Mode + Client Prediction)
* **Master Backend API:** C# (.NET 8/9 Web API + gRPC)
* **Databases:**
  * **PostgreSQL:** เก็บข้อมูลผู้เล่น, กระเป๋าไอเทม, ระบบซื้อขาย/เทรดแบบ **ACID Transaction** (ป้องกันการปั๊มของ 100%)
  * **Redis:** Caching, บันทึก Session ชั่วคราว, ระบบประกาศข้าม Channel
* **Code Sharing:** Shared C# Library (`MapleMMO.Shared`) ใช้สูตรคำนวณและ Data Model ร่วมกันทั้งระบบ

---

## 🎮 3. ระบบการเล่นหลัก (Core Gameplay Mechanics)

### 🏃 การเคลื่อนไหว (MapleStory Platformer Physics)
* **เดิน / วิ่ง / สไลด์ตัว:** แรงเฉื่อยสมูท ควบคุมง่าย
* **กระโดดปรับระดับ (Variable Jump):** ปรับความสูงตามจังหวะกด
* **หมอบ / ก้ม (Crouch / Prone):** หลบการโจมตีแนวนอน
* **กระโดดทะลุลงพื้น (Down + Jump):** ทิ้งตัวลงแพลตฟอร์มชั้นล่าง
* **ปีนเชือก / บันได (Ladder & Rope):** ไต่ขึ้น-ลง และกระโดดฉากออกจากเชือก

### ⚔️ ระบบต่อสู้ & PvP (SoulSaver Style Action)
* **วิชาตัวเบา (Dash & Air Dash):** ดับเบิ้ลแท็บพุ่งตัวเลียดพื้น หรือพุ่งกลางอากาศ
* **Hit Reactions & CC:**
  * `Flinch` (ชะงัก) ขัดจังหวะสกิล
  * `Knockback` (กระเด็น) ผลักถอยหลัง
  * `Air Launch` (งัดลอยฟ้า) ตามไปคอมโบกลางอากาศ
  * `Down & Wake-up` (ล้ม + ลุกแล้วอมตะ) ป้องกัน Infinite Combo
* **Soul Absorbing (ดูดวิญญาณ):** ดูดลูกแก้ววิญญาณจากศัตรูเพื่อเติมเกจท่าไม้ตาย

---

## 📁 4. โครงสร้างโปรเจกต์ (Project Structure)

```text
WarOfSamsara/
 ├── Assets/
 │    ├── Scripts/
 │    │    ├── Player/               # Movement, Controller, Input, Combat
 │    │    │    ├── PlayerMovement2D.cs
 │    │    │    └── PlayerInputHandler.cs
 │    │    ├── Environment/          # One-Way Platforms, Ropes, Portals
 │    │    │    ├── OneWayPlatform.cs
 │    │    │    ├── ClimbableRope.cs
 │    │    │    └── MapPortal.cs
 │    │    ├── Combat/               # Hitboxes, Damage, Knockback, Soul Gauge
 │    │    └── Core/
 │    │         └── Shared/          # Stats, ItemDefinition, Inventory
 │    │              ├── CharacterStats.cs
 │    │              └── ItemDefinition.cs
 │    ├── Photon/
 │    │    └── Fusion/               # Photon Fusion 2 SDK
 │    ├── Scenes/                    # Maps (HumanTown, GhostCemetery, AsuraArena)
 │    └── Art/                       # 2D Sprites, Characters, Tilesets
 ├── README.md
 └── .gitignore
```

---

## 🗺️ 5. แผนการพัฒนาทีละเฟส (Roadmap)

* [x] **Phase 0:** ออกแบบสถาปัตยกรรม & คอนเซปต์ 6 ภพภูมิ
* [x] **Phase 1 (เบื้องต้น):** สคริปต์ฟิสิกส์ 2D Platformer (เดิน, โดด, ก้ม, โดดลงพื้น, ปีนเชือก)
* [ ] **Phase 2:** ติดตั้ง Photon Fusion 2 & ระบบ Network Synchronized Player
* [ ] **Phase 3:** ระบบวิชาตัวเบา (Dash / Air Dash) + Hitbox / Hurtbox & Knockback (SoulSaver Combat)
* [ ] **Phase 4:** ระบบดูดวิญญาณ (Soul Gauge) + ตัวเลขดาเมจลอย (Damage Skins)
* [ ] **Phase 5:** ระบบแมพ & วาปข้าม Channel (Henesys-style Portal)
* [ ] **Phase 6:** ลานประลอง PvP Arena (Free-For-All, ธรรมะ vs อธรรม)
