# ManaChaiLeasing — คู่มือสร้าง Release และติดตั้งเครื่องลูกค้าใหม่

> สำหรับโปรเจกต์ **มานะชัย ลิสซิ่ง (ManaChaiLeasing)**
>
> เอกสารนี้เป็นคู่มือสำหรับ Developer/Vendor ใช้ตั้งแต่เตรียม Source Code,
> สร้าง Setup.exe, นำไปติดตั้งเครื่องลูกค้าใหม่, ออก License,
> Activate โปรแกรม และตรวจระบบก่อนส่งมอบ
>
> **เอกสารนี้ควรเก็บใน Git**
>
> ตำแหน่งแนะนำ:
>
> `Installer\CLIENT_DEPLOYMENT_GUIDE_TH.md`

---

## 1. หลักสำคัญก่อนเริ่ม

โปรแกรมมานะชัย ลิสซิ่งเป็น Windows Desktop Application แบบ Offline

Release สำหรับลูกค้าใช้รูปแบบ:

```text
Source Code
    ↓
Release Build
    ↓
Publish win-x64 Self-contained
    ↓
Inno Setup
    ↓
ManaChaiLeasing_Setup_X.Y.Z.exe
    ↓
ติดตั้งเครื่องลูกค้า
    ↓
โปรแกรมแสดง Machine ID
    ↓
Vendor ออก .license ให้ Machine ID นั้น
    ↓
ลูกค้า Import License
    ↓
เริ่มใช้งาน
```

### สิ่งที่ลูกค้าต้องได้รับ

ปกติส่งเพียง:

```text
1. ManaChaiLeasing_Setup_X.Y.Z.exe
2. ไฟล์ .license ของเครื่องลูกค้า
```

ไฟล์ `.license` จะส่งภายหลังก็ได้ หลังจากติดตั้งแล้วและได้รับ Machine ID จากเครื่องลูกค้า

### สิ่งที่ห้ามส่งให้ลูกค้า

ห้ามส่ง:

```text
VendorTools\
ManaChaiVendorKeyManager.exe
ManaChaiLicenseGenerator.exe
vendor-private-key.pem
Vendor Key Backup ZIP
Password ของ Private Key
Source Code ทั้งโปรเจกต์
```

---

# PART A — เตรียมเครื่อง Developer ก่อนสร้าง Release

## 2. โปรแกรมที่เครื่อง Developer ต้องมี

เครื่องที่ใช้สร้าง Setup ควรมี:

- Git
- .NET SDK ที่โปรเจกต์ใช้งาน
- Inno Setup 6
- Source Code ของ ManaChaiLeasing
- Vendor Signing Key ชุดจริง

ตำแหน่งโปรเจกต์ปัจจุบัน:

```text
C:\Dev\PawnShop-2\ManaChaiLeasing
```

---

## 3. ตรวจว่า Source Code อยู่ในสถานะที่ผ่าน UAT แล้ว

**อย่าสร้าง Release จาก Source ที่ยังไม่ได้ UAT**

เปิด PowerShell แล้วรัน:

```powershell
cd C:\Dev\PawnShop-2\ManaChaiLeasing
git status
```

ก่อนออก Release ควรเห็น:

```text
On branch main
Your branch is up to date with 'origin/main'.

nothing to commit, working tree clean
```

ถ้ายังมีไฟล์ Modified / Untracked ที่เป็นงานจริงของโปรเจกต์
ให้ตรวจสอบและ Commit ให้เรียบร้อยก่อน

ตัวอย่าง:

```powershell
cd C:\Dev\PawnShop-2\ManaChaiLeasing
git add .
git commit -m "Prepare release"
git push
git status
```

> **หลักการ:** Release ที่ส่งให้ลูกค้าควรย้อนกลับไปหา Commit ใน Git ได้เสมอ

---

## 4. ตรวจเลข Version ก่อน Build

เลข Version ของโปรแกรมมี Source of Truth จุดเดียว:

```text
Installer\ReleaseVersion.txt
```

