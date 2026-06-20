import { useEffect, useRef, useState } from "react";

const CW = 1280;
const CH = 720;
const HEX_SIZE = 28;
const COLS = 32;
const ROWS = 18;
const OFFSET_X = 20;
const OFFSET_Y = 20;

function hexCenter(col: number, row: number): [number, number] {
  const x = OFFSET_X + col * HEX_SIZE * 1.732 + (row % 2 === 1 ? HEX_SIZE * 0.866 : 0);
  const y = OFFSET_Y + row * HEX_SIZE * 1.5;
  return [x, y];
}

function hexPath(ctx: CanvasRenderingContext2D, cx: number, cy: number, r: number) {
  ctx.beginPath();
  for (let i = 0; i < 6; i++) {
    const angle = (Math.PI / 3) * i - Math.PI / 6;
    const px = cx + r * Math.cos(angle); const py = cy + r * Math.sin(angle);
    if (i === 0) ctx.moveTo(px, py); else ctx.lineTo(px, py);
  }
  ctx.closePath();
}

type Terrain = "ocean" | "lake" | "grass" | "forest" | "mountain" | "desert" | "snow" | "swamp";

const TERRAIN_COLORS: Record<Terrain, [string, string]> = {
  ocean:    ["#1a4a8a", "#0d3068"],
  lake:     ["#2a6ab5", "#1a4a8a"],
  grass:    ["#4a8a2a", "#3a7020"],
  forest:   ["#2a6020", "#1a4a10"],
  mountain: ["#7a7060", "#5a5048"],
  desert:   ["#C8A862", "#A88840"],
  snow:     ["#D8E8F0", "#B0C8D8"],
  swamp:    ["#4a6040", "#3a5030"],
};

function seededRand(seed: number) {
  let s = seed;
  return () => { s = (s * 16807 + 0) % 2147483647; return (s - 1) / 2147483646; };
}

interface Kingdom { col: number; row: number; name: string; isPlayer: boolean; power: number; }
interface Monster { col: number; row: number; tier: string; }
interface Crystal { col: number; row: number; }

function generateWorld() {
  const rand = seededRand(42937);
  const terrainMap: Terrain[][] = [];
  for (let row = 0; row < ROWS; row++) {
    terrainMap[row] = [];
    for (let col = 0; col < COLS; col++) {
      const r = rand();
      const distX = Math.abs(col - COLS / 2) / (COLS / 2);
      const distY = Math.abs(row - ROWS / 2) / (ROWS / 2);
      const dist = Math.sqrt(distX * distX + distY * distY);
      if (dist > 0.85 || r > 0.95) terrainMap[row][col] = "ocean";
      else if (dist > 0.7) { const t = rand(); terrainMap[row][col] = t < 0.4 ? "ocean" : t < 0.6 ? "lake" : t < 0.8 ? "grass" : "mountain"; }
      else if (row < 3) terrainMap[row][col] = r > 0.5 ? "snow" : "mountain";
      else if (row > ROWS - 4) terrainMap[row][col] = r > 0.4 ? "desert" : "grass";
      else {
        const v = rand();
        if (v < 0.28) terrainMap[row][col] = "grass";
        else if (v < 0.48) terrainMap[row][col] = "forest";
        else if (v < 0.60) terrainMap[row][col] = "mountain";
        else if (v < 0.68) terrainMap[row][col] = "lake";
        else if (v < 0.74) terrainMap[row][col] = "desert";
        else if (v < 0.78) terrainMap[row][col] = "swamp";
        else terrainMap[row][col] = "grass";
      }
    }
  }

  const kingdoms: Kingdom[] = [
    { col: 15, row: 8, name: "Ironhold", isPlayer: true, power: 24800 },
    { col: 8, row: 5, name: "Stormveil", isPlayer: false, power: 18500 },
    { col: 22, row: 6, name: "Dawnwatch", isPlayer: false, power: 31200 },
    { col: 6, row: 12, name: "Ashenvale", isPlayer: false, power: 9800 },
    { col: 24, row: 13, name: "Embervast", isPlayer: false, power: 42100 },
    { col: 18, row: 14, name: "Coldmere", isPlayer: false, power: 7600 },
    { col: 12, row: 3, name: "Frostfall", isPlayer: false, power: 15300 },
    { col: 26, row: 9, name: "Goldhaven", isPlayer: false, power: 28700 },
    { col: 10, row: 10, name: "Thornwall", isPlayer: false, power: 11400 },
    { col: 20, row: 3, name: "Skyspire", isPlayer: false, power: 19900 },
  ];

  const monsters: Monster[] = [
    { col: 5, row: 8, tier: "boss" }, { col: 18, row: 11, tier: "elite" },
    { col: 28, row: 5, tier: "rare" }, { col: 13, row: 15, tier: "common" },
    { col: 25, row: 15, tier: "ancient" }, { col: 9, row: 14, tier: "uncommon" },
    { col: 21, row: 10, tier: "rare" }, { col: 3, row: 5, tier: "elite" },
  ];

  const crystals: Crystal[] = [
    { col: 14, row: 6 }, { col: 20, row: 8 }, { col: 7, row: 10 },
    { col: 23, row: 4 }, { col: 17, row: 13 }, { col: 29, row: 11 },
    { col: 11, row: 7 }, { col: 26, row: 16 },
  ];

  kingdoms.forEach(k => { terrainMap[k.row][k.col] = "grass"; });

  return { terrainMap, kingdoms, monsters, crystals };
}

