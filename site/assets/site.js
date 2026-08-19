const navigation = document.querySelector('#site-nav');
const navigationToggle = document.querySelector('.nav-toggle');
const video = document.querySelector('#intro-video');

navigationToggle?.addEventListener('click', () => {
    const open = navigation?.classList.toggle('open') ?? false;
    navigationToggle.setAttribute('aria-expanded', String(open));
});

navigation?.querySelectorAll('a').forEach((link) => {
    link.addEventListener('click', () => {
        navigation.classList.remove('open');
        navigationToggle?.setAttribute('aria-expanded', 'false');
    });
});

document.querySelector('[data-play-video]')?.addEventListener('click', () => {
    video?.scrollIntoView({ behavior: 'smooth', block: 'center' });
    window.setTimeout(() => video?.play(), 500);
});

const revealObserver = new IntersectionObserver((entries) => {
    entries.forEach((entry) => {
        if (entry.isIntersecting) {
            entry.target.classList.add('visible');
            revealObserver.unobserve(entry.target);
        }
    });
}, { threshold: 0.12 });

document.querySelectorAll('.reveal').forEach((element) => revealObserver.observe(element));
