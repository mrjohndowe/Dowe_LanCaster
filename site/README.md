# Dowe LanCaster website

This PHP site presents Dowe LanCaster 0.7.0, embeds the existing intro video,
and exposes release downloads when the matching files exist in `dist`.

Run it from the repository root so all media and download paths resolve:

```powershell
php -S localhost:8080
```

Then open `http://localhost:8080/site/`.

Before publishing, run `BUILD-RELEASE.cmd`. The Windows download button appears
automatically when `dist/DoweLanCaster-Windows-x64.zip` exists. The Roku receiver
download uses `dist/DoweLanCaster-Roku.zip`.