function drawMap(ctx: CanvasRenderingContext2D, terrainMap: Terrain[][], t: number) {
  for (let row = 0; row < ROWS; row++) {
    for (let col = 0; col < COLS; col++) {
      const [cx, cy] = hexCenter(col, row);
      const terrain = terrainMap[row][col];
      const [c1, c2] = TERRAIN_COLORS[terrain];
      const grd = ctx.createLinearGradient(cx, cy - HEX_SIZE, cx, cy + HEX_SIZE);
      grd.addColorStop(0, c1); grd.addColorStop(1, c2);
      hexPath(ctx, cx, cy, HEX_SIZE - 0.5);
      ctx.fillStyle = grd; ctx.fill();
      ctx.strokeStyle = "rgba(0,0,0,.12)"; ctx.lineWidth = 0.7; ctx.stroke();

      if (terrain === "forest") {
        ctx.fillStyle = "rgba(0,50,0,.35)"; ctx.font = "bold 13px serif";
        ctx.textAlign = "center"; ctx.textBaseline = "middle";
        ctx.fillText("🌲", cx, cy);
      } else if (terrain === "mountain") {
        ctx.fillStyle = "rgba(0,0,0,.25)"; ctx.font = "11px serif";
        ctx.textAlign = "center"; ctx.textBaseline = "middle";
        ctx.fillText("⛰️", cx, cy);
      } else if (terrain === "ocean" || terrain === "lake") {
        const shimmer = 0.05 + 0.03 * Math.sin(t * 0.04 + col * 0.3 + row * 0.2);
        ctx.fillStyle = `rgba(150,210,255,${shimmer})`;
        hexPath(ctx, cx, cy, HEX_SIZE - 3); ctx.fill();
      } else if (terrain === "desert") {
        ctx.fillStyle = "rgba(220,190,80,.15)"; hexPath(ctx, cx, cy, HEX_SIZE - 3); ctx.fill();
      } else if (terrain === "snow") {
        ctx.fillStyle = "rgba(255,255,255,.25)"; hexPath(ctx, cx, cy, HEX_SIZE - 3); ctx.fill();
      }
    }
  }
}

const MONSTER_COLORS: Record<string, string> = {
  common: "#9E9E9E", uncommon: "#4CAF50", rare: "#2196F3", elite: "#9C27B0", boss: "#FF5722", ancient: "#FF9800",
};