ตรวจด้วย:

```powershell
cd C:\Dev\PawnShop-2\ManaChaiLeasing
Get-Content .\Installer\ReleaseVersion.txt
```

ตัวอย่าง:

```text
0.3.4
```

เลข Version ต้องเป็นรูปแบบ:

```text
X.Y.Z
```

เช่น:

```text
0.3.4
0.3.5
0.4.0
1.0.0
```

เลขนี้จะถูกนำไปใช้กับ:

- Version ในตัวโปรแกรม
- Version ใน Windows Installed Apps
- File Version ของ EXE
- ชื่อ Setup.exe

ตัวอย่าง:

```text
ManaChaiLeasing_Setup_0.3.4.exe
```

### ถ้าต้องเปลี่ยน Version

แก้เฉพาะไฟล์:

```text
Installer\ReleaseVersion.txt
```

จากนั้น Build/UAT และ Commit Version ใหม่นั้นเข้า Git ก่อนสร้าง Release ที่จะส่งจริง

---

## 5. ตรวจ Vendor Signing Key

One-click Release จะฝัง **Vendor Public Key** ลงใน Client Application ให้อัตโนมัติ

Key ของ Vendor อยู่ที่:

```text
%LOCALAPPDATA%\ManaChaiLicenseVendor\Keys
```

ตรวจได้ด้วย PowerShell:

```powershell
Get-ChildItem "$env:LOCALAPPDATA\ManaChaiLicenseVendor\Keys"
```

โดยปกติควรมี:

```text
vendor-private-key.pem
vendor-public-key.pem
key-info.json
```

### สำคัญ

- `vendor-public-key.pem` ใช้สำหรับฝัง Public Key ลง Client
- `vendor-private-key.pem` ใช้สำหรับ Sign License เท่านั้น
- Private Key ห้ามส่งให้ลูกค้า
- Password ของ Private Key ห้ามใส่ใน Source Code หรือ Git

ถ้า Key หายหรือย้ายเครื่อง Developer ใหม่
ให้ Restore จาก Vendor Key Backup ก่อนสร้าง Release

---

# PART B — สร้าง Setup.exe

## 6. วิธีที่แนะนำ: One-click Release

โปรเจกต์มี Script:

```text
Installer\00_Build_Release_Setup.bat
```

สามารถดับเบิลคลิกไฟล์นี้จาก Explorer ได้เลย

หรือเรียกจาก PowerShell:

```powershell
cd C:\Dev\PawnShop-2\ManaChaiLeasing
.\Installer\00_Build_Release_Setup.bat
```

Script จะทำให้อัตโนมัติ:

```text
1. อ่าน Vendor Public Key ชุดจริง
2. ฝัง Public Key ลง Licensing\VendorPublicKey.cs
3. ตรวจว่า Public Key ถูก Configure จริง
4. dotnet clean -c Release
5. ลบ Publish เก่า
6. dotnet publish Release
7. Target = win-x64
8. Self-contained = true
9. ตรวจว่า ManaChaiLeasing.exe ถูกสร้าง
10. หา Inno Setup Compiler
11. Compile Installer
12. ตรวจ Setup.exe
13. เปิด Explorer ชี้ไปยัง Setup.exe
```

ดังนั้น **ไม่ต้องรัน Embed Public Key เองก่อนออก Release**
เพราะ One-click Release ทำให้อัตโนมัติแล้ว

---

## 7. ถ้า Build สำเร็จ Setup จะอยู่ที่ไหน

ไฟล์จะอยู่ที่:

```text
C:\Dev\PawnShop-2\ManaChaiLeasing\Installer\Output
```

รูปแบบชื่อ:

```text
ManaChaiLeasing_Setup_X.Y.Z.exe
```

ตัวอย่าง:

```text
ManaChaiLeasing_Setup_0.3.4.exe
```

Publish files จะอยู่ที่:

```text
C:\Dev\PawnShop-2\ManaChaiLeasing\Publish\ManaChaiLeasing-win-x64
```

