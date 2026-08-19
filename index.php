<?php
declare(strict_types=1);

/*
 * Edit this small block to update the site. The video can be a local MP4 path
 * (for example: assets/story.mp4) or an embeddable YouTube/Vimeo URL.
 */
$site = [
    'name' => 'Dowe Lancaster',
    'eyebrow' => 'A story worth discovering',
    'headline' => 'Character, craft, and a life in motion.',
    'intro' => 'Discover the person, the work, and the moments that shaped the journey. This page brings the story together in one considered, cinematic experience.',
    'about' => 'Dowe Lancaster represents a story built through curiosity, purpose, and a commitment to meaningful work. Explore the highlights below, then watch the featured film for a closer look.',
    'location' => 'United States',
    'focus' => 'Story · Work · Legacy',
    'video' => '',
    'video_poster' => '',
    'contact_email' => '',
];

$video = trim((string) $site['video']);
$isEmbed = $video !== '' && (str_contains($video, 'youtube.com') || str_contains($video, 'youtu.be') || str_contains($video, 'vimeo.com'));

function e(string $value): string
{
    return htmlspecialchars($value, ENT_QUOTES, 'UTF-8');
}
?>
<!doctype html>
<html lang="en">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <meta name="description" content="<?= e($site['intro']) ?>">
    <title><?= e($site['name']) ?> — Official Story</title>
    <link rel="preconnect" href="https://fonts.googleapis.com">
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
    <link href="https://fonts.googleapis.com/css2?family=DM+Sans:wght@400;500;600&family=Playfair+Display:ital,wght@0,600;1,500&display=swap" rel="stylesheet">
    <style>
        :root{--ink:#17201d;--cream:#f4f0e7;--paper:#fffdf8;--rust:#a84d2f;--sage:#a7b1a1;--line:rgba(23,32,29,.16);--serif:"Playfair Display",Georgia,serif;--sans:"DM Sans",Arial,sans-serif}
        *{box-sizing:border-box}html{scroll-behavior:smooth}body{margin:0;background:var(--cream);color:var(--ink);font-family:var(--sans);font-size:16px;line-height:1.65}a{color:inherit}button{font:inherit}.shell{width:min(1180px,calc(100% - 40px));margin:auto}.topbar{position:absolute;z-index:5;top:0;left:0;width:100%;padding:28px 0}.nav{display:flex;align-items:center;justify-content:space-between;color:#fff}.brand{font-family:var(--serif);font-size:1.15rem;text-decoration:none;letter-spacing:.02em}.nav-links{display:flex;align-items:center;gap:28px}.nav-links a{text-decoration:none;font-size:.86rem;letter-spacing:.08em;text-transform:uppercase}.nav-cta{border:1px solid rgba(255,255,255,.7);padding:10px 16px;border-radius:99px}.hero{min-height:760px;height:100svh;position:relative;display:grid;place-items:end start;overflow:hidden;background:#35423d}.hero::before{content:"";position:absolute;inset:0;background:linear-gradient(110deg,rgba(13,24,20,.84) 0%,rgba(13,24,20,.48) 44%,rgba(13,24,20,.1) 75%),radial-gradient(circle at 78% 40%,#839488 0%,#40514a 43%,#24312c 100%);}.hero::after{content:"DL";position:absolute;right:-.05em;bottom:-.28em;color:rgba(255,255,255,.055);font:600 min(52vw,620px)/1 var(--serif)}.hero-content{position:relative;z-index:2;color:#fff;padding-bottom:10vh;max-width:820px}.eyebrow{margin:0 0 20px;color:#dbc9b9;font-size:.75rem;font-weight:600;text-transform:uppercase;letter-spacing:.2em}.hero h1{font:600 clamp(3.5rem,8vw,7.8rem)/.93 var(--serif);letter-spacing:-.045em;margin:0 0 28px;max-width:900px}.hero-copy{font-size:clamp(1rem,1.6vw,1.25rem);max-width:620px;color:rgba(255,255,255,.8);margin:0 0 34px}.button{display:inline-flex;align-items:center;gap:12px;border:0;border-radius:99px;padding:15px 22px;background:var(--rust);color:#fff;text-decoration:none;font-weight:600;cursor:pointer}.button svg{width:17px}.scroll-note{position:absolute;z-index:2;right:36px;bottom:38px;color:rgba(255,255,255,.65);font-size:.72rem;letter-spacing:.17em;text-transform:uppercase;writing-mode:vertical-rl}.intro-grid{display:grid;grid-template-columns:.8fr 1.35fr;gap:10vw;padding:130px 0 110px}.section-num{color:var(--rust);font-size:.75rem;letter-spacing:.15em;text-transform:uppercase}.intro-grid h2,.story h2,.film-copy h2{font:600 clamp(2.6rem,5vw,5rem)/1.03 var(--serif);letter-spacing:-.035em;margin:20px 0}.intro-grid .lead{font:500 clamp(1.5rem,2.6vw,2.35rem)/1.35 var(--serif);margin:0 0 34px}.muted{color:#59635f}.facts{display:grid;grid-template-columns:repeat(2,1fr);border-top:1px solid var(--line);margin-top:42px}.fact{padding:24px 0;border-bottom:1px solid var(--line)}.fact:nth-child(odd){padding-right:25px}.fact:nth-child(even){padding-left:25px;border-left:1px solid var(--line)}.fact small{display:block;color:#7a827f;text-transform:uppercase;letter-spacing:.12em;font-size:.68rem;margin-bottom:5px}.film{background:var(--ink);color:#fff;padding:110px 0}.film-grid{display:grid;grid-template-columns:1.25fr .75fr;align-items:center;gap:7vw}.video-frame{aspect-ratio:16/10;position:relative;overflow:hidden;border-radius:3px;background:linear-gradient(135deg,#8b8f81,#2a3933);box-shadow:0 35px 80px rgba(0,0,0,.28)}.video-frame iframe,.video-frame video{width:100%;height:100%;border:0;object-fit:cover}.video-empty{position:absolute;inset:0;display:grid;place-items:center;text-align:center;padding:30px;background:linear-gradient(140deg,rgba(174,151,119,.4),rgba(23,32,29,.4))}.play{width:82px;height:82px;border-radius:50%;display:grid;place-items:center;background:var(--paper);color:var(--ink);margin:auto auto 18px;box-shadow:0 8px 30px rgba(0,0,0,.25)}.play svg{width:25px;margin-left:4px}.video-empty p{margin:0;color:rgba(255,255,255,.75);font-size:.85rem}.film-copy .eyebrow{color:#c88c76}.film-copy p{color:rgba(255,255,255,.65)}.story{padding:130px 0}.story-head{display:flex;justify-content:space-between;gap:40px;align-items:end;margin-bottom:60px}.story-head p{max-width:420px;margin:0}.cards{display:grid;grid-template-columns:repeat(3,1fr);gap:20px}.card{min-height:360px;padding:32px;display:flex;flex-direction:column;justify-content:space-between;background:var(--paper);border:1px solid var(--line);transition:transform .25s ease,box-shadow .25s ease}.card:hover{transform:translateY(-6px);box-shadow:0 24px 60px rgba(23,32,29,.09)}.card-num{font:italic 500 2rem var(--serif);color:var(--rust)}.card h3{font:600 2rem/1.1 var(--serif);margin:0 0 14px}.card p{color:#68716d;margin:0}.quote{padding:120px 0;background:var(--sage);text-align:center}.quote blockquote{font:500 clamp(2.4rem,5vw,5.4rem)/1.05 var(--serif);letter-spacing:-.035em;max-width:960px;margin:0 auto}.quote span{display:block;margin-top:30px;font-size:.72rem;letter-spacing:.18em;text-transform:uppercase}footer{background:var(--paper);padding:70px 0 35px}.footer-main{display:flex;align-items:end;justify-content:space-between;gap:40px;padding-bottom:65px}.footer-main h2{font:600 clamp(2.5rem,5vw,5rem)/1 var(--serif);margin:0}.footer-main p{max-width:420px;color:#69716e}.footer-bottom{border-top:1px solid var(--line);padding-top:25px;display:flex;justify-content:space-between;color:#777f7c;font-size:.78rem}.reveal{opacity:0;transform:translateY(24px);transition:opacity .7s ease,transform .7s ease}.reveal.visible{opacity:1;transform:none}@media(max-width:800px){.shell{width:min(100% - 28px,1180px)}.nav-links a:not(.nav-cta){display:none}.hero{min-height:700px}.hero-content{padding-bottom:13vh}.scroll-note{display:none}.intro-grid,.film-grid{grid-template-columns:1fr;gap:45px;padding-top:90px;padding-bottom:90px}.film{padding:80px 0}.film-grid{padding:0}.film-copy{order:-1}.story{padding:90px 0}.story-head{display:block}.cards{grid-template-columns:1fr}.card{min-height:280px}.footer-main,.footer-bottom{display:block}.footer-bottom span{display:block;margin-top:8px}}@media(prefers-reduced-motion:reduce){html{scroll-behavior:auto}.reveal{opacity:1;transform:none;transition:none}}
    </style>
</head>
<body>
<header class="topbar"><nav class="nav shell" aria-label="Main navigation"><a class="brand" href="#top"><?= e($site['name']) ?></a><div class="nav-links"><a href="#story">The story</a><a href="#film">Film</a><a class="nav-cta" href="#details">Explore details</a></div></nav></header>
<main>
    <section class="hero" id="top">
        <div class="hero-content shell"><p class="eyebrow"><?= e($site['eyebrow']) ?></p><h1><?= e($site['headline']) ?></h1><p class="hero-copy"><?= e($site['intro']) ?></p><a class="button" href="#film"><svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true"><path d="M8 5v14l11-7z"/></svg> Watch the story</a></div><span class="scroll-note">Scroll to discover</span>
    </section>
    <section class="shell intro-grid reveal" id="details"><div><span class="section-num">01 — Introduction</span></div><div><p class="lead"><?= e($site['about']) ?></p><p class="muted">Designed as a living profile, this space can grow with new milestones, photographs, interviews, and films.</p><div class="facts"><div class="fact"><small>Based in</small><?= e($site['location']) ?></div><div class="fact"><small>Focus</small><?= e($site['focus']) ?></div><div class="fact"><small>Profile</small>Personal story</div><div class="fact"><small>Collection</small>Selected highlights</div></div></div></section>
    <section class="film" id="film"><div class="shell film-grid reveal"><div class="video-frame">
        <?php if ($video !== '' && $isEmbed): ?><iframe src="<?= e($video) ?>" title="<?= e($site['name']) ?> featured film" loading="lazy" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>
        <?php elseif ($video !== ''): ?><video controls preload="metadata" <?= $site['video_poster'] ? 'poster="'.e($site['video_poster']).'"' : '' ?>><source src="<?= e($video) ?>" type="video/mp4">Your browser does not support HTML video.</video>
        <?php else: ?><div class="video-empty"><div><span class="play"><svg viewBox="0 0 24 24" fill="currentColor"><path d="M8 5v14l11-7z"/></svg></span><strong>Featured film</strong><p>Add a video URL in the configuration at the top of index.php</p></div></div><?php endif; ?>
    </div><div class="film-copy"><p class="eyebrow">Featured film</p><h2>See the story unfold.</h2><p>A short visual portrait offers a more personal way into the journey—the places, people, and ideas behind the work.</p></div></div></section>
    <section class="story shell" id="story"><div class="story-head reveal"><div><span class="section-num">02 — Highlights</span><h2>Built one chapter<br>at a time.</h2></div><p class="muted">A concise view of the qualities and moments that define the wider story.</p></div><div class="cards"><article class="card reveal"><span class="card-num">I.</span><div><h3>Origins</h3><p>The early influences, formative places, and experiences that set everything in motion.</p></div></article><article class="card reveal"><span class="card-num">II.</span><div><h3>The work</h3><p>Projects shaped by discipline, imagination, and the desire to leave things better than they were found.</p></div></article><article class="card reveal"><span class="card-num">III.</span><div><h3>What’s next</h3><p>A forward-looking chapter grounded in purpose, new ideas, and enduring relationships.</p></div></article></div></section>
    <section class="quote"><div class="shell reveal"><blockquote>“The best stories aren’t simply told. They’re lived.”</blockquote><span>— The guiding idea</span></div></section>
</main>
<footer><div class="shell"><div class="footer-main"><h2><?= e($site['name']) ?></h2><div><p>A considered home for the story, work, and moments that matter.</p><?php if ($site['contact_email']): ?><a class="button" href="mailto:<?= e($site['contact_email']) ?>">Get in touch</a><?php endif; ?></div></div><div class="footer-bottom"><span>© <?= date('Y') ?> <?= e($site['name']) ?></span><span>Made with care</span></div></div></footer>
<script>
const reveal=()=>document.querySelectorAll('.reveal').forEach(el=>{if(el.getBoundingClientRect().top<innerHeight*.9)el.classList.add('visible')});
addEventListener('scroll',reveal,{passive:true});addEventListener('load',reveal);
</script>
</body>
</html>