function drawEntities(ctx: CanvasRenderingContext2D, kingdoms: Kingdom[], monsters: Monster[], crystals: Crystal[], t: number) {
  crystals.forEach(c => {
    const [cx, cy] = hexCenter(c.col, c.row);
    const pulse = 0.6 + 0.3 * Math.sin(t * 0.07 + c.col);
    ctx.shadowColor = `rgba(180,100,255,${pulse})`; ctx.shadowBlur = 12;
    ctx.fillStyle = `rgba(180,100,255,${pulse})`; ctx.font = "bold 14px serif";
    ctx.textAlign = "center"; ctx.textBaseline = "middle"; ctx.fillText("💎", cx, cy);
    ctx.shadowBlur = 0; ctx.shadowColor = "transparent";
  });

  monsters.forEach(m => {
    const [cx, cy] = hexCenter(m.col, m.row);
    const pulse = 0.5 + 0.3 * Math.sin(t * 0.05 + m.col * 0.3);
    const color = MONSTER_COLORS[m.tier] || "#999";
    ctx.fillStyle = `${color}${Math.round(pulse * 60).toString(16).padStart(2, "0")}`;
    hexPath(ctx, cx, cy, HEX_SIZE * 0.7); ctx.fill();
    ctx.strokeStyle = color; ctx.lineWidth = 1.5; hexPath(ctx, cx, cy, HEX_SIZE * 0.7); ctx.stroke();
    ctx.fillStyle = "#fff"; ctx.font = "bold 11px serif";
    ctx.textAlign = "center"; ctx.textBaseline = "middle"; ctx.fillText("☠", cx, cy);
  });

  kingdoms.forEach(k => {
    const [cx, cy] = hexCenter(k.col, k.row);
    if (k.isPlayer) {
      const r = 2 + 1.5 * Math.sin(t * 0.06);
      const grd = ctx.createRadialGradient(cx, cy, 0, cx, cy, HEX_SIZE + r);
      grd.addColorStop(0, "rgba(255,200,50,.35)"); grd.addColorStop(1, "rgba(255,200,50,0)");
      ctx.fillStyle = grd; hexPath(ctx, cx, cy, HEX_SIZE + r); ctx.fill();
    }
    ctx.font = `bold 16px serif`; ctx.textAlign = "center"; ctx.textBaseline = "middle";
    ctx.shadowColor = k.isPlayer ? "#FFD700" : "rgba(0,0,0,.5)"; ctx.shadowBlur = 6;
    ctx.fillText("🏰", cx, cy - 2);
    ctx.shadowBlur = 0; ctx.shadowColor = "transparent";
    const nameW = k.name.length * 5.5 + 10;
    ctx.fillStyle = "rgba(10,15,30,.82)"; ctx.beginPath(); ctx.roundRect(cx - nameW / 2, cy + 14, nameW, 14, 4); ctx.fill();
    ctx.fillStyle = k.isPlayer ? "#FFD700" : "#E0D0A0"; ctx.font = `bold 9px sans-serif`;
    ctx.textAlign = "center"; ctx.textBaseline = "top"; ctx.fillText(k.name, cx, cy + 16);
  });
}

const { terrainMap: TERRAIN, kingdoms: KINGDOMS, monsters: MONSTERS, crystals: CRYSTALS } = generateWorld();

