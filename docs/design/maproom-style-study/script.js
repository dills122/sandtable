// Synthetic presentation state only. No campaign rules, transport or persistence.
const fixtureUnits = [
  { id: 'fox', name: 'Fox group', short: 'FOX', kind: 'Infantry', symbol: '×', position: 'K–7', point: [54, 48] },
  { id: 'harrier', name: 'Harrier group', short: 'HAR', kind: 'Armour', symbol: '○', position: 'G–5', point: [27, 37] },
  { id: 'kestrel', name: 'Kestrel group', short: 'KES', kind: 'Infantry', symbol: '×', position: 'E–8', point: [21, 68] },
  { id: 'osprey', name: 'Osprey group', short: 'OSP', kind: 'Recon', symbol: '/', position: 'M–9', point: [70, 70] },
  { id: 'merlin', name: 'Merlin group', short: 'MER', kind: 'Infantry', symbol: '×', position: 'H–8', point: [39, 61] },
  { id: 'heron', name: 'Heron group', short: 'HER', kind: 'Armour', symbol: '○', position: 'L–4', point: [67, 31] },
  { id: 'swift', name: 'Swift group', short: 'SWI', kind: 'Recon', symbol: '/', position: 'H–3', point: [40, 24] },
  { id: 'tern', name: 'Tern group', short: 'TER', kind: 'Infantry', symbol: '×', position: 'N–7', point: [84, 55] },
];
const destinations = [
  { id: 'crossing', name: 'East crossing', position: 'M–6', point: [70, 47] },
  { id: 'station', name: 'Coast station', position: 'N–4', point: [86, 27] },
  { id: 'ridge', name: 'Ridge pass', position: 'J–9', point: [52, 73] },
];
const copy = {
  draft: ['Draft order', 'No movement applied. Review before issuing.'],
  review: ['Ready to issue', 'Check formation, origin and destination before issuing this example.'],
  pending: ['Awaiting result', 'The example is pending. Position and ledger stay unchanged until acceptance.'],
  accepted: ['Accepted example', 'Position and War Diary now reflect the simulated result. Nothing was saved.'],
  stale: ['Order out of date', 'The example view changed. Refresh orders, then review again.'],
  rejected: ['Order rejected', 'This example destination is unavailable. Refresh orders to make a new draft.'],
  disconnected: ['Outcome unknown', 'The example reply was lost. Reconnect to reconcile it before issuing again.'],
  unsupported: ['Rules-lab boundary', 'The public rules path stops at Combat entry. Further adjudication is unavailable.'],
};
const labels = { draft: 'Draft', review: 'In review', pending: 'Pending', accepted: 'Accepted example', stale: 'Needs refresh', rejected: 'Rejected', disconnected: 'Outcome unknown' };
const get = id => document.getElementById(id);
const node = (tag, text, className) => {
  const element = document.createElement(tag);
  if (text !== undefined) element.textContent = text;
  if (className) element.className = className;
  return element;
};
let units, orders, diary, selected, pending, timer;
let connected = true;
let phase = 'movement';
let generation = 0;
let camera = { zoom: 1, x: 50, y: 50 };

function announce(message) { get('announcement').textContent = message; }
function currentUnit() { return units.find(unit => unit.id === selected); }
function newDraft(unit) { return { state: 'draft', destination: destinations.find(place => place.position !== unit.position).id }; }

function initializeFixture() {
  clearTimeout(timer);
  generation += 1;
  units = structuredClone(fixtureUnits);
  orders = new Map(units.map(unit => [unit.id, newDraft(unit)]));
  diary = units.map(unit => ({ unitId: unit.id, text: 'Fixture view loaded at ' + unit.position + '. No order issued.', title: 'Initial view' }));
  selected = 'fox';
  pending = null;
  connected = true;
  phase = 'movement';
  camera = { zoom: 1, x: 50, y: 50 };
  get('phase-example').value = phase;
  get('reply-example').value = 'accepted';
  get('ledger-search').value = '';
  buildFormationControls();
  render();
}

