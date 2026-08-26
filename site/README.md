# Dowe LanCaster website

This PHP site displays the latest published GitHub release, embeds the existing
intro video, and links directly to the release installer and portable ZIP.

Run it from the repository root so all media and download paths resolve:

```powershell
php -S localhost:8080
```

Then open `http://localhost:8080/site/`.

The site reads GitHub's public latest-release endpoint when each page is loaded.
Publishing a GitHub release updates the displayed version and download links
without a manual site-version edit. If GitHub cannot be reached temporarily,
the page retains a safe fallback link to the releases page.