---

## 8. Setup เป็น Self-contained

Release ใช้:

```text
Release
win-x64
self-contained true
```

ดังนั้นเครื่องลูกค้า **ไม่จำเป็นต้องติดตั้ง .NET Runtime แยก**

เครื่องลูกค้าต้องเป็น Windows x64 ที่รองรับโปรแกรม

---

## 9. สิ่งที่ไม่ต้อง Commit เข้า Git

ห้าม Commit Build Output:

```text
Publish\
Installer\Output\
```

`.gitignore` ของโปรเจกต์ควรมีอย่างน้อย:

```gitignore
Publish/
Installer/Output/
```

และไม่ควร Commit:

```text
*.license
Vendor Private Key
Vendor Key Backup
Password
```

---

# PART C — ตรวจ Setup ก่อนส่งลูกค้า

## 10. ตรวจชื่อและ Version

ตรวจว่าไฟล์ที่ได้ตรงกับ Version ปัจจุบัน

ตัวอย่าง:

```text
Installer\ReleaseVersion.txt
= 0.3.4

Setup ที่ได้
= ManaChaiLeasing_Setup_0.3.4.exe
```

ถ้าเลขไม่ตรง **อย่าส่งลูกค้า**

---

## 11. ตรวจ Hash ของ Setup — แนะนำ

เพื่อให้รู้ว่าไฟล์ที่ส่งไปไม่เสียหรือถูกเปลี่ยนระหว่างทาง
สามารถเก็บ SHA-256 ไว้ได้

PowerShell:

```powershell
cd C:\Dev\PawnShop-2\ManaChaiLeasing
Get-FileHash .\Installer\Output\ManaChaiLeasing_Setup_0.3.4.exe -Algorithm SHA256
```

เปลี่ยน `0.3.4` ให้ตรง Version จริง

เก็บค่า Hash ไว้ใน Note ของ Release ได้
แต่ไม่จำเป็นต้องให้ผู้ใช้งานร้านตรวจทุกครั้ง

---

## 12. ควรทดสอบ Setup บน Clean/Pilot PC ก่อนส่งจริง

ก่อนส่ง Release ใหม่ให้ลูกค้า ควรทดสอบอย่างน้อยหนึ่งครั้งบน:

- เครื่อง Test ที่ไม่มี Source Code
- VM ใหม่
- หรือ Clean/Pilot PC

ตรวจว่า:

```text
Setup เปิดได้
ติดตั้งได้
Shortcut ถูกต้อง
Icon ถูกต้อง
โปรแกรมเปิดได้
Activation Window ขึ้น
Machine ID แสดงได้
Import License ได้
Version ถูกต้อง
Database สร้างได้
```

### อย่าสร้างตั๋วทดสอบปลอมในฐานข้อมูล Production ของลูกค้า

ระบบตั้งใจเก็บประวัติธุรกรรมไว้
ดังนั้น Full Transaction UAT ควรทำบน Test/Pilot Database ก่อนส่งมอบ

บนเครื่อง Production ใหม่ให้ตรวจเฉพาะงานที่ไม่สร้างประวัติปลอมก่อน
แล้วเริ่มบันทึกธุรกรรมจริงเมื่อร้านเริ่มใช้งาน

---

# PART D — ติดตั้งเครื่องลูกค้าใหม่

## 13. ส่ง Setup ไปเครื่องลูกค้า

ส่งเฉพาะ:

```text
ManaChaiLeasing_Setup_X.Y.Z.exe
```

วิธีส่งจะเป็น:

- USB Flash Drive
- Google Drive / OneDrive
- โปรแกรม Remote Support
- วิธีส่งไฟล์อื่นที่เชื่อถือได้

ได้ทั้งหมด

ตัวโปรแกรมหลังติดตั้งทำงาน Offline

---

## 14. เปิด Setup ที่เครื่องลูกค้า

ดับเบิลคลิก:

```text
ManaChaiLeasing_Setup_X.Y.Z.exe
```