function buildFormationControls() {
  get('formation-list').replaceChildren();
  get('map-counters').replaceChildren();
  get('ledger-body').replaceChildren();
  for (const unit of units) {
    const roster = node('button', undefined, 'formation');
    roster.dataset.unit = unit.id;
    const symbol = node('span', unit.symbol, 'counter-mini');
    symbol.setAttribute('aria-hidden', 'true');
    const description = node('span');
    description.append(node('strong', unit.name), node('small', unit.position));
    roster.append(symbol, description);
    roster.addEventListener('click', () => selectUnit(unit.id));
    get('formation-list').append(roster);
    const counter = node('button', undefined, 'map-counter');
    counter.dataset.unit = unit.id;
    const mark = node('span', unit.symbol, 'counter-symbol');
    mark.setAttribute('aria-hidden', 'true');
    counter.append(mark, node('span', unit.short, 'counter-name'));
    counter.addEventListener('click', () => selectUnit(unit.id));
    // A counter reached by keyboard must be brought back into the visible camera.
    counter.addEventListener('focus', () => { if (camera.zoom > 1) centerOn(unit); });
    get('map-counters').append(counter);
    const row = node('tr');
    row.setAttribute('role', 'row');
    row.dataset.unit = unit.id;
    const nameCell = node('td');
    const select = node('button', unit.name);
    select.dataset.unit = unit.id;
    select.addEventListener('click', () => selectUnit(unit.id));
    nameCell.append(select);
    row.append(nameCell, node('td', unit.kind), node('td', unit.position), node('td'));
    [...row.cells].forEach((cell, index) => {
      cell.setAttribute('role', 'cell');
      const value = node('span', undefined, 'cell-value');
      value.append(...cell.childNodes);
      const label = node('span', ['Formation', 'Kind', 'Position', 'Order state'][index], 'cell-label');
      label.setAttribute('aria-hidden', 'true');
      cell.replaceChildren(label, value);
    });
    get('ledger-body').append(row);
  }
}

function selectUnit(id) {
  selected = id;
  render();
  announce(currentUnit().name + ' selected at ' + currentUnit().position + '.');
}

function renderCamera() {
  const bound = 50 * (camera.zoom - 1) / camera.zoom;
  camera.x = Math.max(50 - bound, Math.min(50 + bound, camera.x));
  camera.y = Math.max(50 - bound, Math.min(50 + bound, camera.y));
  const plane = get('map-plane');
  plane.style.setProperty('--camera-zoom', camera.zoom);
  plane.style.setProperty('--camera-x', -(camera.x - 50) * camera.zoom + '%');
  plane.style.setProperty('--camera-y', -(camera.y - 50) * camera.zoom + '%');
  get('zoom-level').textContent = Math.round(camera.zoom * 100) + '%';
  get('zoom-out').disabled = camera.zoom <= 1;
  get('zoom-in').disabled = camera.zoom >= 2;
  get('fit-map').disabled = camera.zoom === 1;
}

function centerOn(unit) {
  [camera.x, camera.y] = unit.point;
  renderCamera();
}
function locateUnit(id) {
  selectUnit(id);
  camera.zoom = Math.max(1.5, camera.zoom);
  centerOn(currentUnit());
  get('theater-title').focus();
  get('theater-title').scrollIntoView({ block: 'nearest' });
  announce(currentUnit().name + ' located on the map.');
}

function renderLedger() {
  const query = get('ledger-search').value.toLocaleLowerCase().trim();
  let visible = 0;
  for (const row of get('ledger-body').rows) {
    const unit = units.find(item => item.id === row.dataset.unit);
    row.hidden = !(unit.name + ' ' + unit.kind + ' ' + unit.position).toLocaleLowerCase().includes(query);
    if (!row.hidden) visible += 1;
    row.dataset.selected = String(unit.id === selected);
    row.querySelector('button').setAttribute('aria-pressed', String(unit.id === selected));
    row.cells[2].querySelector('.cell-value').textContent = unit.position;
    row.cells[3].querySelector('.cell-value').textContent = labels[orders.get(unit.id).state];
  }
  get('ledger-count').textContent = visible + ' / ' + units.length;
  get('ledger-empty').hidden = visible !== 0;
}
function sortLedger() {
  const field = get('ledger-sort').value;
  [...get('ledger-body').rows].sort((a, b) => {
    const first = units.find(unit => unit.id === a.dataset.unit)[field];
    const second = units.find(unit => unit.id === b.dataset.unit)[field];
    return first.localeCompare(second);
  }).forEach(row => get('ledger-body').append(row));
}
function renderDiary() {
  const records = diary.filter(entry => get('diary-scope').value === 'all' || entry.unitId === selected);
  get('diary-list').replaceChildren(...records.slice().reverse().map(entry => {
    const item = node('li');
    const unit = units.find(candidate => candidate.id === entry.unitId);
    const button = node('button', unit.name + ' · Locate');
    button.addEventListener('click', () => locateUnit(unit.id));
    item.append(node('strong', entry.title), node('p', entry.text), button);
    return item;
  }));
}

