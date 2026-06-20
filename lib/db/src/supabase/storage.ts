import { getSupabaseAdminClient } from "./adminClient";
import { STORAGE_BUCKETS, type SupabaseStorageBucket } from "./types";

export async function ensureStorageBuckets(): Promise<void> {
  const admin = getSupabaseAdminClient();
  const { data: existing, error } = await admin.storage.listBuckets();
  if (error) throw new Error(`Failed to list storage buckets: ${error.message}`);

  const existingNames = new Set((existing ?? []).map((b) => b.name));

  for (const [name, config] of Object.entries(STORAGE_BUCKETS)) {
    if (existingNames.has(name)) continue;
    const { error: createError } = await admin.storage.createBucket(name, {
      public: config.public,
      fileSizeLimit: config.maxFileSizeMb * 1024 * 1024,
      allowedMimeTypes: config.allowedMimeTypes,
    });
    if (createError) {
      throw new Error(`Failed to create bucket '${name}': ${createError.message}`);
    }
  }
}

export async function getStoragePublicUrl(
  bucket: SupabaseStorageBucket,
  path: string
): Promise<string> {
  const admin = getSupabaseAdminClient();
  const { data } = admin.storage.from(bucket).getPublicUrl(path);
  return data.publicUrl;
}

export { STORAGE_BUCKETS };
export type { SupabaseStorageBucket };