Installer ต้องใช้สิทธิ์ Administrator
Windows จะแสดง UAC ให้ยืนยัน

กด Yes

ตำแหน่งติดตั้งปกติ:

```text
C:\Program Files\ManaChaiLeasing
```

Installer มีตัวเลือกสร้าง Desktop Shortcut

---

## 15. กรณี Windows SmartScreen เตือน

Installer ปัจจุบันยังไม่ได้ Sign ด้วย Commercial Code Signing Certificate

Windows บางเครื่องจึงอาจแสดง SmartScreen

ต้องตรวจให้แน่ใจก่อนว่าไฟล์เป็น Setup ที่สร้างจากเครื่อง Developer ของเราเอง

ถ้าเป็นไฟล์ที่ถูกต้อง จึงเลือก:

```text
More info
→ Run anyway
```

ถ้าไม่แน่ใจว่าไฟล์มาจากไหน **อย่าฝืนเปิด**

---

# PART E — Activation เครื่องลูกค้าใหม่

## 16. เปิดโปรแกรมครั้งแรก

หลังติดตั้ง เปิด:

```text
มานะชัย ลิสซิ่ง
```

ถ้าเครื่องยังไม่มี License
โปรแกรมจะเปิดหน้า Activation แทน Main Program

หน้าจอจะแสดง:

```text
Machine ID
MC-XXXX-XXXX-XXXX
```

ตัวอย่าง:

```text
MC-AB12-CD34-EF56
```

---

## 17. เอา Machine ID จากเครื่องลูกค้า

กดปุ่ม Copy Machine ID ในหน้า Activation

จากนั้นส่ง Machine ID กลับมาที่เครื่อง Vendor/Developer

สามารถส่งเป็นข้อความได้ เช่น:

```text
ลูกค้า: ร้านมานะชัย
Machine ID: MC-AB12-CD34-EF56
```

### สำคัญ

License จะผูกกับ **Machine ID เครื่องนั้น**

ถ้าออก License ผิด Machine ID
โปรแกรมจะไม่ยอมใช้ License

---

# PART F — ออก License ที่เครื่อง Developer

## 18. เปิด ManaChai License Generator

ที่เครื่อง Developer ไปที่:

```text
C:\Dev\PawnShop-2\ManaChaiLeasing\VendorTools
```

ถ้า License Generator เคย Build แล้ว ใช้:

```text
04_Open_License_Generator.bat
```

ถ้ายังไม่เคย Build / EXE หาย / ย้าย Developer PC ใหม่ ใช้:

```text
02_Build_and_Open_License_Generator.bat
```

สามารถเปิดผ่าน Explorer ได้

---

## 19. กรอกข้อมูลเพื่อสร้าง License

ใน License Generator กรอก:

```text
Customer Name
Machine ID
License Type
Private Key Password
```

ประเภท License ปัจจุบัน:

### Trial

```text
Trial 7 วัน
```

มีอายุ:

```text
7 × 24 ชั่วโมง
```

นับจากเวลาที่ออก License

### Permanent

```text
Permanent
```

ไม่มีวันหมดอายุ
แต่ยังผูกกับ Machine ID เครื่องเดียว

---

## 20. สร้างไฟล์ .license

เมื่อ Generate สำเร็จ ไฟล์โดยปกติจะอยู่ที่:

```text
Documents\ManaChai Licenses
```

นามสกุล:

```text
.license
```

สามารถ Rename ชื่อไฟล์ได้
แต่ **ห้ามเปิดแล้วแก้ JSON ด้านใน**

เพราะ Digital Signature จะไม่ผ่านถ้าเนื้อหาถูกแก้ไข

---

## 21. ส่งอะไรกลับให้ลูกค้า

ส่งเฉพาะไฟล์:

```text
ชื่อใดก็ได้.license
```

ห้ามส่ง:

```text
License Generator
Private Key
Password
Key Backup
VendorTools
```

---

# PART G — Import License ที่เครื่องลูกค้า

## 22. Import License

