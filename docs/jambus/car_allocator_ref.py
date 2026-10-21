"""Reference port of Jam Bus 37.0 (libMyGame.so, armeabi-v7a) sand-colour -> car allocation.

Each function names the native routine it mirrors. Read docs/jambus/level-logic.md first.
Run:  python3 docs/jambus/car_allocator_ref.py   (self-check over every level in LevelMap.json)
"""
import json, math, random, sys
from functools import cmp_to_key
from pathlib import Path

FRONT_SEGMENT_PERCENT = 10   # GameTuningParams::frontSegmentPercent (default in .data)
MID_SEGMENT_PERCENT = 30     # GameTuningParams::midSegmentPercent
EPS = 1e-6                   # double tie tolerance (literal in .rodata next to the comparators)


# ---------------------------------------------------------------- picture -> sand counts
def canonical_colors(palette):
    """_applyDynamicCarAllocation step 1: ids sharing one RGB collapse to the first (smallest) id."""
    first_by_rgb, remap = {}, {}
    for cid in sorted(palette):
        remap[cid] = first_by_rgb.setdefault(palette[cid].upper(), cid)
    return remap


def sand_counts(grid, tile, remap):
    """_applyDynamicCarAllocation step 2: grains per colour = pixels * tile^2 (tile = sands.sands)."""
    counts = {}
    for i, c in enumerate(grid):
        if c < 1:
            continue
        c = remap.get(c, c)
        grid[i] = c
        counts[c] = counts.get(c, 0) + tile * tile
    return counts


def front_mid_counts(grid, width, height, tile):
    """SandBusGameManager::_computeFrontMidSandCounts.
    Pixels are read bottom row first (y = height-1 .. 0), left to right; each pixel repeats `tile` times.
    front = first frontPct% of that sequence, mid = frontPct%..midPct%."""
    seq = []
    for y in range(height - 1, -1, -1):
        for x in range(width):
            c = grid[y * width + x]
            if c > 0:
                seq.extend([c] * max(tile, 1))
    n = len(seq)
    f_pct = max(min(FRONT_SEGMENT_PERCENT, 100), 1)
    m_pct = max(min(MID_SEGMENT_PERCENT, 100), f_pct + 1)
    f_end = f_pct * n // 100
    m_end = m_pct * n // 100
    front, mid = {}, {}
    for c in seq[:f_end]:
        front[c] = front.get(c, 0) + 1
    for c in seq[f_end:m_end]:
        mid[c] = mid.get(c, 0) + 1
    return front, mid


