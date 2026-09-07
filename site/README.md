# Dowe LanCaster website

This PHP site displays the latest published GitHub release, embeds a dedicated
35-second How It Works video, and links directly to the release installer and
portable ZIP.

Run it with `site` as the document root so all media and download paths resolve:

```powershell
php -S localhost:8080 -t site
```

Then open `http://localhost:8080/`.

The site reads GitHub's public latest-release endpoint when each page is loaded.
Publishing a GitHub release updates the displayed version and download links
without a manual site-version edit. If GitHub cannot be reached temporarily,
the page retains a safe fallback link to the releases page.
