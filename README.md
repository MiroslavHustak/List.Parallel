Storage settings - tam se podiovej na ruzne temporary files a staer update files atd.

---

**Manually delete temp files right now:**
```powershell
Remove-Item -Path "C:\Windows\Temp\*" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path "$env:LOCALAPPDATA\Temp\*" -Recurse -Force -ErrorAction SilentlyContinue
```

---

**Delete the failed update attempts that started this whole mess:**
```powershell
net stop wuauserv
Remove-Item -Path "C:\Windows\SoftwareDistribution.old" -Recurse -Force -ErrorAction SilentlyContinue
net start wuauserv
```

net stop wuauserv
Remove-Item -Path "C:\Windows\SoftwareDistribution\Download\*" -Recurse -Force -ErrorAction SilentlyContinue
net start wuauserv

If you don't use Hibernation mode (and just use Sleep), you can delete this completely and reclaim 8–16+ GB instantly. Run this in Administrator PowerShell:
powercfg.exe /hibernate off



The Windows Component Store saves older versions of system files so you can uninstall updates. Sometimes it gets bloated with redundant data.
Dism.exe /online /Cleanup-Image /StartComponentCleanup /ResetBase

VS
modern IDEs and development tools generate massive background caches just by being open:
C:\Users\User\AppData\Local\Microsoft\WebsiteCache
to ale u mne neni, bo vse uz mam na E

Audacity - tam moze byt hoden v ProgramData

. Compact Windows System Files (Reclaims 2–4 GB)
If you just need a temporary pocket of space so Windows Update can breathe, you can compress your entire Windows operating system files using a native feature called CompactOS. It takes 2 minutes and has zero impact on performance:powershellcompact.exe /CompactOS:always

---

**Check size of DISM log which can grow huge:**
```powershell
Get-Item "C:\Windows\Logs\DISM\dism.log" | Select-Object Length
```

---

**Most importantly — how big is SoftwareDistribution folder now:**
```powershell
(Get-ChildItem "C:\Windows\SoftwareDistribution" -Recurse -ErrorAction SilentlyContinue | Measure-Object -Property Length -Sum).Sum / 1GB
```

Run these and paste the results — we need to recover at least 5-6GB on C: before anything else can proceed.

Good findings:

- `C:\Windows\Temp` — only **28MB**, negligible
- `C:\Users\...\AppData\Local\Temp` — **1.6GB**, worth clearing

---

**Clear both now:**
```powershell
Remove-Item -Path "C:\Windows\Temp\*" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path "$env:LOCALAPPDATA\Temp\*" -Recurse -Force -ErrorAction SilentlyContinue
```

---

**Then check the big one — SoftwareDistribution:**
```powershell
(Get-ChildItem "C:\Windows\SoftwareDistribution" -Recurse -ErrorAction SilentlyContinue | Measure-Object -Property Length -Sum).Sum / 1GB
```

**And the old renamed one we created earlier:**
```powershell
(Get-ChildItem "C:\Windows\SoftwareDistribution.old" -Recurse -ErrorAction SilentlyContinue | Measure-Object -Property Length -Sum).Sum / 1GB
```

**And WinSxS:**
```powershell
(Get-ChildItem "C:\Windows\WinSxS" -Recurse -ErrorAction SilentlyContinue | Measure-Object -Property Length -Sum).Sum / 1GB
```

---

The `SoftwareDistribution.old` folder we renamed earlier is a prime candidate — it's the old update cache and is **completely safe to delete** since we already renamed it. That alone could recover several GB immediately.


Yes, you can **absolutely check and delete both** "Previous Windows installation" and "Delivery Optimization Files."

Delivery Optimization files are just temporary updates that your PC shares with other computers on your local network to speed up their updates. Deleting them is completely safe and won't harm your system at all. Clearing both will give you over 13 GB back immediately.

Regarding your Android folder and Visual Studio, here is exactly what you should do:

---

## 1. Can you use the Symlink trick?

**Yes, absolutely.** In fact, since you are using Visual Studio, a symlink is the cleanest option because it saves you from digging through Visual Studio's deep settings menus to reconfigure paths. Visual Studio will look at the old C drive path, and Windows will silently redirect it to your data disk.

## 2. Should you move it back?

**No, leave it on the data disk.** Moving it back will just choke your C drive again. Instead, let's use that Symlink trick right where it is.

Here is the exact command you need for your specific `Program Files (x86)` setup:

1. **Open Command Prompt as Admin:** Required for system folders.
Press the **Windows Key**, type **cmd**, right-click **Command Prompt**, and select **Run as administrator**.


2. **Check the C drive folder:** Crucial preparation step.
Go to `C:\Program Files (x86)` in your file explorer. If there is still an empty "Android" folder left behind from your move, **delete it**. The symlink command will fail if a folder with that name already exists on C.


3. **Run the Symlink Command:** Creates the virtual shortcut.
Copy and paste the following command into your black Command Prompt window, then press Enter:

```cmd
mklink /d "C:\Program Files (x86)\Android" "F:\Android"

```

*(Note: If your data disk letter is different than `D:`, or if you put it inside another folder like `D:\Data\Android`, change the second path to match exactly where the folder sits right now).*


Once you press Enter, you should see a message saying: `symbolic link created for...`.

If you open `C:\Program Files (x86)`, you will see the Android folder is back, but it will have a little shortcut arrow on it. Visual Studio will now work perfectly, thinking the files never left, while your 13 GB stays safely on your data disk.

C:\WINDOWS\system32>mklink /d "C:\Program Files (x86)\Android" "F:\Android"
symbolic link created for C:\Program Files (x86)\Android <<===>> F:\Android

c:\Users\User>mklink /d "C:\Users\User\.nuget" "F:\VisualStudioNuGet\.nuget"
symbolic link created for C:\Users\User\.nuget <<===>> F:\VisualStudioNuGet\.nuget



