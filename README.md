# Password Manager

A simple and secure password manager built in **C#**, using **AES encryption** to safely store passwords.

---

## Features

- **Master Password Protection** at startup  
- Secure storage of passwords in an encrypted file (`vault.dat`)  
- Add, delete, search, and view passwords  
- Generate strong random passwords of desired length  
- Automatic load and save of password vault  

---


## Contributors

- [[Farnaz](https://github.com/Farnaztr)]
- [[](https://github.com/TankeBiPelak)]
- [[Farnaz]((https://github.com/BxharAhmadi))]


## Installation & Run

1. Open the project in **Visual Studio** or **VS Code**.  
2. Make sure **.NET Core 3.1** or higher is installed.  
3. Run the program and enter your **Master Password**.  

---

## How to Use

- **Add new password:** choose `1` from the menu  
- **List saved passwords:** choose `2`  
- **Search password:** choose `3`  
- **Delete password:** choose `4`  
- **Generate random password:** choose `5`  
- **Exit program:** choose `0`  

**Note:**  
- If no vault exists, the program will create it automatically.  
- Master Password is used for encrypting/decrypting data, so do not forget it.  

---

## Security Notes

- Passwords are encrypted using **AES**.  
- **Salt** and **IV** are used for enhanced security.  
- No passwords are stored as plain text outside the program.  

---

## Example `vault.dat` File

```json
{
  "SaltBase64": "YBXdT1v4EqAtrqGPg+QWEQ==",
  "IVBase64": "EY0VbkqPKMc5PsjYMz+4dA==",
  "CipherTextBase64": "6TAPR306pI5hUZI++7HMCNSe..."
}
