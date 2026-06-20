import type { SupabaseClient, RealtimeChannel } from "@supabase/supabase-js";

export type RealtimeEvent =
  | "resource_updates"
  | "construction_updates"
  | "march_updates"
  | "alliance_chat"
  | "world_events";

interface ChannelSubscription {
  channel: RealtimeChannel;
  event: RealtimeEvent;
  kingdomId?: string;
  worldId?: string;
}

export class RealtimeService {
  private subscriptions: Map<string, ChannelSubscription> = new Map();
  private client: SupabaseClient | null = null;
  private _ready = false;

  constructor(client?: SupabaseClient) {
    this.client = client ?? null;
  }

  get isReady(): boolean {
    return this._ready && this.client !== null;
  }

  initialize(client: SupabaseClient): void {
    this.client = client;
    this._ready = true;
  }

  subscribeToResourceUpdates(
    kingdomId: string,
    _onUpdate: (payload: unknown) => void
  ): string {
    this._assertReady();
    const key = `resource_updates:${kingdomId}`;
    if (this.subscriptions.has(key)) return key;

    const channel = this.client!
      .channel(`kingdom:${kingdomId}:resources`)
      .on("broadcast", { event: "resource_tick" }, _onUpdate);

    channel.subscribe();
    this.subscriptions.set(key, { channel, event: "resource_updates", kingdomId });
    return key;
  }

  subscribeToConstructionUpdates(
    kingdomId: string,
    _onUpdate: (payload: unknown) => void
  ): string {
    this._assertReady();
    const key = `construction_updates:${kingdomId}`;
    if (this.subscriptions.has(key)) return key;

    const channel = this.client!
      .channel(`kingdom:${kingdomId}:construction`)
      .on("broadcast", { event: "construction_complete" }, _onUpdate);

    channel.subscribe();
    this.subscriptions.set(key, { channel, event: "construction_updates", kingdomId });
    return key;
  }

  subscribeToMarchUpdates(
    kingdomId: string,
    _onUpdate: (payload: unknown) => void
  ): string {
    this._assertReady();
    const key = `march_updates:${kingdomId}`;
    if (this.subscriptions.has(key)) return key;

    const channel = this.client!
      .channel(`kingdom:${kingdomId}:marches`)
      .on("broadcast", { event: "march_update" }, _onUpdate);

    channel.subscribe();
    this.subscriptions.set(key, { channel, event: "march_updates", kingdomId });
    return key;
  }

  subscribeToAllianceChat(
    allianceId: string,
    _onMessage: (payload: unknown) => void
  ): string {
    this._assertReady();
    const key = `alliance_chat:${allianceId}`;
    if (this.subscriptions.has(key)) return key;

    const channel = this.client!
      .channel(`alliance:${allianceId}:chat`)
      .on("broadcast", { event: "chat_message" }, _onMessage);

    channel.subscribe();
    this.subscriptions.set(key, { channel, event: "alliance_chat" });
    return key;
  }

  subscribeToWorldEvents(
    worldId: string,
    _onEvent: (payload: unknown) => void
  ): string {
    this._assertReady();
    const key = `world_events:${worldId}`;
    if (this.subscriptions.has(key)) return key;

    const channel = this.client!
      .channel(`world:${worldId}:events`)
      .on("broadcast", { event: "world_event" }, _onEvent);

    channel.subscribe();
    this.subscriptions.set(key, { channel, event: "world_events", worldId });
    return key;
  }

  unsubscribe(key: string): void {
    const sub = this.subscriptions.get(key);
    if (!sub) return;
    sub.channel.unsubscribe();
    this.subscriptions.delete(key);
  }

  unsubscribeAll(): void {
    for (const [key] of this.subscriptions) {
      this.unsubscribe(key);
    }
  }

  getActiveSubscriptions(): string[] {
    return Array.from(this.subscriptions.keys());
  }

  private _assertReady(): void {
    if (!this.isReady) {
      throw new Error(
        "RealtimeService: not initialized. Call initialize(supabaseClient) before subscribing. " +
        "Realtime features require SUPABASE_URL and SUPABASE_ANON_KEY env vars."
      );
    }
  }
}

export const realtimeService = new RealtimeService();
