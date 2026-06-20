import crypto from "crypto";
import { logger } from "./logger";

const SESSION_SECRET = process.env["SESSION_SECRET"] || "eternal-kingdoms-dev-secret";

export interface JwtPayload {
  userId: number;
  username: string;
  role: string;
  iat?: number;
  exp?: number;
}

function base64url(data: string): string {
  return Buffer.from(data).toString("base64url");
}

function fromBase64url(data: string): string {
  return Buffer.from(data, "base64url").toString("utf8");
}

function sign(payload: string, secret: string): string {
  return crypto
    .createHmac("sha256", secret)
    .update(payload)
    .digest("base64url");
}

export function createToken(payload: JwtPayload): string {
  const header = base64url(JSON.stringify({ alg: "HS256", typ: "JWT" }));
  const body = base64url(
    JSON.stringify({
      ...payload,
      iat: Math.floor(Date.now() / 1000),
      exp: Math.floor(Date.now() / 1000) + 60 * 60 * 24 * 7, // 7 days
    })
  );
  const sig = sign(`${header}.${body}`, SESSION_SECRET);
  return `${header}.${body}.${sig}`;
}

export function verifyToken(token: string): JwtPayload | null {
  try {
    const parts = token.split(".");
    if (parts.length !== 3) return null;
    const [header, body, sig] = parts;
    const expected = sign(`${header}.${body}`, SESSION_SECRET);
    if (sig !== expected) return null;
    const payload = JSON.parse(fromBase64url(body)) as JwtPayload;
    if (payload.exp && payload.exp < Math.floor(Date.now() / 1000)) return null;
    return payload;
  } catch (err) {
    logger.warn({ err }, "JWT verification failed");
    return null;
  }
}
