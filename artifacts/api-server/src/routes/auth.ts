import { Router } from "express";
import crypto from "crypto";
import { generateKingdomPosition } from "@workspace/game-engine";
import { createToken } from "../lib/jwt";
import { requireAuth, type AuthRequest } from "../middlewares/auth";
import { RegisterBody, LoginBody } from "@workspace/api-zod";
import {
  userRepository,
  worldRepository,
  kingdomRepository,
  resourceRepository,
} from "@workspace/db";

const router = Router();

function hashPassword(password: string): string {
  return crypto.createHash("sha256").update(password + "ek_salt_2026").digest("hex");
}

router.post("/register", async (req, res) => {
  const parsed = RegisterBody.safeParse(req.body);
  if (!parsed.success) {
    res.status(400).json({ error: "Invalid input" });
    return;
  }

  const { username, email, password, kingdomName } = parsed.data;

  try {
    const existingByEmail = await userRepository.findByEmail(email);
    if (existingByEmail) {
      res.status(409).json({ error: "Email already in use" });
      return;
    }

    const existingByUsername = await userRepository.findByUsername(username);
    if (existingByUsername) {
      res.status(409).json({ error: "Username already taken" });
      return;
    }

    let world = await worldRepository.findActive();
    if (!world) {
      world = await worldRepository.insert({
        name: "Aethoria",
        description: "The first world of Eternal Kingdoms",
        status: "active",
        maxKingdoms: 1000,
        seed: 42937,
      });
    }

    const passwordHash = hashPassword(password);
    const user = await userRepository.insert({
      username,
      email,
      passwordHash,
      role: "player",
      worldId: world.id,
    });

    const existingPositions = await kingdomRepository.findExistingPositionsByWorldId(world.id);
    const taken = existingPositions
      .filter((k) => k.x !== null && k.y !== null)
      .map((k) => ({ x: k.x as number, y: k.y as number }));

    const pos = generateKingdomPosition(user.id, world.seed ?? 42937, taken);
    const finalKingdomName = kingdomName ?? `${username}'s Kingdom`;

    const kingdom = await kingdomRepository.insert({
      userId: user.id,
      worldId: world.id,
      name: finalKingdomName,
      power: 0,
      mapX: pos.x,
      mapY: pos.y,
    });

    await resourceRepository.insert({
      kingdomId: kingdom.id,
      food: 500,
      wood: 500,
      stone: 500,
      iron: 200,
      gold: 100,
    });

    const token = createToken({ userId: user.id, username: user.username, role: user.role });

    res.status(201).json({
      token,
      user: {
        id: user.id,
        username: user.username,
        email: user.email,
        role: user.role,
        createdAt: user.createdAt,
        lastLoginAt: user.lastLoginAt,
      },
    });
  } catch (err) {
    req.log.error({ err }, "Register error");
    res.status(500).json({ error: "Internal server error" });
  }
});

router.post("/login", async (req, res) => {
  const parsed = LoginBody.safeParse(req.body);
  if (!parsed.success) {
    res.status(400).json({ error: "Invalid input" });
    return;
  }

  const { email, password } = parsed.data;

  try {
    const user = await userRepository.findByEmail(email);

    if (!user || user.passwordHash !== hashPassword(password)) {
      res.status(401).json({ error: "Invalid email or password" });
      return;
    }

    await userRepository.updateLastLogin(user.id);

    const token = createToken({ userId: user.id, username: user.username, role: user.role });

    res.json({
      token,
      user: {
        id: user.id,
        username: user.username,
        email: user.email,
        role: user.role,
        createdAt: user.createdAt,
        lastLoginAt: new Date(),
      },
    });
  } catch (err) {
    req.log.error({ err }, "Login error");
    res.status(500).json({ error: "Internal server error" });
  }
});

router.get("/me", requireAuth, async (req: AuthRequest, res) => {
  try {
    const user = await userRepository.findById(req.user!.userId);

    if (!user) {
      res.status(401).json({ error: "User not found" });
      return;
    }

    res.json({
      id: user.id,
      username: user.username,
      email: user.email,
      role: user.role,
      createdAt: user.createdAt,
      lastLoginAt: user.lastLoginAt,
    });
  } catch (err) {
    req.log.error({ err }, "GetMe error");
    res.status(500).json({ error: "Internal server error" });
  }
});

export default router;
