#!/usr/bin/env python3
"""Collects packet counts per connection state and direction from the official
Minecraft Wiki, and writes tests/McProtocol.Tests/wiki-packet-counts.json.

minecraft.wiki sits behind Cloudflare. Requests MUST carry a descriptive User-Agent
(no UA gives 403, a generic client UA gets tarpitted), and they MUST be serial:
concurrent clients share one per-IP budget and trip a block that takes ~90 s to clear.
So this is one process at ~1 req/s. Responses are cached on disk, so a re-run costs
no requests at all.

Versions are revisions of one page, 'Java Edition protocol/Packets'. Two page
structures exist: up to protocol 772 packets are level-4 headings; from 773 the page
adds a 'List of packets' section and hoists shared packets into level-2 sections, so
counts must come from the template table instead. handshaking is always counted from
headings, which is what keeps Legacy Server List Ping in the number.

Usage: python3 scripts/wiki-packet-counts.py
"""
import json, os, re, time, urllib.parse, urllib.request

API = "https://minecraft.wiki/api.php"
UA  = "McProtoNet-protocol-audit/1.0 (github.com/Titlehhhh/minecraft-protocol-fs)"
BASE = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "tests", "McProtocol.Tests", ".wiki-cache")
CACHE = os.path.join(BASE, "httpcache"); os.makedirs(CACHE, exist_ok=True)
REPO  = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")
OUT   = os.path.join(REPO, "tests", "McProtocol.Tests", "wiki-packet-counts.json")

STATES = ["handshaking","status","login","configuration","play"]
DIRS   = ["clientbound","serverbound"]

# protocol -> (mcVersion, oldid or None for the live page)
VERSIONS = [
 (753,"1.16.3",2772553),        (754,"1.16.4-1.16.5",2772660), (755,"1.17",2772685),
 (756,"1.17.1",2772702),        (757,"1.18-1.18.1",2772764),   (758,"1.18.2",2772783),
 (759,"1.19",2772902),          (760,"1.19.2",2772944),        (761,"1.19.3",2773015),
 (762,"1.19.4",2773029),        (763,"1.20-1.20.1",2773082),   (764,"1.20.2",2773142),
 (765,"1.20.3-1.20.4",2773281), (766,"1.20.5-1.20.6",2773283), (767,"1.21-1.21.1",2789623),
 (769,"1.21.4",2938097),        (770,"1.21.5",2992295),        (771,"1.21.6",3024144),
 (772,"1.21.7-1.21.8",3258009), (773,"1.21.9-1.21.10",3657983),(776,"26.2",None),
]
DELAY = 1.2
_last = [0.0]

def get(**kw):
    kw.setdefault("format","json"); kw.setdefault("formatversion","2")
    qs  = urllib.parse.urlencode(kw)
    key = os.path.join(CACHE, re.sub(r'[^A-Za-z0-9]+','_',qs)[:180] + ".json")
    if os.path.exists(key):
        return json.load(open(key,encoding="utf-8"))
    wait = DELAY - (time.time() - _last[0])
    if wait > 0: time.sleep(wait)
    req = urllib.request.Request(API+"?"+qs, headers={"User-Agent":UA})
    for attempt in range(4):
        try:
            with urllib.request.urlopen(req, timeout=60) as r:
                ct = r.headers.get("Content-Type","")
                body = r.read().decode("utf-8")
                _last[0] = time.time()
                if "json" not in ct:
                    raise RuntimeError(f"non-JSON response ({ct}) — likely a Cloudflare block")
                json.dump(json.loads(body), open(key,"w",encoding="utf-8"))
                return json.loads(body)
        except Exception as e:
            _last[0] = time.time()
            if attempt == 3: raise
            back = 95 * (attempt+1)
            print(f"    ! {type(e).__name__}: {e} -> backing off {back}s", flush=True)
            time.sleep(back)

