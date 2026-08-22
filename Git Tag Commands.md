# Git Tag Commands
> **Commands to add a new tag to the repo and push it**

```powershell
$currentVersion = 'v0.9.5.5'
git checkout main 
git pull
git tag -a $currentVersion -m "Dowe Lancaster v"$currentVersion
git push origin $currentVersion
git tag --list
git show $currentVersion
```
<div style="background-color: #010a1381; border: 1px solid #ffffff; border-top: none; padding: 8px 12px; font-size: 12px; color: #ffffff; font-family: sans-serif; border-bottom-left-radius: 4px; border-bottom-right-radius: 4px; margin-top: -16px;">
  ⚠️ Use code with caution.
</div>

### Push Commands with Message
```powershell
$version = 'v0.9.5.5'
$comment = "chore: update application version to 0.9.5.5 in installer script"
git add .
git commit -m $comment
git pull --rebase origin main
git push
git status
```
<div style="background-color: #010a1381; border: 1px solid #ffffff; border-top: none; padding: 8px 12px; font-size: 12px; color: #ffffff; font-family: sans-serif; border-bottom-left-radius: 4px; border-bottom-right-radius: 4px; margin-top: -16px;">
  ⚠️ Use code with caution.
</div>

> No Git Checkout, Not git diff or show
---
```powershell
$currentVersion = 'v0.9.5.5'
git pull
git tag -a $currentVersion -m "Dowe Lancaster v"$currentVersion
git push origin $currentVersion
git tag --list
git status
```
<div style="background-color: #010a1381; border: 1px solid #ffffff; border-top: none; padding: 8px 12px; font-size: 12px; color: #ffffff; font-family: sans-serif; border-bottom-left-radius: 4px; border-bottom-right-radius: 4px; margin-top: -16px;">
  ⚠️ Use code with caution.
</div>

>Show a regular commit message
- - -

```powershell
$commitMessage = "Retry release lookup when updating changelog"
git add . 
git commit -m $commitMessage
$branch = git branch --show-current
git pull --rebase origin $branch
git push origin $branch
```
# New-Service Commands
### Basic Command Syntax
```powershell
New-Service -Name "MyCustomService" -BinaryPathName "C:\Path\To\YourApp.exe"
```
<div style="background-color: #010a1381; border: 1px solid #ffffff; border-top: none; padding: 8px 12px; font-size: 12px; color: #ffffff; font-family: sans-serif; border-bottom-left-radius: 4px; border-bottom-right-radius: 4px; margin-top: -16px;">
  ⚠️ Use code with caution.
</div>

## Full Practical Example

> This comprehensive command defines the service name, display name, description, startup type, and binary path:

---

```powershell
New-Service -Name "MyCustomService" `
            -BinaryPathName "C:\Path\To\YourApp.exe" `
            -DisplayName "My Custom Background Service" `
            -Description "This service runs my custom background executable." `
            -StartupType Automatic
```
<div style="background-color: #010a1381; border: 1px solid #ffffff; border-top: none; padding: 8px 12px; font-size: 12px; color: #ffffff; font-family: sans-serif; border-bottom-left-radius: 4px; border-bottom-right-radius: 4px; margin-top: -16px;">
  ⚠️ Use code with caution.
</div>

# Add firewall port rule

> powershell add port to firewall

Open **PowerShell as Administrator**, then run:

```powershell
New-NetFirewallRule `
  -DisplayName "Allow TCP Port 8080" `
  -Direction Inbound `
  -Protocol TCP `
  -LocalPort 8080 `
  -Action Allow `
  -Profile Domain,Private
```

Replace `8080` with the port you need.

For UDP, change `-Protocol TCP` to `-Protocol UDP`.

To allow both TCP and UDP:

```powershell
$port = 8888

New-NetFirewallRule -DisplayName "Allow TCP Port $port" -Direction Inbound -Protocol TCP -LocalPort $port -Action Allow -Profile Domain,Private
New-NetFirewallRule -DisplayName "Allow UDP Port $port" -Direction Inbound -Protocol UDP -LocalPort $port -Action Allow -Profile Domain,Private
```

Verify the rules:

```powershell
Get-NetFirewallRule -DisplayName "Allow * Port 8888" |
    Get-NetFirewallPortFilter
```

Remove them later:

```powershell
Remove-NetFirewallRule -DisplayName "Allow TCP Port 8080"
Remove-NetFirewallRule -DisplayName "Allow UDP Port 8080"
```

If the computer must accept connections while using a **Public** network profile, add `Public` to `-Profile`. That is broader exposure, so only do it when necessary. Opening the firewall port permits traffic to reach the computer, but an application or service must also be running and listening on that port.