กลับมาที่หน้า Activation ของเครื่องลูกค้า

กด:

```text
นำเข้า License...
```

เลือกไฟล์:

```text
*.license
```

โปรแกรมจะตรวจ:

```text
Digital Signature
Key ID
License Schema
Machine ID
License Type
วันหมดอายุ (ถ้ามี)
Clock Rollback Protection (Trial)
```

ถ้าถูกต้อง โปรแกรมจะแสดง:

```text
เปิดใช้งานสำเร็จ
```

จากนั้นเข้าสู่ Main Program ได้

---

## 23. License ถูกเก็บที่ไหน

โปรแกรมจะติดตั้ง License ไปที่:

```text
%LOCALAPPDATA%\ManaChaiLeasing\License\ManaChaiLeasing.license
```

ไม่จำเป็นต้องเก็บ `.license` ไว้ใน Program Files

ควรเก็บสำเนาไฟล์ License ฝั่ง Vendor แยกไว้ตามลูกค้า
แต่ **ไม่ควร Commit Customer License เข้า Git**

---

# PART H — ตั้งค่าหลังติดตั้ง

## 24. ตรวจ Version ในโปรแกรม

หลัง Activate แล้วตรวจว่า Version ในโปรแกรมตรงกับ Setup

ตัวอย่าง:

```text
v0.3.4
```

และใน:

```text
ตั้งค่า
→ สิทธิ์การใช้งาน
```

ควรเห็น License ถูกต้อง

---

## 25. Database อยู่ที่ไหน

Database จริงไม่ได้อยู่ใน Program Files

อยู่ที่:

```text
%LOCALAPPDATA%\ManaChaiLeasing\Data\ManaChaiLeasing.db
```

ข้อดีคือ:

- Update โปรแกรมได้โดยไม่ทับ Database
- Uninstall/Reinstall ไม่ได้ตั้งใจลบ Database
- Program Files แยกจากข้อมูลธุรกิจ

---

## 26. ตั้ง Auto Backup — สำคัญมาก

หลังติดตั้งเครื่องลูกค้าใหม่
ควรตั้ง Auto Backup ก่อนเริ่มใช้งานจริง

เข้า:

```text
ตั้งค่า
→ Auto Backup
```

เลือกโฟลเดอร์บน:

```text
USB Flash Drive
External HDD / SSD
หรือ Drive อื่นที่ไม่ใช่ C:
```

**ไม่แนะนำให้ Backup ลง C:**
เพราะถ้า Disk/Windows เสีย Database และ Backup อาจหายพร้อมกัน

ระบบจะสร้างประมาณ:

```text
ManaChaiLeasing_AutoBackup_yyyyMMdd.db
```

หลักปัจจุบัน:

- Auto Backup ตอนเปิดโปรแกรม
- Auto Backup หลังธุรกรรมสำคัญ
- หนึ่งไฟล์ต่อวัน โดย Refresh ไฟล์ของวันนั้น
- เก็บย้อนหลังประมาณ 30 วัน

### ทดสอบ

หลังเลือก Folder แล้ว
ตรวจว่าไฟล์ `.db` ถูกสร้างใน External Drive จริง

---

# PART I — Final Checklist ก่อนส่งมอบ

## 27. Checklist เครื่องลูกค้าใหม่

ก่อนบอกลูกค้าว่าใช้งานได้ ให้เช็ก:

```text
[ ] Setup ติดตั้งสำเร็จ
[ ] Icon โปรแกรมถูกต้อง
[ ] Version ถูกต้อง
[ ] Activation สำเร็จ
[ ] Machine ID ตรงกับ License
[ ] โปรแกรมเปิดเข้าหน้าหลักได้
[ ] Database Status ปกติ
[ ] Single Instance ทำงาน
[ ] Auto Backup ตั้งค่าแล้ว
[ ] Auto Backup สร้างไฟล์จริง
[ ] Search เปิดได้
[ ] Settings เปิดได้
[ ] Thai ID Reader ไม่มีก็ยังกรอกข้อมูลมือได้
```