function render() {
  const unit = currentUnit();
  const order = orders.get(selected);
  const state = phase === 'unsupported' ? 'unsupported' : !connected ? 'disconnected' : order.state;
  document.querySelectorAll('button[data-unit]').forEach(button => {
    const item = units.find(candidate => candidate.id === button.dataset.unit);
    button.setAttribute('aria-pressed', String(item.id === selected));
    button.setAttribute('aria-label', 'Select ' + item.name + ', ' + item.kind + ', at ' + item.position);
    if (button.classList.contains('map-counter')) {
      button.style.left = item.point[0] + '%';
      button.style.top = item.point[1] + '%';
    }
    if (button.classList.contains('formation')) button.querySelector('small').textContent = item.position + ' · ' + item.kind;
  });
  get('roster-count').textContent = units.length;
  get('unit-title').textContent = unit.name;
  get('unit-kind').textContent = unit.kind;
  get('unit-position').textContent = unit.position;
  get('selection-summary').textContent = unit.name + ' · ' + unit.position;
  get('phase-label').textContent = phase === 'unsupported' ? 'Combat entry' : 'Movement';
  get('decision-label').textContent = phase === 'unsupported' ? 'Unsupported phase' : pending ? 'Awaiting ' + units.find(item => item.id === pending.unitId).short + ' result' : 'Awaiting your order';
  get('session-label').textContent = connected ? 'Local preview · No server' : 'Disconnected example · View stale';
  const reviewing = state === 'review';
  get('order-form').hidden = !['draft', 'pending'].includes(state);
  get('order-review').hidden = !reviewing;
  get('destination').replaceChildren(...destinations.filter(place => place.position !== unit.position).map(place => {
    const option = node('option', place.name + ' · ' + place.position);
    option.value = place.id;
    return option;
  }));
  get('destination').value = order.destination;
  const editable = state === 'draft' && !pending;
  get('destination').disabled = !editable;
  get('review-order').disabled = !editable;
  get('issue-order').disabled = !!pending || !connected || phase !== 'movement';
  const destination = destinations.find(place => place.id === order.destination);
  get('destination-detail').textContent = destination.name + ' · ' + destination.position;
  get('review-copy').textContent = 'Move ' + unit.name + ' from ' + unit.position + ' to ' + destination.name + ' (' + destination.position + ').';
  get('outcome').dataset.state = state;
  get('outcome').hidden = state === 'draft' || state === 'review';
  get('outcome-title').textContent = copy[state][0];
  get('outcome-copy').textContent = copy[state][1];
  const recovery = state === 'disconnected' ? 'Reconnect example' : ['stale', 'rejected'].includes(state) ? 'Refresh orders' : state === 'accepted' ? 'New draft' : '';
  get('recover-order').hidden = !recovery;
  get('recover-order').textContent = recovery;
  get('order-hint').textContent = pending && pending.unitId !== selected ? 'Another formation has an unresolved example. You can inspect; issuing waits.' : 'One order at a time. Turn resolution is not simulated.';
  for (const [id, active] of [['step-draft', state === 'draft'], ['step-review', state === 'review'], ['step-result', !['draft', 'review'].includes(state)]]) {
    if (active) get(id).setAttribute('aria-current', 'step'); else get(id).removeAttribute('aria-current');
  }
  const routeVisible = get('map-layer').value === 'orders' && ['draft', 'review', 'pending'].includes(state) && phase === 'movement';
  get('route-layer').toggleAttribute('hidden', !routeVisible);
  get('draft-route').setAttribute('d', 'M' + unit.point.join(' ') + 'L' + destination.point.join(' '));
  get('route-end').hidden = !routeVisible;
  get('route-end').style.left = destination.point[0] + '%';
  get('route-end').style.top = destination.point[1] + '%';
  get('map-context').textContent = routeVisible ? 'Dashed route: draft only · ' + unit.short : get('map-layer').value === 'terrain' ? 'Terrain view · Draft route hidden' : 'Last confirmed example positions';
  get('reply-example').disabled = !!pending;
  get('phase-example').disabled = !!pending;
  renderLedger();
  renderDiary();
  renderCamera();
}

