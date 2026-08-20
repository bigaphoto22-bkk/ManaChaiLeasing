ManaChai License Generator
Phase 2L.3

PURPOSE
-------
Vendor-only utility for generating signed ManaChaiLeasing license files.

LICENSE TYPES
-------------
1) Trial
   - Valid for exactly 7 x 24 hours from IssuedAtUtc.
   - ExpiresAtUtc is included in the signed payload.

2) Permanent
   - ExpiresAtUtc is null.
   - Still bound to one Machine ID.

MACHINE BINDING
---------------
The generator accepts only Machine IDs in this format:

MC-XXXX-XXXX-XXXX

The Machine ID is included in the signed payload.
Phase 2L.4 will verify that the license Machine ID matches the customer's actual PC.

SIGNING
-------
- Uses the Vendor Signing Key created in Phase 2L.2.
- Private key remains at:
  %LOCALAPPDATA%\ManaChaiLicenseVendor\Keys
- The private-key password is requested each time a license is generated.
- Password is not stored.
- Signature algorithm: RSA-PSS + SHA-256.

LICENSE FILE SCHEMA
-------------------
SchemaVersion: MCL-LIC-1

Signed fields:
- SchemaVersion
- KeyId
- LicenseId
- CustomerName
- MachineId
- LicenseType
- IssuedAtUtc
- ExpiresAtUtc

The signature is Base64 in SignatureBase64.

DEFAULT OUTPUT
--------------
Documents\ManaChai Licenses

The generated .license file may be renamed.
Do not manually edit the JSON contents; Phase 2L.4 will reject modified signed content.

IMPORTANT
---------
This tool is for the software vendor only.
Do NOT give the License Generator, Vendor Private Key, Vendor Key Backup,
or Private Key password to customers.

Phase 2L.3 does NOT yet lock the customer application.
Enforcement begins in Phase 2L.4 after client-side public-key validation is added.