### สิ่งที่ยังไม่ควรทำบน Production ใหม่เพื่อ "ลองเฉย ๆ"

ไม่ควรสร้าง:

```text
ตั๋วจำนำปลอม
ต่อดอกปลอม
ไถ่ถอนปลอม
```

เพราะรายการธุรกรรมถูกออกแบบให้เก็บเป็นประวัติ

ทดสอบธุรกรรมเต็มรูปแบบบน Pilot/Test Database แทน

---

# PART J — หลังส่งมอบลูกค้า

## 28. สิ่งที่ควรเก็บฝั่ง Vendor

ต่อหนึ่ง Release ควรรู้:

```text
Version ที่ส่ง
Git Commit ที่เป็น Source
วันที่ส่ง
ชื่อลูกค้า
Machine ID
ประเภท License
```

Customer License สามารถเก็บใน Folder ภายนอก Git ได้

ตัวอย่างแนวคิด:

```text
Documents\
  ManaChai Licenses\
    Customer-A\
    Customer-B\
```

---

# PART K — ถ้าเครื่องลูกค้าพังและต้องย้ายไปเครื่องใหม่

## 29. Database กับ License เป็นคนละเรื่อง

ถ้า PC ลูกค้าพังแล้วเปลี่ยนเครื่อง:

```text
Database Backup
→ Restore เข้าเครื่องใหม่ได้
```

แต่:

```text
License เครื่องเก่า
→ โดยปกติใช้กับเครื่องใหม่ไม่ได้
```

เพราะ Machine ID เปลี่ยน

ดังนั้นขั้นตอนคือ:

```text
1. ติดตั้ง Setup บนเครื่องใหม่
2. เปิดโปรแกรมเพื่อดู Machine ID ใหม่
3. Vendor ออก License ใหม่ให้ Machine ID ใหม่
4. Activate
5. Restore Database จาก Backup ล่าสุด
6. เปิดโปรแกรมใหม่
7. ตรวจข้อมูล
8. ตั้ง Auto Backup ใหม่
```

---

# PART L — อัปเดต Version บนเครื่องลูกค้าเดิม

## 30. ถ้าเป็นการ Update ไม่ใช่เครื่องใหม่

Setup ใช้ AppId เดิม:

```text
A37C3B29-821A-4EE0-9E9D-A01C2B77F001
```

ดังนั้น Setup Version ใหม่จะถือว่าเป็น Application ตัวเดิม

โดยทั่วไป:

```text
1. Backup Database ก่อน
2. ปิดโปรแกรม
3. รัน Setup Version ใหม่
4. ติดตั้งทับ
5. เปิดโปรแกรม
6. ตรวจ Version
7. ตรวจ License
8. ตรวจ Database
9. ตรวจ Auto Backup
```

Database และ License อยู่ใน LocalAppData
จึงไม่ได้อยู่ใน Program Files ที่ Installer อัปเดต

---

# PART M — Troubleshooting

## 31. One-click Build แจ้งไม่พบ Public Key

ตรวจ:

```powershell
Get-ChildItem "$env:LOCALAPPDATA\ManaChaiLicenseVendor\Keys"
```

ต้องมี:

```text
vendor-public-key.pem
key-info.json
```

ถ้าหาย ให้ Restore Vendor Key ก่อน

---

## 32. Build แจ้งไม่พบ Inno Setup Compiler

ตรวจว่าติดตั้ง:

```text
Inno Setup 6
```

Script จะค้น `ISCC.exe` จาก:

```text
PATH
Program Files (x86)\Inno Setup 6
Program Files\Inno Setup 6
LocalAppData\Programs\Inno Setup 6
```

ติดตั้ง/แก้ Inno Setup แล้วรัน:

```text
Installer\00_Build_Release_Setup.bat
```

ใหม่

---

## 33. Client แจ้ง Wrong Machine

หมายความว่า `.license` ถูกออกให้ Machine ID คนละเครื่อง