# ---------------------------------------------------------------- SandBusCarAllocator::allocate
def allocate(counts, vehicles, front, mid, forced=None, special_area=False, rng=None):
    """counts: {color: grains}; vehicles: [{'id','cap','step'}] (cap = bulletCount, step = realOutStep).
    Returns (ok, error, results) with results = [{'id','color','amount'}] sorted by id."""
    if not counts:
        return False, 'no sand colors', []
    if len(vehicles) < len(counts):
        return False, 'fewer vehicles than sand colors', []
    if any(v <= 0 for v in counts.values()):
        return False, 'invalid sand color count', []
    total_sand = sum(counts.values())
    if total_sand <= 0:
        return False, 'no sand', []
    if any(v['id'] < 1 for v in vehicles):
        return False, 'invalid vehicle boxId', []

    cap = lambda v: max(v['cap'], 1)
    vs = sorted(vehicles, key=lambda v: (-cap(v), v['id']))          # capacity desc, id asc
    total_cap = sum(cap(v) for v in vs)

    entries = [{'color': c, 'count': n, 'assigned': 0, 'ideal': total_cap * n / total_sand, 'veh': []}
               for c, n in counts.items()]
    entries.sort(key=lambda e: (-e['count'], e['color']))              # sand desc, colour asc

    # one vehicle per colour first (biggest car -> biggest colour)
    for i, e in enumerate(entries):
        e['veh'].append(vs[i]); e['assigned'] += cap(vs[i])
    # the rest go to the colour with the largest capacity deficit
    for v in vs[len(entries):]:
        best = entries[0]
        for e in entries[1:]:
            d_best, d_e = best['ideal'] - best['assigned'], e['ideal'] - e['assigned']
            if abs(d_best - d_e) <= EPS:
                if e['count'] > best['count'] or (e['count'] == best['count'] and e['color'] < best['color']):
                    best = e
            elif d_e > d_best:
                best = e
        best['veh'].append(v); best['assigned'] += cap(v)

    # split each colour's grains across its cars (largest remainder, every car >= 1)
    results = []
    for e in entries:
        if not e['veh'] or e['count'] <= 0:
            continue
        if len(e['veh']) == 1:
            results.append({'id': e['veh'][0]['id'], 'color': e['color'], 'amount': e['count']}); continue
        sum_cap = sum(cap(v) for v in e['veh'])
        shares = []
        for v in e['veh']:
            raw = e['count'] * cap(v) / sum_cap
            fl = math.floor(raw)
            shares.append({'id': v['id'], 'amount': max(fl, 1), 'frac': raw - fl, 'cap': cap(v)})
        given = sum(s['amount'] for s in shares)
        while given > e['count']:                                      # take back from the largest share
            s = max(shares, key=lambda s: (s['amount'], s['cap']))
            if s['amount'] < 2:
                break
            s['amount'] -= 1; given -= 1
        shares.sort(key=cmp_to_key(share_order))                       # frac desc, cap desc, id asc
        for k in range(e['count'] - given):
            shares[k % len(shares)]['amount'] += 1
        shares.sort(key=lambda s: s['id'])
        results += [{'id': s['id'], 'color': e['color'], 'amount': s['amount']} for s in shares]

    if special_area:
        # SpecialArea users: (color, amount) pairs shuffled across cars (mt19937 seeded from /dev/urandom ^ clock)
        rng = rng or random.Random()
        pairs = [(r['color'], r['amount']) for r in results]
        rng.shuffle(pairs)
        for r, (c, a) in zip(results, pairs):
            r['color'], r['amount'] = c, a
    else:
        reorder_by_exit_tier(results, vs, front, mid)

    if forced and forced[0] > 0 and forced[1] > 0:                     # level 1 only, see doc
        apply_forced_first(results, *forced)

    results.sort(key=lambda r: r['id'])
    return True, '', results


def share_order(a, b):
    if abs(a['frac'] - b['frac']) <= EPS:
        return (b['cap'] - a['cap']) or (a['id'] - b['id'])
    return -1 if a['frac'] > b['frac'] else 1


# ---------------------------------------------------------------- FUN_0130f6e8 / FUN_0131282c
def reorder_by_exit_tier(results, vehicles, front, mid):
    """Cars that can leave now (step 1) must carry the colours at the bottom 10% of the picture;
    cars one step deep (step 2) the colours at 10-30%. Colours are swapped in, never re-split."""
    step_of = {v['id']: (v['step'] if v['step'] > 0 else 999) for v in vehicles}
    cars = [{'id': r['id'], 'step': step_of.get(r['id'], 999), 'color': r['color'], 'amount': r['amount']}
            for r in results]
    t1 = [i for i, c in enumerate(cars) if c['step'] == 1]
    t2 = [i for i, c in enumerate(cars) if c['step'] == 2]
    rest = [i for i, c in enumerate(cars) if c['step'] not in (1, 2)]

    protected = {}
    for color, need in sorted(front.items(), key=lambda kv: (-kv[1], kv[0])):
        if need > 0:
            ensure_color_in_tier(cars, t1, t2 + rest, color, need, protected)
            protected[color] = need
    for color, need in sorted(mid.items(), key=lambda kv: (-kv[1], kv[0])):
        if need > 0:
            ensure_color_in_tier(cars, t2, rest + t1, color, need, protected)

    by_id = {c['id']: c for c in cars}
    for r in results:
        c = by_id[r['id']]
        r['color'], r['amount'] = c['color'], c['amount']