function finishExample(submission) {
  if (submission.generation !== generation || pending !== submission) return;
  const unit = units.find(item => item.id === submission.unitId);
  const order = orders.get(submission.unitId);
  order.state = submission.reply;
  if (submission.reply === 'accepted') {
    unit.position = submission.destination.position;
    unit.point = [...submission.destination.point];
    diary.push({ unitId: unit.id, title: 'Movement accepted · example', text: unit.name + ' moved from ' + submission.origin + ' to ' + unit.position + '. Simulated result; not saved.' });
  }
  if (submission.reply === 'disconnected') connected = false;
  else pending = null;
  render();
  sortLedger();
  announce(unit.name + ': ' + copy[order.state][0] + '.');
}

get('order-form').addEventListener('submit', event => {
  event.preventDefault();
  if (pending || !connected || phase !== 'movement' || orders.get(selected).state !== 'draft') return;
  orders.get(selected).state = 'review';
  render();
  get('issue-order').focus();
});
get('destination').addEventListener('change', event => {
  orders.get(selected).destination = event.target.value;
  render();
});
get('edit-order').addEventListener('click', () => {
  orders.get(selected).state = 'draft';
  render();
  get('destination').focus();
});
get('issue-order').addEventListener('click', () => {
  if (pending || !connected || phase !== 'movement' || orders.get(selected).state !== 'review') return;
  const order = orders.get(selected);
  order.state = 'pending';
  pending = { unitId: selected, origin: currentUnit().position, destination: structuredClone(destinations.find(place => place.id === order.destination)), reply: get('reply-example').value, generation };
  const submission = pending;
  render();
  get('outcome').tabIndex = -1;
  get('outcome').focus();
  announce('Example issued. Awaiting result.');
  timer = setTimeout(() => finishExample(submission), 900);
});
get('recover-order').addEventListener('click', () => {
  if (!connected && pending) {
    connected = true;
    pending.reply = 'accepted';
    finishExample(pending);
  } else {
    orders.set(selected, newDraft(currentUnit()));
    render();
    announce('Orders refreshed. Review a new draft.');
  }
  if (!get('recover-order').hidden) get('recover-order').focus();
  else if (!get('destination').disabled) get('destination').focus();
});
get('ledger-search').addEventListener('input', renderLedger);
get('ledger-sort').addEventListener('change', sortLedger);
get('diary-scope').addEventListener('change', renderDiary);
get('map-layer').addEventListener('change', render);
get('zoom-in').addEventListener('click', () => { camera.zoom = Math.min(2, camera.zoom + 0.25); centerOn(currentUnit()); });
get('zoom-out').addEventListener('click', () => { camera.zoom = Math.max(1, camera.zoom - 0.25); renderCamera(); });
get('fit-map').addEventListener('click', () => { camera = { zoom: 1, x: 50, y: 50 }; renderCamera(); });
get('locate-unit').addEventListener('click', () => locateUnit(selected));
function toggleRoster(open) {
  get('roster').hidden = !open;
  document.querySelector('.workspace').classList.toggle('roster-closed', !open);
  get('toggle-roster').setAttribute('aria-expanded', String(open));
}
get('toggle-roster').addEventListener('click', () => toggleRoster(get('roster').hidden));
const narrow = matchMedia('(max-width: 68.75rem)');
narrow.addEventListener('change', event => toggleRoster(!event.matches));
toggleRoster(!narrow.matches);
get('open-ledger').addEventListener('click', () => { get('ledger-search').focus(); get('ledger-search').scrollIntoView({ block: 'nearest' }); });
get('expand-ledger').addEventListener('click', () => {
  const expanded = document.querySelector('.app-shell').classList.toggle('ledger-expanded');
  get('expand-ledger').setAttribute('aria-expanded', String(expanded));
  get('expand-ledger').textContent = expanded ? 'Compact' : 'Expand';
});
get('preview-settings').addEventListener('click', () => get('settings-dialog').showModal());
get('density').addEventListener('change', event => { document.body.dataset.density = event.target.value; });
get('inspector-width').addEventListener('input', event => { document.documentElement.style.setProperty('--inspector-width', Number(event.target.value) / 16 + 'rem'); });
get('phase-example').addEventListener('change', event => { phase = event.target.value; render(); announce(copy[phase === 'unsupported' ? 'unsupported' : 'draft'][0]); });
get('reset-fixture').addEventListener('click', () => { initializeFixture(); get('settings-dialog').close(); announce('Fixture reset. No campaign data was saved.'); });
initializeFixture();
sortLedger();
