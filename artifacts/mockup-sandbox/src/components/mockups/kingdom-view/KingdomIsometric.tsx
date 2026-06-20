import { useEffect, useRef, useState } from "react";

const CW = 1280;
const CH = 720;
const TW = 80;
const TH = 40;
const OX = 640;
const OY = 110;

function sx(gx: number, gy: number) { return (gx - gy) * (TW / 2) + OX; }
function sy(gx: number, gy: number) { return (gx + gy) * (TH / 2) + OY; }

function diamond(ctx: CanvasRenderingContext2D, x: number, y: number, w: number, h: number, fill: string, stroke?: string) {
  ctx.beginPath();
  ctx.moveTo(x, y);
  ctx.lineTo(x + w / 2, y + h / 2);
  ctx.lineTo(x, y + h);
  ctx.lineTo(x - w / 2, y + h / 2);
  ctx.closePath();
  ctx.fillStyle = fill;
  ctx.fill();
  if (stroke) { ctx.strokeStyle = stroke; ctx.lineWidth = 0.8; ctx.stroke(); }
}

function box(ctx: CanvasRenderingContext2D, bsx: number, bsy: number, gw: number, gh: number, bh: number, cl: string, cr: string, ct: string) {
  const bw = TW * gw; const bt = TH * gh;
  ctx.beginPath(); ctx.moveTo(bsx + bw / 2, bsy + bt / 2); ctx.lineTo(bsx, bsy + bt);
  ctx.lineTo(bsx, bsy + bt - bh); ctx.lineTo(bsx + bw / 2, bsy + bt / 2 - bh); ctx.closePath();
  ctx.fillStyle = cr; ctx.fill(); ctx.strokeStyle = "rgba(0,0,0,.25)"; ctx.lineWidth = .6; ctx.stroke();
  ctx.beginPath(); ctx.moveTo(bsx - bw / 2, bsy + bt / 2); ctx.lineTo(bsx, bsy + bt);
  ctx.lineTo(bsx, bsy + bt - bh); ctx.lineTo(bsx - bw / 2, bsy + bt / 2 - bh); ctx.closePath();
  ctx.fillStyle = cl; ctx.fill(); ctx.strokeStyle = "rgba(0,0,0,.25)"; ctx.lineWidth = .6; ctx.stroke();
  diamond(ctx, bsx, bsy - bh, bw, bt, ct, "rgba(0,0,0,.15)");
}

function peakedRoof(ctx: CanvasRenderingContext2D, bsx: number, bsy: number, gw: number, gh: number, bh: number, roofH: number, color: string, dark: string) {
  const bw = TW * gw; const bt = TH * gh;
  const apex = { x: bsx, y: bsy - bh - roofH };
  const tl = { x: bsx - bw / 2, y: bsy - bh + bt / 2 };
  const tr = { x: bsx + bw / 2, y: bsy - bh + bt / 2 };
  const bot = { x: bsx, y: bsy - bh + bt };
  ctx.beginPath(); ctx.moveTo(apex.x, apex.y); ctx.lineTo(tr.x, tr.y); ctx.lineTo(bot.x, bot.y); ctx.lineTo(tl.x, tl.y); ctx.closePath();
  ctx.fillStyle = color; ctx.fill(); ctx.strokeStyle = "rgba(0,0,0,.3)"; ctx.lineWidth = .7; ctx.stroke();
  ctx.beginPath(); ctx.moveTo(apex.x, apex.y); ctx.lineTo(tr.x, tr.y); ctx.lineTo(bot.x, bot.y); ctx.closePath();
  ctx.fillStyle = dark; ctx.fill();
}

function pagodaRoof(ctx: CanvasRenderingContext2D, bsx: number, bsy: number, bh: number, color: string, dark: string) {
  for (let tier = 0; tier < 3; tier++) {
    const scale = 1 - tier * 0.25;
    const tierH = bh * 0.18;
    const tierY = bsy - bh * 0.45 - tier * (tierH + 6);
    const tw2 = TW * scale; const th2 = TH * scale;
    peakedRoof(ctx, bsx, tierY, 1, 1, 0, tierH, color, dark);
    void tw2; void th2;
  }
}

