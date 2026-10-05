(function () {
  // Theme toggle
  var root = document.documentElement, btn = document.getElementById('theme');
  try { var saved = localStorage.getItem('theme'); if (saved) root.setAttribute('data-theme', saved); } catch (e) {}
  function isDark() {
    var t = root.getAttribute('data-theme');
    return t ? t === 'dark' : window.matchMedia('(prefers-color-scheme: dark)').matches;
  }
  function paint() { btn.textContent = isDark() ? '☀' : '☾'; }
  btn.addEventListener('click', function () {
    var next = isDark() ? 'light' : 'dark';
    root.setAttribute('data-theme', next);
    try { localStorage.setItem('theme', next); } catch (e) {}
    paint();
  });
  paint();

  // Publications
  var TYPE_LABEL = { all: 'All', journal: 'Journal', conference: 'Conference', workshop: 'Workshop', chapter: 'Book chapter' };
  var list = document.getElementById('publist'), tabs = document.getElementById('tabs'),
      q = document.getElementById('q'), count = document.getElementById('count');
  var type = 'all';

  function esc(s) { return String(s).replace(/[&<>"]/g, function (c) { return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c]; }); }
  function authors(s) {
    return esc(s).replace(/(Ahmed S\.? ?Shatnawi|Ahmad S\.? Shatnawi|Shatnawi, Ahmed S\.|Shatnawi, A\. S\.|Shatnawi, A\.|Ahmed Shatnawi)/g, '<b>$1</b>');
  }

  Object.keys(TYPE_LABEL).forEach(function (k) {
    var n = k === 'all' ? PUBS.length : PUBS.filter(function (p) { return p[6] === k; }).length;
    var b = document.createElement('button');
    b.textContent = TYPE_LABEL[k] + ' (' + n + ')';
    b.setAttribute('aria-pressed', k === 'all');
    b.addEventListener('click', function () {
      type = k;
      Array.prototype.forEach.call(tabs.children, function (c) { c.setAttribute('aria-pressed', c === b); });
      render();
    });
    tabs.appendChild(b);
  });

  function render() {
    var term = q.value.trim().toLowerCase();
    var rows = PUBS.filter(function (p) {
      return (type === 'all' || p[6] === type) &&
        (!term || (p[1] + ' ' + p[2] + ' ' + p[3] + ' ' + p[4]).toLowerCase().indexOf(term) !== -1);
    });
    count.textContent = rows.length + ' of ' + PUBS.length + ' publications';
    list.innerHTML = rows.map(function (p) {
      var doi = p[5] ? ' <a href="https://doi.org/' + esc(p[5]) + '" target="_blank" rel="noopener">doi:' + esc(p[5]) + '</a>' : '';
      return '<div class="pub"><div class="n">' + p[0] + '.</div><div>' +
        '<div>' + authors(p[1]) + '</div>' +
        '<div class="t">' + esc(p[2]) + '</div>' +
        '<div><span class="v">' + esc(p[3]) + '</span> <span class="y">(' + p[4] + ')</span>' + doi + '</div></div></div>';
    }).join('') || '<p class="muted">No matches.</p>';
  }
  q.addEventListener('input', render);
  render();
})();
