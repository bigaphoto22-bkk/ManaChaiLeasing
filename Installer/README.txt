ManaChaiLeasing - Deployment / Installer Tools
Phase 2L.6

============================================================
หลักการ
============================================================

Installer แยกจาก VendorTools อย่างชัดเจน

Installer\
    ใช้สำหรับ Publish โปรแกรมและสร้าง Setup.exe

VendorTools\
    ใช้สำหรับ Vendor Signing Key / License Generator เท่านั้น
    ห้ามส่งโฟลเดอร์ VendorTools ให้ลูกค้า

Setup.exe สามารถเป็นไฟล์เดียวกันสำหรับลูกค้าหลายรายได้
แต่แต่ละเครื่องต้องมี .license ที่ตรงกับ Machine ID ของเครื่องนั้น

============================================================
เวอร์ชันปัจจุบัน
============================================================

แก้ที่ไฟล์เดียว:

Installer\ReleaseVersion.txt

รูปแบบ:
X.Y.Z

ตัวอย่าง:
0.2.0
0.2.1
0.3.0
1.0.0

ชื่อ Setup จะถูกสร้างเป็น:

ManaChaiLeasing_Setup_X.Y.Z.exe

============================================================
ไฟล์ที่ใช้บ่อย
============================================================

00_Build_Release_Setup.bat
    แนะนำให้ใช้ไฟล์นี้เป็นหลัก

    ดับเบิลคลิกครั้งเดียวแล้วระบบจะ:
    1. ฝัง Vendor Public Key ชุดจริงอัตโนมัติ
    2. ตรวจว่า Public Key ไม่ใช่ NOT-CONFIGURED
    3. dotnet clean Release
    4. ลบ Publish เก่า
    5. dotnet publish Release / win-x64 / self-contained
    6. ตรวจ ManaChaiLeasing.exe
    7. หา Inno Setup Compiler
    8. Compile Setup.exe
    9. ตรวจ Setup.exe ที่สร้าง
    10. เปิด Explorer ชี้ไปที่ Setup.exe

01_Publish_Application.bat
    ใช้เฉพาะเมื่ออยาก Publish โปรแกรมโดยยังไม่สร้าง Setup

    หมายเหตุ:
    Script นี้ฝัง Public Key ให้อัตโนมัติก่อน Publish เช่นกัน

02_Build_Setup.bat
    ใช้เฉพาะเมื่อ Publish เสร็จแล้ว
    และต้องการ Compile Setup ใหม่โดยไม่ Publish ซ้ำ

03_Open_Setup_Output.bat
    เปิด:
    Installer\Output

============================================================
Release Workflow ที่แนะนำ
============================================================

1. UAT Phase ผ่าน
2. Commit + Push Source
3. เปลี่ยน Installer\ReleaseVersion.txt หากเป็น Release ใหม่
4. ดับเบิลคลิก:
   00_Build_Release_Setup.bat
5. ตรวจว่าได้:
   Installer\Output\ManaChaiLeasing_Setup_X.Y.Z.exe
6. ทดสอบ Setup บนเครื่อง Pilot/Clean PC
7. ตรวจ Activation / License / Database persistence
8. จึงส่ง Setup ให้ลูกค้า

============================================================
Public Key
============================================================

Release Build จะเรียก:

VendorTools\Embed-PublicKeyIntoClient.ps1

อัตโนมัติ

จึงไม่ต้องจำว่าต้องรัน
06_Embed_Public_Key_Into_Client.bat
ก่อนออก Release อีก

สิ่งที่ถูกฝังใน Client:
- Vendor Public Key
- Key ID

สิ่งที่ไม่ถูกฝัง:
- Vendor Private Key
- Private Key Password
- Vendor Key Backup

============================================================
ตำแหน่งไฟล์
============================================================

Publish Output:
Publish\ManaChaiLeasing-win-x64\

Setup Output:
Installer\Output\

ไฟล์ Publish และ Setup Output ไม่ควร Commit เข้า Git

.gitignore ของโปรเจกต์ควรมี:

Publish/
Installer/Output/

============================================================
Inno Setup
============================================================

ระบบจะหา ISCC.exe ตามลำดับ:

1. PATH
2. Program Files (x86)\Inno Setup 6
3. Program Files\Inno Setup 6
4. LocalAppData\Programs\Inno Setup 6

ถ้าไม่พบ จะหยุดก่อนสร้าง Setup และแจ้งข้อความชัดเจน

============================================================
Installer identity
============================================================

AppId เดิมยังคงใช้:

A37C3B29-821A-4EE0-9E9D-A01C2B77F001

ดังนั้น Setup รุ่นใหม่จะถือเป็นโปรแกรมตัวเดิม
ไม่สร้างรายการโปรแกรมซ้ำเพราะเปลี่ยนเวอร์ชัน

Database และ License เก็บใน LocalAppData
ไม่ได้เก็บใน Program Files
ดังนั้น uninstall/reinstall จะไม่ตั้งใจลบข้อมูลธุรกิจหรือ License

============================================================
ความปลอดภัย
============================================================

Setup.exe ที่สร้าง:
- ไม่มี Private Key
- ไม่มี License Generator
- ไม่มี Vendor Key Manager
- ไม่มี VendorTools
- มีเฉพาะ Client Application ที่ Publish แล้ว

ต่อให้ Setup.exe ถูก Copy:
ผู้ใช้เครื่องอื่นยังต้องมี .license
ที่ Signature ถูกต้องและ Machine ID ตรงกับเครื่องนั้น

============================================================
Phase 2L.6
============================================================

Phase นี้เป็น Deployment Convenience เท่านั้น
ไม่มี Migration ใหม่
ไม่เปลี่ยนฐานข้อมูล
ไม่เปลี่ยนกติกาจำนำ / ต่อดอก / ไถ่ถอน
ไม่เปลี่ยน Signing algorithm หรือ License schema


============================================================
Phase 2L.6 Fix1
============================================================

แก้การตรวจ VendorPublicKey.cs หลัง Embed Public Key

เดิม:
ตรวจคำว่า NOT-CONFIGURED ทั้งไฟล์ ซึ่งทำให้ false positive
เพราะคำนี้ยังอยู่ใน IsConfigured logic แม้ Key จริงถูกฝังแล้ว

ใหม่:
ตรวจเฉพาะ:
- public const string KeyId = "MC-KEY-..."
- private const string PemBase64 ต้องไม่ว่าง

ไม่มีการเปลี่ยน Public Key, Private Key, License schema หรือ Release workflow อื่น


============================================================
Phase 2L.6 Fix2
============================================================

แก้ PowerShell argument forwarding ของ dotnet publish

ปัญหาเดิม:
Invoke-Checked รับ option -o แล้ว PowerShell ตีความเป็น Common Parameter
(-OutVariable / -OutBuffer) ของ function ทำให้เกิด:
"Parameter name 'o' is ambiguous"

แก้ใหม่:
เรียก dotnet clean / dotnet publish โดยตรง
และส่ง option เป็น string arguments ชัดเจน เช่น:
"-o" $PublishDir

ไม่มีการเปลี่ยน:
- Public Key
- License logic
- Clock rollback
- Inno Setup template
- Database