function drawTree(ctx: CanvasRenderingContext2D, x: number, y: number) {
  ctx.beginPath(); ctx.moveTo(x, y - 28); ctx.lineTo(x + 12, y); ctx.lineTo(x - 12, y); ctx.closePath();
  ctx.fillStyle = "#3a7a28"; ctx.fill();
  ctx.beginPath(); ctx.moveTo(x, y - 38); ctx.lineTo(x + 9, y - 18); ctx.lineTo(x - 9, y - 18); ctx.closePath();
  ctx.fillStyle = "#4e9e35"; ctx.fill();
  ctx.fillStyle = "#5aAF40"; ctx.beginPath(); ctx.arc(x, y - 40, 5, 0, Math.PI * 2); ctx.fill();
  ctx.fillRect(x - 2, y - 1, 4, 8);
}

function drawWaterBackground(ctx: CanvasRenderingContext2D, t: number) {
  const grd = ctx.createLinearGradient(0, 0, 0, CH);
  grd.addColorStop(0, "#1a4a7a"); grd.addColorStop(0.5, "#2262A4"); grd.addColorStop(1, "#1a3d6a");
  ctx.fillStyle = grd; ctx.fillRect(0, 0, CW, CH);
  ctx.strokeStyle = "rgba(90,160,240,0.18)"; ctx.lineWidth = 1;
  for (let row = 0; row < 20; row++) {
    const yOff = (row * 36 + t * 0.3) % (CH + 20) - 10;
    ctx.beginPath();
    for (let xi = 0; xi < CW; xi += 8) {
      const wave = Math.sin((xi / 60) + t * 0.05 + row * 0.4) * 3;
      if (xi === 0) ctx.moveTo(xi, yOff + wave); else ctx.lineTo(xi, yOff + wave);
    }
    ctx.stroke();
  }
}

interface LabelPos { id: string; name: string; lx: number; ly: number; }

const BUILDINGS = [
  { id: "castle",   name: "Castle",           gx: 2, gy: 2, gw: 2, gh: 2, bh: 90,  cl: "#8D6E63", cr: "#5D4037", ct: "#9C7B6F", roofType: "pagoda" as const, rc: "#D32F2F", rd: "#B71C1C" },
  { id: "treasury", name: "Treasure House",   gx: 5, gy: 1, gw: 1, gh: 1, bh: 55,  cl: "#8D7520", cr: "#5D4C10", ct: "#A08530", roofType: "dome"   as const, rc: "#F9A825", rd: "#E65100" },
  { id: "academy",  name: "Academy",          gx: 1, gy: 4, gw: 1, gh: 1, bh: 62,  cl: "#1565C0", cr: "#0D47A1", ct: "#1976D2", roofType: "peak"   as const, rc: "#1E88E5", rd: "#0D47A1" },
  { id: "trading",  name: "Trading Post",     gx: 6, gy: 3, gw: 1, gh: 1, bh: 48,  cl: "#757575", cr: "#424242", ct: "#8A8A8A", roofType: "peak"   as const, rc: "#E53935", rd: "#B71C1C" },
  { id: "tower",    name: "Watch Tower",      gx: 7, gy: 2, gw: 1, gh: 1, bh: 95,  cl: "#9E9E9E", cr: "#616161", ct: "#B0B0B0", roofType: "peak"   as const, rc: "#78909C", rd: "#455A64" },
  { id: "storage",  name: "Storage",          gx: 5, gy: 4, gw: 1, gh: 1, bh: 42,  cl: "#8D6E63", cr: "#6D4C41", ct: "#9A7A6A", roofType: "flat"   as const, rc: "#BF360C", rd: "#7F0000" },
  { id: "alliance", name: "Hall of Alliance", gx: 3, gy: 5, gw: 1, gh: 1, bh: 58,  cl: "#2E7D32", cr: "#1B5E20", ct: "#388E3C", roofType: "peak"   as const, rc: "#43A047", rd: "#1B5E20" },
  { id: "hospital", name: "Hospital",         gx: 1, gy: 6, gw: 1, gh: 1, bh: 46,  cl: "#C62828", cr: "#7F0000", ct: "#D32F2F", roofType: "flat"   as const, rc: "#EF9A9A", rd: "#C62828" },
  { id: "wall",     name: "Wall",             gx: 4, gy: 7, gw: 1, gh: 1, bh: 30,  cl: "#8B7355", cr: "#6B5340", ct: "#9B8365", roofType: "flat"   as const, rc: "#A09070", rd: "#8B7355" },
];