วิธีแก้:

```text
1. Copy Machine ID จากหน้า Activation เครื่องลูกค้าอีกครั้ง
2. ออก License ใหม่
3. ส่ง .license ใหม่
4. Import ใหม่
```

อย่าแก้ไฟล์ `.license` ด้วยมือ

---

## 34. Client แจ้ง Public Key Not Configured

Release ชุดนั้นไม่ได้ฝัง Vendor Public Key ถูกต้อง

**อย่าออก License ใหม่เพื่อแก้ปัญหานี้**

ให้กลับมาที่ Developer PC แล้วสร้าง Setup ใหม่ด้วย:

```powershell
cd C:\Dev\PawnShop-2\ManaChaiLeasing
.\Installer\00_Build_Release_Setup.bat
```

จากนั้นติดตั้ง Release ที่ถูกต้อง

---

## 35. Trial แจ้ง Clock Rollback

ตรวจวันที่/เวลา Windows ของเครื่องลูกค้า

ถ้าเวลาถูกย้อนกลับ โปรแกรมจะ Block Trial License ตามระบบป้องกัน Clock Rollback

Permanent License ไม่มีวันหมดอายุและไม่ใช้กฎ Trial แบบเดียวกัน

---

# PART N — Git Policy สำหรับ Deployment

## 36. ไฟล์ Deployment ที่ควร Commit

ควร Commit:

```text
Installer\CLIENT_DEPLOYMENT_GUIDE_TH.md
Installer\ReleaseVersion.txt
Installer\*.bat
Installer\*.ps1
Installer\ManaChaiLeasing_Installer.template.iss
VendorTools Source
Licensing\VendorPublicKey.cs
```

`VendorPublicKey.cs` มี Public Key เท่านั้น
จึงไม่ถือเป็น Secret

---

## 37. ไฟล์ที่ห้าม Commit

ห้าม Commit:

```text
Publish\
Installer\Output\
*.license
vendor-private-key.pem
Vendor Key Backup ZIP
Private Key Password
Database ของลูกค้า
Backup Database ของลูกค้า
Support ZIP ที่อาจมาจากลูกค้า
```

---

# PART O — Release Workflow แบบย่อ

เมื่อคุ้นแล้ว ใช้ Checklist นี้ได้เลย:

```text
1. UAT PASS
2. ตรวจ Version
3. Commit + Push
4. git status = clean
5. ตรวจ Vendor Keys
6. Run Installer\00_Build_Release_Setup.bat
7. ได้ ManaChaiLeasing_Setup_X.Y.Z.exe
8. Test Setup บน Clean/Pilot PC
9. ส่ง Setup ให้ลูกค้า
10. ลูกค้าติดตั้ง
11. รับ Machine ID
12. Vendor Generate .license
13. ส่ง .license
14. Client Import License
15. ตรวจ Version / License / Database
16. ตั้ง Auto Backup ไป External Drive
17. ตรวจ Backup ถูกสร้างจริง
18. ส่งมอบ
```

---

# หมายเหตุสำคัญ

- Setup.exe ชุดเดียวกันสามารถใช้ติดตั้งหลายเครื่องได้
- แต่ `.license` ต้องออกแยกตาม Machine ID ของแต่ละเครื่อง
- Customer Photo ไม่ใช่ส่วนหนึ่งของระบบ
- Thai ID Reader เป็นเพียงตัวช่วยกรอกข้อมูล
- ไม่มี Reader โปรแกรมยังต้องใช้งานด้วยการกรอกมือได้เต็มรูปแบบ
- โปรแกรมหลักทำงาน Offline
- Database Backup สำคัญกว่าการ Reinstall โปรแกรม เพราะตัวโปรแกรมสร้างใหม่ได้ แต่ข้อมูลธุรกิจสร้างย้อนหลังไม่ได้

---

**เอกสารนี้เป็นส่วนหนึ่งของ Source Repository และควรอัปเดตเมื่อ Release/License/Installer Workflow เปลี่ยน**
