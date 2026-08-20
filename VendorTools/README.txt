ManaChai Vendor Tools
Phase 2L.3.1

============================================================
จุดประสงค์
============================================================
โฟลเดอร์นี้เป็นเครื่องมือสำหรับ "ผู้ขายโปรแกรม" เท่านั้น
ไม่ควรส่ง VendorTools ให้ลูกค้า

ไฟล์ .bat ด้านล่างใช้สำหรับเปิดเครื่องมือโดยไม่ต้องจำคำสั่ง PowerShell

============================================================
ไฟล์ที่ใช้บ่อย
============================================================

01_Build_and_Open_Key_Manager.bat
    ใช้เมื่อ:
    - ใช้เครื่องนี้ครั้งแรก
    - Source ของ Key Manager มีการเปลี่ยนแปลง
    - EXE หาย
    - ย้ายไปเครื่อง Developer ใหม่

    ทำงาน:
    - Build ManaChai Vendor Signing Key Manager
    - Publish แบบ self-contained win-x64
    - เปิดโปรแกรมให้อัตโนมัติ

02_Build_and_Open_License_Generator.bat
    ใช้เมื่อ:
    - ใช้ License Generator ครั้งแรก
    - Source Generator มีการเปลี่ยนแปลง
    - EXE หาย
    - ย้ายไปเครื่อง Developer ใหม่

    ทำงาน:
    - Build ManaChai License Generator
    - Publish แบบ self-contained win-x64
    - เปิดโปรแกรมให้อัตโนมัติ

03_Open_Key_Manager.bat
    ใช้เปิด Key Manager ที่ Build ไว้แล้ว
    ไม่ Build ซ้ำ

04_Open_License_Generator.bat
    ใช้เปิด License Generator ที่ Build ไว้แล้ว
    ไม่ Build ซ้ำ

05_Open_Vendor_Folder.bat
    เปิดโฟลเดอร์:
    %LOCALAPPDATA%\ManaChaiLicenseVendor

============================================================
ตำแหน่งเครื่องมือที่ Build แล้ว
============================================================

%LOCALAPPDATA%\ManaChaiLicenseVendor\Tool

ตัวอย่าง:
ManaChaiVendorKeyManager.exe
ManaChaiLicenseGenerator.exe

============================================================
ตำแหน่ง Vendor Signing Key
============================================================

%LOCALAPPDATA%\ManaChaiLicenseVendor\Keys

ไฟล์สำคัญ:
vendor-private-key.pem
vendor-public-key.pem
key-info.json

Private Key อยู่ "นอก Git Project"

============================================================
สิ่งที่ต้อง Backup จริง ๆ
============================================================

สำคัญที่สุด:
1. Vendor Key Backup ZIP
2. Password ของ Private Key

ควรมี Vendor Key Backup อย่างน้อย 2 ชุด
และเก็บคนละสถานที่

Password ควรเก็บแยกจาก Backup ZIP

============================================================
ถ้าเครื่อง Developer พัง
============================================================

1. ติดตั้ง Git / .NET SDK ตาม Environment ของโปรเจกต์
2. Clone ManaChaiLeasing repository
3. เปิด VendorTools
4. ดับเบิลคลิก:
   01_Build_and_Open_Key_Manager.bat
5. ใน Key Manager เลือก Restore Key...
6. เลือก Vendor Key Backup ZIP
7. ใส่ Password เดิม
8. ตรวจว่า Key ID ตรงกับ Key ID เดิม
9. ดับเบิลคลิก:
   02_Build_and_Open_License_Generator.bat
10. สามารถออก License ต่อด้วย Signing Key เดิมได้

============================================================
สิ่งที่ห้ามส่งให้ลูกค้า
============================================================

ห้ามส่ง:
- VendorTools ทั้งโฟลเดอร์
- ManaChaiVendorKeyManager.exe
- ManaChaiLicenseGenerator.exe
- vendor-private-key.pem
- Vendor Key Backup ZIP
- Password ของ Private Key

ส่งให้ลูกค้าได้เฉพาะสิ่งที่จำเป็น เช่น:
- โปรแกรม ManaChaiLeasing / Installer
- ไฟล์ .license ที่ออกให้ลูกค้ารายนั้น
- Public Key ไม่ถือเป็นความลับ แต่ปกติลูกค้าไม่จำเป็นต้องได้รับแยก

============================================================
Git
============================================================

Source ของ Vendor Tools:
เก็บใน Git ได้

Private Key:
ไม่อยู่ใน Git Project และห้ามนำเข้า Git

Generated customer *.license:
ถูก ignore ใน .gitignore

============================================================
Phase
============================================================

Phase 2L.3.1 = Convenience Tools เท่านั้น

ไม่มีการเปลี่ยน:
- Signing algorithm
- Machine ID
- Trial expiry
- Permanent license
- Customer application lock

การบังคับ License ฝั่งลูกค้าจะเริ่มใน Phase 2L.4