def ensure_color_in_tier(cars, tier, pool, color, need, protected):
    tier_sum = lambda col: sum(cars[i]['amount'] for i in tier if cars[i]['color'] == col)
    have = tier_sum(color)
    if have >= need:
        return
    donors = sorted((i for i in pool if cars[i]['color'] == color), key=lambda i: -cars[i]['amount'])
    for d in donors:
        if have >= need or not tier:
            break
        victim = -1
        for i in tier:
            c = cars[i]
            if c['color'] == color:
                continue
            p = protected.get(c['color'], 0)
            if p > 0 and tier_sum(c['color']) - c['amount'] < p:
                continue                                              # would break an earlier front colour
            if victim < 0 or c['amount'] < cars[victim]['amount']:
                victim = i
        if victim < 0:
            break
        a, b = cars[d], cars[victim]
        a['color'], b['color'] = b['color'], a['color']
        a['amount'], b['amount'] = b['amount'], a['amount']
        have = tier_sum(color)


def apply_forced_first(results, vehicle_id, color):
    """Tail of allocate(): the forced car swaps (color, amount) with the same-colour car whose amount is closest."""
    k = next((i for i, r in enumerate(results) if r['id'] == vehicle_id), -1)
    if k < 0 or results[k]['color'] == color:
        return
    best = -1
    for j, r in enumerate(results):
        if j != k and r['color'] == color:
            if best < 0 or abs(r['amount'] - results[k]['amount']) < abs(results[best]['amount'] - results[k]['amount']):
                best = j
    if best >= 0:
        a, b = results[k], results[best]
        a['color'], b['color'] = b['color'], a['color']
        a['amount'], b['amount'] = b['amount'], a['amount']


# ---------------------------------------------------------------- fallback when allocate() fails
def fallback_bind(box_colors, palette, grid):
    """_applyParkingBoxSandColorIds + _bindParkingBoxesToSandLevelColors (only when allocate fails)."""
    remap = canonical_colors(palette)
    out = [remap.get(c, c) if c in palette else c for c in box_colors]
    level_colors = sorted({c for c in grid if c >= 1})
    wrong = sorted((i for i, c in enumerate(out) if c not in level_colors), key=lambda i: out[i])
    for slot, i in enumerate(wrong[:len(level_colors)]):
        out[i] = level_colors[slot]
    return out


# ---------------------------------------------------------------- self-check over the exported data
def self_check(root):
    meta = json.loads((root / 'SandPic/lsMeta.json').read_text())
    level_map = json.loads((root / 'LevelMap.json').read_text())
    checked = failed = 0
    for profile in level_map['profiles']:
        g = profile['groups'][0]
        palette = {int(k): v for k, v in (meta['groups'].get(str(g), {}).get('colors') or meta['global']['colors']).items()}
        for row in profile['levels']:
            box = json.loads((root / row['box']).read_text())
            pic = next(iter(json.loads((root / row['picture']).read_text()).values()))
            s = pic['sands']
            grid = list(s['data'])
            counts = sand_counts(grid, s['sands'], canonical_colors(palette))
            front, mid = front_mid_counts(grid, s['width'], s['height'], s['sands'])
            vehicles = [{'id': i + 1, 'cap': b['bulletCount'], 'step': 1} for i, b in enumerate(box['boxInfos'])]
            ok, err, res = allocate(counts, vehicles, front, mid)
            checked += 1
            got = {}
            for r in res:
                got[r['color']] = got.get(r['color'], 0) + r['amount']
            if not ok or got != counts or any(r['amount'] < 1 for r in res) or len(res) != len(vehicles):
                failed += 1
                print('FAIL', g, row['level'], err or 'sum mismatch')
    print(f'checked={checked} failed={failed}')
    return failed == 0


if __name__ == '__main__':
    data = Path(sys.argv[1]) if len(sys.argv) > 1 else Path(__file__).resolve().parents[2] / 'Assets/_Dev/Data/Level'
    sys.exit(0 if self_check(data) else 1)
