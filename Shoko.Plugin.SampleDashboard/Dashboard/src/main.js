import './style.css';

const status = document.getElementById('status');

// The page is served from the same origin as the Shoko Web UI, so it can
// reuse the session the Web UI stored when the user signed in.
function getApiKey() {
  try {
    return JSON.parse(localStorage.getItem('apiSession') ?? 'null')?.apikey ?? null;
  } catch {
    return null;
  }
}

async function load() {
  const apiKey = getApiKey();
  if (!apiKey) {
    status.textContent = 'Sign in to the Shoko Web UI in this browser first.';
    return;
  }

  const response = await fetch('/api/plugin/SampleDashboard/Stats', { headers: { apikey: apiKey } });
  if (!response.ok) {
    status.textContent = `The server answered ${response.status}.`;
    return;
  }

  const stats = await response.json();
  document.getElementById('series-count').textContent = stats.SeriesCount;
  document.getElementById('video-count').textContent = stats.VideoCount;
  document.getElementById('stats').hidden = false;
  status.textContent = `Signed in as ${stats.UserName}.`;
}

load().catch((error) => {
  status.textContent = `Could not reach the server: ${error.message}`;
});