def collect(pv, mcv, oldid):
    sel = {"oldid":oldid} if oldid else {"page":"Java Edition protocol/Packets"}
    p  = get(action="parse", prop="sections|revid", **sel)["parse"]
    w0 = get(action="parse", prop="wikitext", section=0, **sel)["parse"]["wikitext"]
    m  = re.search(r'([0-9][\w.]*),\s*protocol\s+(\d+)', w0)
    banner_pv = int(m.group(2)) if m else None
    banner_v  = m.group(1) if m else None
    if banner_pv != pv:
        return {"protocol":pv,"error":f"banner mismatch: page says protocol {banner_pv} ({banner_v})"}

    own = [s for s in p["sections"] if str(s.get("index","")) and not str(s["index"]).startswith("T-")]
    maxlv = max(int(s["level"]) for s in own)

    # --- heading walk ---
    head, names_h = {}, {}
    state = direction = None
    for s in own:
        lv, line = int(s["level"]), s["line"].strip()
        line_clean = re.sub(r"<[^>]+>","",line).strip()
        if lv == 2:
            state = line_clean.lower(); direction = None
        elif lv == 3:
            direction = line_clean.lower() if line_clean.lower() in DIRS else None
        elif lv == 4 and state in STATES and direction:
            k = (state,direction)
            head[k] = head.get(k,0)+1
            names_h.setdefault(k,[]).append(line_clean)

    # --- new structure? ---
    lop = [s for s in own if s["line"].strip().lower() == "list of packets"]
    tmpl, names_t = {}, {}
    if lop:
        wt = get(action="parse", prop="wikitext", section=lop[0]["index"], **sel)["parse"]["wikitext"]
        cur = None
        for line in wt.split("\n"):
            t = line.strip()
            b = re.match(r'\{\{\s*packet list/begin\s*\|\s*([^|}]+)\|\s*([^|}]+)', t)
            if b:
                cur = (b.group(1).strip().lower(), b.group(2).strip().lower())
                tmpl.setdefault(cur,0); names_t.setdefault(cur,[]); continue
            e = re.match(r'\{\{\s*packet list\s*\|\s*([^|}]+)', t)
            if e and cur:
                tmpl[cur] += 1; names_t[cur].append(e.group(1).strip())

    src, names, method = (tmpl, names_t, "api-wikitext") if lop else (head, names_h, "api-sections")

    counts, out_names = {}, {}
    for st in STATES:
        if st == "handshaking":                       # always headings: keeps Legacy SLP
            counts[st] = {d: head.get((st,d),0) for d in DIRS}
            for d in DIRS: out_names[f"{st}.{d}"] = names_h.get((st,d),[])
        elif any(k[0] == st for k in src):
            counts[st] = {d: src.get((st,d),0) for d in DIRS}
            for d in DIRS: out_names[f"{st}.{d}"] = names.get((st,d),[])
        else:
            counts[st] = {d: None for d in DIRS}
            for d in DIRS: out_names[f"{st}.{d}"] = None

    rec = {"protocol":pv,"mcVersion":mcv,
           "source":{"revisionId":p.get("revid"),"method":method,"bannerVersion":banner_v,
                     "oldid":oldid,"structure":"new" if lop else "classic","maxHeadingLevel":maxlv},
           "counts":counts,
           "countsByHeadings":{s:{d:head.get((s,d)) for d in DIRS} for s in STATES} if lop else None,
           "names":out_names}
    return rec

if __name__ == "__main__":
    versions = []
    for pv, mcv, oldid in VERSIONS:
        r = collect(pv, mcv, oldid)
        if "error" in r:
            raise SystemExit(f"{pv} {mcv}: {r['error']}")
        c = r["counts"]
        f = lambda s, d: "-" if c[s][d] is None else c[s][d]
        print(f"{pv:>4} {mcv:<16} {r['source']['structure']:<7} "
              f"h {f('handshaking','clientbound')}/{f('handshaking','serverbound')}  "
              f"s {f('status','clientbound')}/{f('status','serverbound')}  "
              f"l {f('login','clientbound')}/{f('login','serverbound')}  "
              f"c {f('configuration','clientbound')}/{f('configuration','serverbound')}  "
              f"p {f('play','clientbound')}/{f('play','serverbound')}", flush=True)
        versions.append({"protocol": pv, "mcVersion": mcv, "oldid": oldid,
                         "structure": r["source"]["structure"], "method": r["source"]["method"],
                         "counts": c})

    doc = {
        "comment": "Packet counts per connection state and direction, read from the official "
                   "Minecraft Wiki page 'Java Edition protocol/Packets'. Each version is one "
                   "revision of that page (oldid); protocol 776 is the live page. Regenerate with "
                   "scripts/wiki-packet-counts.py. null means the state does not exist in that "
                   "protocol version - the configuration state arrives in 764. "
                   "handshaking.serverbound is 2 because it counts Legacy Server List Ping (0xFE) "
                   "as well as Handshake.",
        "wikiPage": "https://minecraft.wiki/w/Java_Edition_protocol/Packets",
        "undocumented": {
            "protocols": [735, 736, 751, 768, 774, 775],
            "reason": "The wiki page carries no full packet list for these releases. Its history "
                      "jumps 578 -> 753, 767 -> 769 and 773 -> 776, so they were never merged into "
                      "the stable page. They are left out of the count assertions on purpose."},
        "versions": versions,
    }
    json.dump(doc, open(OUT, "w", encoding="utf-8"), ensure_ascii=False, indent=2)
    print("\nwritten:", OUT)
