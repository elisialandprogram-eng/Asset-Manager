---
name: Orval + TanStack Query v5 queryKey
description: How to satisfy TypeScript when passing options to Orval-generated hooks
---

TanStack Query v5 `UseQueryOptions` requires `queryKey` as a mandatory field. Orval v8 generates hooks that accept `{ query?: UseQueryOptions<...> }` but doesn't auto-fill `queryKey` into your partial. Result: TS2741 error.

**Fix:** Always pass `queryKey` using the generated `getXxxQueryKey()` helper:

```tsx
import { useGetKingdomState, getGetKingdomStateQueryKey } from "@workspace/api-client-react";

useGetKingdomState(id, {
  query: {
    queryKey: getGetKingdomStateQueryKey(id),
    enabled: !!id,
    refetchInterval: 8000,
  }
});
```

**Why:** The Orval-generated `getXxxQueryOptions` adds queryKey internally, but TypeScript sees the raw `UseQueryOptions` type on the `query` parameter and demands queryKey.

**How to apply:** Every Orval hook call that passes a `query` option object needs `queryKey` from the corresponding `getXxxQueryKey(args)` export. Pattern: `get` + `Get` + hook name without `use` + `QueryKey`.
