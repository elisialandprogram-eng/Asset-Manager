export interface SupabaseClientConfig {
  url: string;
  key: string;
}

export interface SupabaseAdminConfig {
  url: string;
  serviceRoleKey: string;
}

export type SupabaseStorageBucket =
  | "avatars"
  | "kingdom-assets"
  | "building-icons"
  | "monster-assets"
  | "unity-assets"
  | "battle-reports"
  | "alliance-assets"
  | "future-nft-assets";

export const STORAGE_BUCKETS: Record<
  SupabaseStorageBucket,
  { public: boolean; maxFileSizeMb: number; allowedMimeTypes: string[] }
> = {
  "avatars": {
    public: true,
    maxFileSizeMb: 2,
    allowedMimeTypes: ["image/png", "image/jpeg", "image/webp"],
  },
  "kingdom-assets": {
    public: true,
    maxFileSizeMb: 10,
    allowedMimeTypes: ["image/png", "image/svg+xml", "image/webp"],
  },
  "building-icons": {
    public: true,
    maxFileSizeMb: 5,
    allowedMimeTypes: ["image/png", "image/svg+xml"],
  },
  "monster-assets": {
    public: true,
    maxFileSizeMb: 10,
    allowedMimeTypes: ["image/png", "image/svg+xml", "image/webp"],
  },
  "unity-assets": {
    public: false,
    maxFileSizeMb: 500,
    allowedMimeTypes: ["application/octet-stream", "application/gzip"],
  },
  "battle-reports": {
    public: false,
    maxFileSizeMb: 1,
    allowedMimeTypes: ["application/json"],
  },
  "alliance-assets": {
    public: true,
    maxFileSizeMb: 5,
    allowedMimeTypes: ["image/png", "image/svg+xml", "image/webp"],
  },
  "future-nft-assets": {
    public: true,
    maxFileSizeMb: 50,
    allowedMimeTypes: ["image/png", "image/gif", "video/mp4", "application/json"],
  },
};
