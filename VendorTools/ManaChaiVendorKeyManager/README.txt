ManaChai Vendor Signing Key Manager
Phase 2L.2

PURPOSE
-------
This tool is for the SOFTWARE VENDOR only.
Do NOT send this tool, its private key, its password, or a Vendor Key Backup to customers.

SOURCE IN GIT
-------------
VendorTools\ManaChaiVendorKeyManager\Program.cs.txt
VendorTools\ManaChaiVendorKeyManager\Build-KeyManager.ps1

The source file uses .cs.txt intentionally so the main ManaChaiLeasing WPF project
will not compile it as part of the customer application.

LOCAL TOOL LOCATION
-------------------
%LOCALAPPDATA%\ManaChaiLicenseVendor\Tool

PRIVATE KEY LOCATION
--------------------
%LOCALAPPDATA%\ManaChaiLicenseVendor\Keys

The Private Key is generated on the vendor machine only.
No Private Key is included in the Phase ZIP.

SECURITY
--------
- RSA 3072-bit signing key.
- Private key exported as encrypted PKCS#8 PEM.
- Password-based encryption uses AES-256-CBC + PBKDF2/SHA-256, 200,000 iterations.
- Password is never saved by the tool.
- Public key may be shared.
- Private key and Vendor Key Backup must remain private.

BACKUP
------
Use "Backup Key..." after creating the key.
Keep at least two encrypted backup ZIP files in separate physical locations.
Keep the password separate from those ZIP files.

RESTORE AFTER COMPUTER FAILURE
------------------------------
1. Restore/clone the ManaChaiLeasing Git repository on the new developer PC.
2. Run Build-KeyManager.ps1 to rebuild the Key Manager.
3. Open the Key Manager.
4. Choose "Restore Key...".
5. Select the encrypted Vendor Key Backup ZIP.
6. Enter the same Private Key password.
7. Verify the restored Key ID is exactly the original Key ID.

PUBLIC KEY PACKAGE
------------------
"Export Public Key Package..." creates a ZIP containing ONLY:
- vendor-public-key.pem
- key-info.json

It does NOT contain the Private Key.
This public package can safely be supplied for integration into the customer application.

IMPORTANT
---------
Losing both:
1) all Private Key backups, and
2) the password
means new licenses can no longer be signed with that original key.
Existing customer licenses remain verifiable by software that already contains
the corresponding Public Key.