const GROUND_COLOR     = "#C8A978";
const GROUND_DARK      = "#B89860";
const GROUND_HIGHLIGHT = "#D8B988";
const WALL_COLOR       = "#8B7355";
const WALL_DARK        = "#6B5340";
const WALL_TOP         = "#9B8365";

const TREE_POSITIONS = [
  [0.5, 0.5],[0.5,8.5],[8.5,0.5],[8.5,8.5],
  [-0.3,4],[-0.3,5],[9.3,3],[9.3,6],
  [4,-0.3],[5,-0.3],[3,9.3],[6,9.3],
];

export function KingdomIsometric() {
  const cvs = useRef<HTMLCanvasElement>(null);
  const raf = useRef<number>(0);
  const [labels, setLabels] = useState<LabelPos[]>([]);

  useEffect(() => {
    const canvas = cvs.current; if (!canvas) return;
    const ctx = canvas.getContext("2d")!;
    let t = 0;

    function render() {
      t++;
      drawWaterBackground(ctx, t);

      const occupied = new Set<string>();
      BUILDINGS.forEach(b => {
        for (let dx = 0; dx < b.gw; dx++) for (let dy = 0; dy < b.gh; dy++) occupied.add(`${b.gx+dx},${b.gy+dy}`);
      });

      const wallSet = new Set<string>();
      for (let i = 0; i <= 8; i++) { wallSet.add(`${i},0`); wallSet.add(`${i},8`); wallSet.add(`0,${i}`); wallSet.add(`8,${i}`); }

      for (let d = 0; d <= 16; d++) {
        for (let gxi = Math.max(0, d - 8); gxi <= Math.min(8, d); gxi++) {
          const gyi = d - gxi; if (gyi < 0 || gyi > 8) continue;
          const bsx = sx(gxi, gyi); const bsy = sy(gxi, gyi);
          const key = `${gxi},${gyi}`;
          const isWall = wallSet.has(key);
          const isOccupied = occupied.has(key);
          if (isWall) {
            diamond(ctx, bsx, bsy, TW, TH, WALL_COLOR, "rgba(0,0,0,.2)");
            const grd = ctx.createLinearGradient(bsx - TW/2, bsy + TH/2, bsx, bsy + TH);
            grd.addColorStop(0, WALL_DARK); grd.addColorStop(1, "#4a3520");
            ctx.beginPath(); ctx.moveTo(bsx - TW/2, bsy + TH/2); ctx.lineTo(bsx, bsy + TH); ctx.lineTo(bsx, bsy + TH + 20); ctx.lineTo(bsx - TW/2, bsy + TH/2 + 20); ctx.closePath();
            ctx.fillStyle = grd; ctx.fill();
            ctx.beginPath(); ctx.moveTo(bsx + TW/2, bsy + TH/2); ctx.lineTo(bsx, bsy + TH); ctx.lineTo(bsx, bsy + TH + 20); ctx.lineTo(bsx + TW/2, bsy + TH/2 + 20); ctx.closePath();
            ctx.fillStyle = WALL_DARK; ctx.fill();
          } else {
            const c = ((gxi + gyi) % 2 === 0) ? GROUND_COLOR : GROUND_DARK;
            diamond(ctx, bsx, bsy, TW, TH, c, "rgba(0,0,0,.12)");
            if ((gxi + gyi) % 5 === 0) { ctx.fillStyle = "rgba(255,255,255,.06)"; ctx.beginPath(); ctx.arc(bsx + 10, bsy + TH * .55, 3, 0, Math.PI * 2); ctx.fill(); }
          }
          void isOccupied;

          const bld = BUILDINGS.find(b => b.gx === gxi && b.gy === gyi);
          if (bld) {
            const bsx2 = sx(bld.gx + bld.gw / 2, bld.gy + bld.gh / 2);
            const bsy2 = sy(bld.gx + bld.gw / 2, bld.gy + bld.gh / 2);
            box(ctx, bsx2, bsy2, bld.gw, bld.gh, bld.bh, bld.cl, bld.cr, bld.ct);
            if (bld.roofType === "pagoda") {
              pagodaRoof(ctx, bsx2, bsy2, bld.bh, bld.rc, bld.rd);
            } else if (bld.roofType === "dome") {
              ctx.beginPath(); ctx.arc(bsx2, bsy2 - bld.bh - 10, 26, 0, Math.PI * 2);
              const grd = ctx.createRadialGradient(bsx2 - 5, bsy2 - bld.bh - 18, 2, bsx2, bsy2 - bld.bh - 10, 28);
              grd.addColorStop(0, "#FFE57F"); grd.addColorStop(1, bld.rc);
              ctx.fillStyle = grd; ctx.fill(); ctx.strokeStyle = bld.rd; ctx.lineWidth = 1.5; ctx.stroke();
            } else if (bld.roofType === "peak") {
              peakedRoof(ctx, bsx2, bsy2, bld.gw, bld.gh, bld.bh, 22, bld.rc, bld.rd);
            } else {
              diamond(ctx, bsx2, bsy2 - bld.bh, TW * bld.gw, TH * bld.gh, bld.rc, bld.rd);
            }
            if (bld.id === "tower") {
              ctx.fillStyle = "#37474F"; ctx.beginPath(); ctx.arc(bsx2, bsy2 - bld.bh - 30, 12, 0, Math.PI * 2); ctx.fill();
              ctx.fillStyle = "#FFD600"; ctx.beginPath(); ctx.arc(bsx2, bsy2 - bld.bh - 30, 4, 0, Math.PI * 2); ctx.fill();
            }
            if (bld.id === "academy") {
              ctx.strokeStyle = `rgba(100,180,255,${0.4 + 0.2 * Math.sin(t * 0.06)})`; ctx.lineWidth = 2;
              for (let r = 25; r < 55; r += 12) { ctx.beginPath(); ctx.arc(bsx2, bsy2 - bld.bh / 2, r, 0, Math.PI * 2); ctx.stroke(); }
            }
          }
        }
      }
      TREE_POSITIONS.forEach(([tx, ty]) => { drawTree(ctx, sx(tx, ty), sy(tx, ty) + 4); });

      const cornerPositions = [[0,0],[8,0],[0,8],[8,8]];
      cornerPositions.forEach(([cx, cy]) => {
        const csx = sx(cx, cy); const csy = sy(cx, cy);
        box(ctx, csx, csy, 1, 1, 35, WALL_COLOR, WALL_DARK, WALL_TOP);
        peakedRoof(ctx, csx, csy, 1, 1, 35, 18, "#A09070", "#7B6040");
      });

      if (t === 1) {
        const lp = BUILDINGS.map(b => ({
          id: b.id, name: b.name,
          lx: sx(b.gx + b.gw / 2, b.gy + b.gh / 2),
          ly: sy(b.gx + b.gw / 2, b.gy + b.gh / 2) - b.bh - (b.gw > 1 ? 20 : 6),
        }));
        setLabels(lp);
      }

      raf.current = requestAnimationFrame(render);
    }

    render();
    return () => cancelAnimationFrame(raf.current);
  }, []);

  const resources = [
    { icon: "🥇", name: "Gold", value: "1,248,500" },
    { icon: "🌾", name: "Food", value: "842,300" },
    { icon: "🪵", name: "Wood", value: "631,200" },
    { icon: "🪨", name: "Stone", value: "415,800" },
  ];

  return (
    <div className="relative overflow-hidden bg-[#1a3a6c]" style={{ width: CW, height: CH, fontFamily: "serif" }}>
      <canvas ref={cvs} width={CW} height={CH} style={{ display: "block" }} />

      {labels.map(l => (
        <div key={l.id} className="absolute pointer-events-none" style={{ left: l.lx, top: l.ly, transform: "translate(-50%,-100%)" }}>
          <div style={{ background: "rgba(10,15,30,0.88)", border: "1px solid rgba(200,180,100,0.5)", borderRadius: 20, padding: "3px 12px", whiteSpace: "nowrap", color: "#F5E9C8", fontSize: 12, fontWeight: 600, letterSpacing: 0.3, boxShadow: "0 2px 8px rgba(0,0,0,0.6)", backdropFilter: "blur(4px)" }}>
            {l.name}
          </div>
        </div>
      ))}

      <div style={{ position: "absolute", top: 0, left: 0, right: 0, background: "linear-gradient(180deg,rgba(0,0,0,.75) 0%,transparent 100%)", padding: "10px 18px", display: "flex", alignItems: "center", justifyContent: "space-between" }}>
        <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
          <div style={{ width: 36, height: 36, borderRadius: 8, background: "linear-gradient(135deg,#B8860B,#8B6914)", border: "2px solid #F0C040", display: "flex", alignItems: "center", justifyContent: "center", fontSize: 20 }}>🏰</div>
          <div>
            <div style={{ color: "#F5E9C8", fontSize: 15, fontWeight: 700, letterSpacing: 0.5 }}>Ironhold</div>
            <div style={{ color: "#A0906A", fontSize: 11 }}>Kingdom Lv.5</div>
          </div>
        </div>
        <div style={{ display: "flex", gap: 20 }}>
          {resources.map(r => (
            <div key={r.name} style={{ display: "flex", alignItems: "center", gap: 5, background: "rgba(0,0,0,.45)", borderRadius: 8, padding: "4px 10px", border: "1px solid rgba(200,160,60,.25)" }}>
              <span style={{ fontSize: 14 }}>{r.icon}</span>
              <div>
                <div style={{ color: "#F5E9C8", fontSize: 12, fontWeight: 600, lineHeight: 1 }}>{r.value}</div>
                <div style={{ color: "#A0906A", fontSize: 9 }}>{r.name}</div>
              </div>
            </div>
          ))}
        </div>
        <div style={{ display: "flex", gap: 8 }}>
          {["⚔️ Attack","🗺️ World","⚙️ Build"].map(btn => (
            <button key={btn} style={{ background: "linear-gradient(180deg,#8B6914,#5a4010)", color: "#F5E9C8", border: "1px solid #C0940A", borderRadius: 6, padding: "5px 12px", fontSize: 11, fontWeight: 600, cursor: "pointer" }}>{btn}</button>
          ))}
        </div>
      </div>

      <div style={{ position: "absolute", bottom: 12, left: "50%", transform: "translateX(-50%)", display: "flex", gap: 10 }}>
        {["🏛️ Buildings","⚔️ Troops","📜 Research","🛡️ Alliance"].map(tab => (
          <div key={tab} style={{ background: "rgba(0,0,0,.6)", border: "1px solid rgba(200,160,60,.3)", borderRadius: 8, padding: "6px 14px", color: "#D4B870", fontSize: 11, fontWeight: 600, cursor: "pointer" }}>{tab}</div>
        ))}
      </div>
    </div>
  );
}