export function WorldMapLoK() {
  const cvs = useRef<HTMLCanvasElement>(null);
  const raf = useRef<number>(0);
  const [selected, setSelected] = useState<Kingdom | null>(null);
  const [hoveredCoord, setHoveredCoord] = useState<string>("");

  useEffect(() => {
    const canvas = cvs.current; if (!canvas) return;
    const ctx = canvas.getContext("2d")!;
    let t = 0;
    function render() {
      t++;
      ctx.fillStyle = "#0a1520"; ctx.fillRect(0, 0, CW, CH);
      drawMap(ctx, TERRAIN, t);
      drawEntities(ctx, KINGDOMS, MONSTERS, CRYSTALS, t);
      raf.current = requestAnimationFrame(render);
    }
    render();
    return () => cancelAnimationFrame(raf.current);
  }, []);

  function handleClick(e: React.MouseEvent<HTMLCanvasElement>) {
    const rect = cvs.current!.getBoundingClientRect();
    const mx = e.clientX - rect.left; const my = e.clientY - rect.top;
    for (const k of KINGDOMS) {
      const [cx, cy] = hexCenter(k.col, k.row);
      if (Math.hypot(mx - cx, my - cy) < HEX_SIZE) { setSelected(k); return; }
    }
    setSelected(null);
  }

  function handleMouseMove(e: React.MouseEvent<HTMLCanvasElement>) {
    const rect = cvs.current!.getBoundingClientRect();
    const mx = e.clientX - rect.left; const my = e.clientY - rect.top;
    for (let row = 0; row < ROWS; row++) for (let col = 0; col < COLS; col++) {
      const [cx, cy] = hexCenter(col, row); if (Math.hypot(mx - cx, my - cy) < HEX_SIZE * 0.9) { setHoveredCoord(`${col},${row}`); return; }
    }
    setHoveredCoord("");
  }

  const tierColors: Record<string, string> = { common: "#9E9E9E", uncommon: "#4CAF50", rare: "#2196F3", elite: "#9C27B0", boss: "#FF5722", ancient: "#FF9800" };

  return (
    <div className="relative overflow-hidden bg-[#0a1520]" style={{ width: CW, height: CH, fontFamily: "serif" }}>
      <canvas ref={cvs} width={CW} height={CH} style={{ display: "block", cursor: "crosshair" }} onClick={handleClick} onMouseMove={handleMouseMove} />

      <div style={{ position: "absolute", top: 0, left: 0, right: 0, background: "linear-gradient(180deg,rgba(0,0,0,.8) 0%,transparent 100%)", padding: "10px 18px", display: "flex", alignItems: "center", justifyContent: "space-between" }}>
        <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
          <span style={{ fontSize: 22 }}>🌍</span>
          <div>
            <div style={{ color: "#F5E9C8", fontSize: 16, fontWeight: 700 }}>Aethoria · World 1</div>
            <div style={{ color: "#8A7A5A", fontSize: 10 }}>Seed: 42937 · Season 3</div>
          </div>
        </div>
        <div style={{ display: "flex", gap: 16, alignItems: "center" }}>
          {[["🏰", KINGDOMS.length, "Kingdoms"], ["☠️", MONSTERS.length, "Threats"], ["💎", CRYSTALS.length, "Crystals"]].map(([icon, count, label]) => (
            <div key={String(label)} style={{ textAlign: "center" }}>
              <div style={{ fontSize: 18 }}>{icon}</div>
              <div style={{ color: "#F5E9C8", fontSize: 13, fontWeight: 700, lineHeight: 1 }}>{count}</div>
              <div style={{ color: "#8A7A5A", fontSize: 9 }}>{label}</div>
            </div>
          ))}
        </div>
        <div style={{ display: "flex", gap: 8 }}>
          {["🏰 My Kingdom", "⚔️ March", "🔍 Scout"].map(btn => (
            <button key={btn} style={{ background: "linear-gradient(180deg,#8B6914,#5a4010)", color: "#F5E9C8", border: "1px solid #C0940A", borderRadius: 6, padding: "5px 12px", fontSize: 11, fontWeight: 600, cursor: "pointer" }}>{btn}</button>
          ))}
        </div>
      </div>

      <div style={{ position: "absolute", bottom: 12, left: 12, background: "rgba(10,15,30,.88)", border: "1px solid rgba(200,160,60,.3)", borderRadius: 10, padding: "10px 14px", backdropFilter: "blur(6px)" }}>
        <div style={{ color: "#A0906A", fontSize: 9, fontWeight: 700, letterSpacing: 1, marginBottom: 6 }}>LEGEND</div>
        {[["#4a8a2a","Grassland"],["#2a6020","Forest"],["#7a7060","Mountain"],["#1a4a8a","Ocean"],["#C8A862","Desert"],["#D8E8F0","Snow"]].map(([c, name]) => (
          <div key={name} style={{ display: "flex", alignItems: "center", gap: 6, marginBottom: 3 }}>
            <div style={{ width: 10, height: 10, borderRadius: "50%", background: c, border: "1px solid rgba(255,255,255,.2)" }} />
            <span style={{ color: "#D4C490", fontSize: 10 }}>{name}</span>
          </div>
        ))}
        <div style={{ marginTop: 8, borderTop: "1px solid rgba(200,160,60,.2)", paddingTop: 8 }}>
          {Object.entries(tierColors).map(([tier, c]) => (
            <div key={tier} style={{ display: "flex", alignItems: "center", gap: 6, marginBottom: 3 }}>
              <div style={{ width: 10, height: 10, borderRadius: "50%", background: c, border: "1px solid rgba(255,255,255,.2)" }} />
              <span style={{ color: "#D4C490", fontSize: 10, textTransform: "capitalize" }}>{tier}</span>
            </div>
          ))}
        </div>
      </div>

      {selected && (
        <div style={{ position: "absolute", top: 70, right: 16, background: "rgba(10,15,30,.92)", border: `1px solid ${selected.isPlayer ? "#FFD700" : "rgba(200,160,60,.4)"}`, borderRadius: 12, padding: "14px 18px", minWidth: 200, backdropFilter: "blur(8px)" }}>
          <div style={{ display: "flex", alignItems: "center", gap: 8, marginBottom: 10 }}>
            <span style={{ fontSize: 24 }}>🏰</span>
            <div>
              <div style={{ color: selected.isPlayer ? "#FFD700" : "#F5E9C8", fontSize: 15, fontWeight: 700 }}>{selected.name}</div>
              <div style={{ color: "#8A7A5A", fontSize: 10 }}>{selected.isPlayer ? "▶ Your Kingdom" : "Enemy Kingdom"}</div>
            </div>
          </div>
          <div style={{ color: "#A0906A", fontSize: 11, marginBottom: 4 }}>Power: <span style={{ color: "#F5E9C8", fontWeight: 700 }}>{selected.power.toLocaleString()}</span></div>
          <div style={{ color: "#A0906A", fontSize: 11, marginBottom: 12 }}>Coords: <span style={{ color: "#F5E9C8" }}>{selected.col},{selected.row}</span></div>
          {!selected.isPlayer && (
            <div style={{ display: "flex", gap: 6 }}>
              {["⚔️ Attack","🔍 Scout","🤝 Ally"].map(a => (
                <button key={a} style={{ background: "rgba(200,160,60,.15)", color: "#D4B870", border: "1px solid rgba(200,160,60,.3)", borderRadius: 6, padding: "4px 8px", fontSize: 10, cursor: "pointer" }}>{a}</button>
              ))}
            </div>
          )}
          <button onClick={() => setSelected(null)} style={{ position: "absolute", top: 8, right: 10, background: "none", border: "none", color: "#8A7A5A", fontSize: 16, cursor: "pointer" }}>×</button>
        </div>
      )}

      {hoveredCoord && (
        <div style={{ position: "absolute", bottom: 12, right: 12, background: "rgba(10,15,30,.7)", border: "1px solid rgba(200,160,60,.2)", borderRadius: 6, padding: "3px 10px" }}>
          <span style={{ color: "#8A7A5A", fontSize: 10 }}>📍 {hoveredCoord}</span>
        </div>
      )}
    </div>
  );
}
